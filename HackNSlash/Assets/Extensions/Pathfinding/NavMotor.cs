using System.Collections;
using UnityEngine;

namespace Extensions.Pathfinding
{
    /** <summary>
     * Rigidbody-based movement motor for navigating characters.
     * Consumes a steering direction and executes physical movement,
     * rotation, and all specialised motion primitives used in hack-and-slash
     * combat (strafe, orbit, back-jump, wander, etc.).
     * </summary>
     * <remarks>
     * This component is intentionally agnostic of pathfinding or AI logic.
     * Feed it directions and call the appropriate method; the motor handles
     * physics forces, rotation, and timing internally.
     * </remarks>
     */
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class NavMotor : MonoBehaviour
    {
        #region Configuration

        [Tooltip("Tuning data shared with NavPathPlanner.")]
        public NavConfig config;

        #endregion

        #region Public State

        /** <summary>The direction the motor is currently trying to move (XZ, normalised).</summary> */
        public Vector3 CurrentMoveDirection { get; private set; }

        /** <summary>True while a back-jump is in progress.</summary> */
        public bool IsBackJumping { get; private set; }

        /** <summary>True while a wander destination is active.</summary> */
        public bool IsWandering => _hasWanderTarget;

        /** <summary>Current orbit angle around the orbit target (degrees).</summary> */
        public float OrbitAngle { get; private set; }

        #endregion

        #region Private State

        private Rigidbody _rb;
        private bool _motionLocked;
        private float _motionLockTimer;
        private Vector3 _wanderOrigin;
        private float _wanderTimer;
        private Vector3 _wanderTarget;
        private bool _hasWanderTarget;
        private Coroutine _backJumpRoutine;

        // Config fallbacks.
        private float MaxSpeed => config != null ? config.maxSpeed : 5f;
        private float AccelForce => config != null ? config.accelerationForce : 40f;
        private float DecelForce => config != null ? config.decelerationForce : 40f;
        private float RotSpeed => config != null ? config.rotationSpeed : 360f;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _wanderOrigin = transform.position;
        }

        private void FixedUpdate()
        {
            // Tick motion lock.
            if (_motionLocked)
            {
                _motionLockTimer -= Time.fixedDeltaTime;
                if (_motionLockTimer <= 0f) _motionLocked = false;
            }
        }

        private void OnDisable()
        {
            if (_backJumpRoutine != null) StopCoroutine(_backJumpRoutine);
        }

        #endregion

        #region Core Movement

        /** <summary>
         * Move toward <paramref name="direction"/> (normalised XZ) at a fraction of <see cref="NavConfig.maxSpeed"/>.
         * Call this every Update or FixedUpdate from the AI.
         * </summary>
         * <param name="direction">Normalised XZ direction.</param>
         * <param name="speedMultiplier">0–1 multiplier on maxSpeed.</param>
         * <param name="faceDirection">When true, rotate to face the movement direction.</param>
         */
        public void Move(Vector3 direction, float speedMultiplier = 1f, bool faceDirection = true)
        {
            if (_motionLocked) return;

            direction.y = 0f;
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            CurrentMoveDirection = direction;

            float targetSpeed = MaxSpeed * Mathf.Clamp01(speedMultiplier);
            Vector3 targetVelocity = direction * targetSpeed;

            Vector3 currentVelocityXZ = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            Vector3 diff = targetVelocity - currentVelocityXZ;

            float forceScale = direction.sqrMagnitude > 0.01f ? AccelForce : DecelForce;
            _rb.AddForce(diff * forceScale, ForceMode.Acceleration);

            if (faceDirection && direction.sqrMagnitude > 0.01f)
                RotateToward(direction);
        }

        /** <summary>
         * Decelerate to zero without changing look direction.
         * </summary>
         */
        public void Brake()
        {
            Vector3 currentXZ = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(-currentXZ * DecelForce, ForceMode.Acceleration);
            CurrentMoveDirection = Vector3.zero;
        }

        /** <summary>
         * Immediately zero horizontal velocity (hard stop, no deceleration ramp).
         * </summary>
         */
        public void Stop()
        {
            Vector3 v = _rb.linearVelocity;
            v.x = 0f;
            v.z = 0f;
            _rb.linearVelocity = v;
            CurrentMoveDirection = Vector3.zero;
        }

        /** <summary>
         * Rotate the agent to face <paramref name="direction"/> using the configured rotation speed.
         * </summary>
         */
        public void RotateToward(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target,
                RotSpeed * Time.deltaTime);
        }

        /** <summary>
         * Rotate the agent to face a world-space <paramref name="position"/>.
         * </summary>
         */
        public void FacePosition(Vector3 position)
        {
            Vector3 dir = (position - transform.position);
            dir.y = 0f;
            RotateToward(dir.normalized);
        }

        #endregion

        #region Strafe

        /** <summary>
         * Move perpendicular to the direction toward <paramref name="pivot"/>.
         * Positive <paramref name="sign"/> = strafe right, negative = strafe left.
         * The agent always faces the pivot while strafing.
         * </summary>
         */
        public void Strafe(Vector3 pivot, float sign = 1f, float speedMultiplier = 1f)
        {
            Vector3 toPivot = (pivot - transform.position);
            toPivot.y = 0f;
            if (toPivot.sqrMagnitude < 0.0001f) return;

            Vector3 right = Vector3.Cross(Vector3.up, toPivot.normalized);
            float mult = (config != null ? config.strafeSpeedMultiplier : 0.6f) * Mathf.Clamp01(speedMultiplier);
            Move(right * Mathf.Sign(sign), mult, false);
            FacePosition(pivot);
        }

        #endregion

        #region Orbit

        /** <summary>
         * Orbit around <paramref name="center"/> at <see cref="NavConfig.circleRadius"/>.
         * Call every frame; the orbit angle advances automatically.
         * </summary>
         * <param name="center">World-space pivot point.</param>
         * <param name="angularSpeed">Degrees per second. Negative = clockwise.</param>
         */
        public void Orbit(Vector3 center, float angularSpeed = 60f)
        {
            OrbitAngle += angularSpeed * Time.deltaTime;
            float rad = OrbitAngle * Mathf.Deg2Rad;
            float r = config != null ? config.circleRadius : 4f;
            float spring = config != null ? config.circleRadiusSpring : 3f;

            Vector3 desired = center + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * r;
            Vector3 toDesired = desired - transform.position;
            toDesired.y = 0f;

            // Spring toward the orbit ring.
            Vector3 steering = toDesired.normalized * spring;
            Move(steering.normalized, 1f, false);
            FacePosition(center);
        }

        /** <summary>
         * Reset the orbit angle so the next <see cref="Orbit"/> call starts cleanly.
         * </summary>
         */
        public void ResetOrbit()
        {
            Vector3 delta = transform.position - Vector3.zero; // placeholder; caller sets center
            OrbitAngle = Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg;
        }

        /** <summary>
         * Resets the orbit angle relative to a specific <paramref name="center"/>.
         * </summary>
         */
        public void ResetOrbitAround(Vector3 center)
        {
            Vector3 delta = transform.position - center;
            delta.y = 0f;
            OrbitAngle = Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg;
        }

        #endregion

        #region Back Jump

        /** <summary>
         * Launch the agent backward and upward away from <paramref name="threatPosition"/>,
         * locking horizontal input for <see cref="NavConfig.backJumpLockDuration"/> seconds.
         * </summary>
         */
        public void BackJump(Vector3 threatPosition)
        {
            if (IsBackJumping) return;

            if (_backJumpRoutine != null) StopCoroutine(_backJumpRoutine);
            _backJumpRoutine = StartCoroutine(BackJumpRoutine(threatPosition));
        }

        private IEnumerator BackJumpRoutine(Vector3 threatPosition)
        {
            IsBackJumping = true;

            float hSpeed = config != null ? config.backJumpHorizontalSpeed : 6f;
            float vSpeed = config != null ? config.backJumpVerticalSpeed : 5f;
            float lockDur = config != null ? config.backJumpLockDuration : 0.35f;

            Vector3 away = (transform.position - threatPosition);
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
            away.Normalize();

            // Zero current velocity and apply jump impulse.
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x * 0f, 0f, _rb.linearVelocity.z * 0f);
            _rb.AddForce(new Vector3(away.x * hSpeed, vSpeed, away.z * hSpeed), ForceMode.VelocityChange);

            LockMotion(lockDur);

            yield return new WaitForSeconds(lockDur);
            IsBackJumping = false;
        }

        #endregion

        #region Wander

        /** <summary>
         * Tick wander behaviour. Call every Update/FixedUpdate.
         * Picks a random destination near the spawn origin and navigates toward it.
         * Returns the desired XZ move direction (normalised) to feed into an external
         * path planner or directly into <see cref="Move"/>.
         * </summary>
         * <returns>Desired movement direction, or <c>Vector3.zero</c> when idle between destinations.</returns>
         */
        public Vector3 TickWander()
        {
            float interval = config != null
                ? Random.Range(config.wanderIntervalMin, config.wanderIntervalMax)
                : 3f;

            _wanderTimer += Time.deltaTime;

            if (!_hasWanderTarget || _wanderTimer >= interval)
            {
                _wanderTimer = 0f;
                _wanderTarget = PickRandomWanderPoint();
                _hasWanderTarget = true;
            }

            Vector3 toTarget = _wanderTarget - transform.position;
            toTarget.y = 0f;

            float arrivedDist = config != null ? config.arrivalRadius : 0.25f;
            if (toTarget.sqrMagnitude <= arrivedDist * arrivedDist)
            {
                _hasWanderTarget = false;
                return Vector3.zero;
            }

            return toTarget.normalized;
        }

        /** <summary>Reset the wander origin to the agent's current position.</summary> */
        public void ResetWanderOrigin()
        {
            _wanderOrigin = transform.position;
        }

        private Vector3 PickRandomWanderPoint()
        {
            float r = config != null ? config.wanderRadius : 8f;
            Vector2 offset = Random.insideUnitCircle * r;
            return _wanderOrigin + new Vector3(offset.x, 0f, offset.y);
        }

        #endregion

        #region Retreat

        /** <summary>
         * Move directly away from <paramref name="threatPosition"/> at full speed.
         * </summary>
         */
        public void RetreatFrom(Vector3 threatPosition, float speedMultiplier = 1f)
        {
            Vector3 away = (transform.position - threatPosition);
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = transform.forward;
            Move(away.normalized, speedMultiplier);
        }

        #endregion

        #region Chase

        /** <summary>
         * Move directly toward <paramref name="targetPosition"/> at full speed,
         * stopping within <see cref="NavConfig.arrivalRadius"/>.
         * Bypasses pathfinding — use for short-range charges or dashes.
         * </summary>
         */
        public void ChargeToward(Vector3 targetPosition, float speedMultiplier = 1f)
        {
            Vector3 dir = (targetPosition - transform.position);
            dir.y = 0f;
            float arrived = config != null ? config.arrivalRadius : 0.25f;
            if (dir.sqrMagnitude <= arrived * arrived)
            {
                Brake();
                return;
            }
            Move(dir.normalized, speedMultiplier);
        }

        #endregion

        #region Utilities

        /** <summary>
         * Temporarily lock horizontal movement for <paramref name="duration"/> seconds.
         * Useful to freeze the agent during attack wind-ups.
         * </summary>
         */
        public void LockMotion(float duration)
        {
            _motionLocked = true;
            _motionLockTimer = duration;
        }

        /** <summary>Immediately unlock motion if it was locked by <see cref="LockMotion"/>.</summary> */
        public void UnlockMotion()
        {
            _motionLocked = false;
            _motionLockTimer = 0f;
        }

        /** <summary>
         * True if the agent's XZ speed is below <paramref name="threshold"/>.
         * Useful for blend-tree or animation decisions.
         * </summary>
         */
        public bool IsEffectivelyStationary(float threshold = 0.1f)
        {
            Vector3 xzVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            return xzVel.sqrMagnitude < threshold * threshold;
        }

        /** <summary>XZ speed of the Rigidbody in m/s.</summary> */
        public float CurrentSpeed()
        {
            return new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z).magnitude;
        }

        #endregion

        #region Gizmos
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Current move direction arrow.
            if (CurrentMoveDirection.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, CurrentMoveDirection * 1.5f);
            }

            // Wander target.
            if (_hasWanderTarget)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f);
                Gizmos.DrawWireSphere(_wanderTarget, 0.3f);
                Gizmos.DrawLine(transform.position, _wanderTarget);
            }

            // Wander radius around origin.
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            float r = config != null ? config.wanderRadius : 8f;
            DrawCircleXZ(_wanderOrigin, r);

            // Circle orbit radius.
            if (config != null && config.circleRadius > 0f)
            {
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.2f);
                DrawCircleXZ(transform.position, config.circleRadius);
            }
        }

        private static void DrawCircleXZ(Vector3 center, float radius)
        {
            const int segments = 32;
            float step = 360f / segments * Mathf.Deg2Rad;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = i * step;
                Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
#endif
        #endregion
    }
}



