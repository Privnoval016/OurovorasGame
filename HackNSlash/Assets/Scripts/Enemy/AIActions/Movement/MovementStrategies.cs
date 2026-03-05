using System;
using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Chase the player along the A* path.
 * Automatically re-evaluates arrival and releases the action once within
 * <see cref="stopDistance"/> of the target.
 * </summary>
 */
[Serializable]
public class ChaseStrategy : IMovementStrategy
{
    [Tooltip("Speed multiplier on NavConfig.maxSpeed while chasing.")]
    [Range(0.1f, 2f)] public float speedMultiplier = 1f;

    [Tooltip("XZ distance at which chasing is considered complete.")]
    [Min(0.1f)] public float stopDistance = 2f;

    [Tooltip("If true, the enemy faces the player while approaching.")]
    public bool faceTarget = true;

    private Transform _target;

    public string DisplayName => "Chase";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _target = context.GetTarget(EnemyContextKeys.Player);
        esm.ts.ea.RootMotionEnabled(false);
        if (animData != null) esm.ts.ea.PlayEnemyAnimation(animData.walkCycle);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        if (_target == null)
        {
            // Refresh if sensor finds the player again.
            _target = context.GetTarget(EnemyContextKeys.Player);
            return;
        }

        if (faceTarget) esm.TurnToPosition(_target.position);
        esm.MoveToDestination(_target.position, speedMultiplier);
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm)
        => esm.Brake();

    public bool IsComplete(EnemyContext context, EnemyStateMachine esm)
    {
        if (_target == null) return true;
        Vector3 delta = _target.position - esm.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= stopDistance * stopDistance;
    }
}

/** <summary>
 * Strafe perpendicular to the player — the enemy moves sideways while
 * keeping its facing locked on the target.
 * </summary>
 */
[Serializable]
public class StrafeStrategy : IMovementStrategy
{
    [Tooltip("Positive = strafe right, negative = strafe left.")]
    [Range(-1f, 1f)] public float strafeSign = 1f;

    [Tooltip("Speed multiplier relative to NavConfig.maxSpeed.")]
    [Range(0.1f, 2f)] public float speedMultiplier = 0.7f;

    [Tooltip("Duration (seconds) before the action completes and the brain re-evaluates.")]
    [Min(0.1f)] public float duration = 1.5f;

    private float _elapsed;
    private Transform _target;

    public string DisplayName => $"Strafe {(strafeSign >= 0 ? "Right" : "Left")}";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _elapsed = 0f;
        _target  = context.GetTarget(EnemyContextKeys.Player);
        esm.ts.ea.RootMotionEnabled(false);
        if (animData != null) esm.ts.ea.PlayEnemyAnimation(animData.walkCycle);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;
        if (_target == null) _target = context.GetTarget(EnemyContextKeys.Player);
        if (_target == null) return;
        esm.StrafeAround(_target.position, strafeSign, speedMultiplier);
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm) => esm.Brake();
    public bool IsComplete(EnemyContext context, EnemyStateMachine esm) => _elapsed >= duration;
}

/** <summary>
 * Orbit around the player at <see cref="Extensions.Pathfinding.NavConfig.circleRadius"/>.
 * </summary>
 */
[Serializable]
public class OrbitStrategy : IMovementStrategy
{
    [Tooltip("Angular speed in degrees per second. Negative = clockwise.")]
    public float angularSpeed = 60f;

    [Tooltip("Duration (seconds) before the brain re-evaluates.")]
    [Min(0.1f)] public float duration = 2f;

    [Tooltip("Orbit radius in metres. When > 0 this overrides NavConfig.circleRadius, " +
             "letting you set a per-action orbit distance without touching the shared config.")]
    [Min(0f)] public float radiusOverride = 0f;

    private float _elapsed;
    private Transform _target;

    public string DisplayName => "Orbit";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _elapsed = 0f;
        _target  = context.GetTarget(EnemyContextKeys.Player);
        esm.ts.ea.RootMotionEnabled(false);
        if (animData != null) esm.ts.ea.PlayEnemyAnimation(animData.walkCycle);
        if (_target != null) esm.ts.motor.ResetOrbitAround(_target.position);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;
        if (_target == null) _target = context.GetTarget(EnemyContextKeys.Player);
        if (_target == null) return;

        if (radiusOverride > 0f)
            esm.ts.motor.Orbit(_target.position, angularSpeed, radiusOverride);
        else
            esm.OrbitAround(_target.position, angularSpeed);
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm) => esm.Brake();
    public bool IsComplete(EnemyContext context, EnemyStateMachine esm) => _elapsed >= duration;
}

/** <summary>
 * Move directly away from the player at full speed.
 * </summary>
 */
[Serializable]
public class RetreatStrategy : IMovementStrategy
{
    [Range(0.1f, 2f)] public float speedMultiplier = 1f;

    [Tooltip("Stop retreating when this far from the player.")]
    [Min(0.5f)] public float desiredDistance = 6f;

    [Tooltip("Max seconds to retreat before the brain re-evaluates anyway.")]
    [Min(0.1f)] public float maxDuration = 2f;

    private float _elapsed;
    private Transform _target;

    public string DisplayName => "Retreat";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _elapsed = 0f;
        _target  = context.GetTarget(EnemyContextKeys.Player);
        esm.ts.ea.RootMotionEnabled(false);
        if (animData != null) esm.ts.ea.PlayEnemyAnimation(animData.walkCycle);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;
        if (_target == null) _target = context.GetTarget(EnemyContextKeys.Player);
        if (_target == null) return;
        esm.RetreatFrom(_target.position, speedMultiplier);
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm) => esm.Brake();

    public bool IsComplete(EnemyContext context, EnemyStateMachine esm)
    {
        if (_elapsed >= maxDuration || _target == null) return true;
        Vector3 delta = _target.position - esm.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude >= desiredDistance * desiredDistance;
    }
}

/** <summary>
 * Leap backward away from the player.
 * The action completes as soon as the back-jump motor flag clears.
 * </summary>
 */
[Serializable]
public class BackJumpStrategy : IMovementStrategy
{
    [Tooltip("Seconds to wait after landing before releasing the action.")]
    [Min(0f)] public float settleTime = 0.1f;

    private Transform _target;
    private float _settleTimer;
    private bool _jumpFired;

    public string DisplayName => "Back Jump";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _target    = context.GetTarget(EnemyContextKeys.Player);
        _jumpFired = false;
        _settleTimer = 0f;
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        if (!_jumpFired)
        {
            Vector3 threat = _target != null ? _target.position : esm.transform.position - esm.transform.forward;
            esm.BackJump(threat);
            _jumpFired = true;
        }
        if (_jumpFired && !esm.ts.motor.IsBackJumping) _settleTimer += Time.deltaTime;
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm) { }
    public bool IsComplete(EnemyContext context, EnemyStateMachine esm)
        => _jumpFired && !esm.ts.motor.IsBackJumping && _settleTimer >= settleTime;
}

/** <summary>
 * Wander around the spawn point until a time budget is exhausted.
 * </summary>
 */
[Serializable]
public class WanderStrategy : IMovementStrategy
{
    [Tooltip("Seconds to wander before the brain re-evaluates.")]
    [Min(0.5f)] public float duration = 4f;

    private float _elapsed;

    public string DisplayName => "Wander";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _elapsed = 0f;
        esm.ts.ea.RootMotionEnabled(false);
        if (animData != null) esm.ts.ea.PlayEnemyAnimation(animData.walkCycle);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm) { _elapsed += Time.deltaTime; esm.Wander(); }
    public void OnExit(EnemyContext context, EnemyStateMachine esm) => esm.Brake();
    public bool IsComplete(EnemyContext context, EnemyStateMachine esm) => _elapsed >= duration;
}

/** <summary>
 * Stand still for a fixed duration — useful as an idle before committing to an attack.
 * </summary>
 */
[Serializable]
public class IdleStrategy : IMovementStrategy
{
    [Tooltip("How long to idle before releasing back to the brain.")]
    [Min(0.1f)] public float duration = 1f;

    [Tooltip("If true, face the player while idling.")]
    public bool facePlayer = true;

    private float _elapsed;
    private Transform _target;

    public string DisplayName => "Idle";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _elapsed = 0f;
        _target  = context.GetTarget(EnemyContextKeys.Player);
        esm.Brake();
        if (animData != null) esm.ts.ea.PlayEnemyAnimation(animData.idleClip);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;
        if (!facePlayer) return;
        if (_target == null) _target = context.GetTarget(EnemyContextKeys.Player);
        if (_target != null) esm.TurnToPosition(_target.position);
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm) { }
    public bool IsComplete(EnemyContext context, EnemyStateMachine esm) => _elapsed >= duration;
}

/** <summary>
 * Charge straight at the player without following the A* path — for short-range
 * dashes and quick aggressive rushes.
 * </summary>
 */
[Serializable]
public class ChargeStrategy : IMovementStrategy
{
    [Range(0.1f, 3f)] public float speedMultiplier = 1.5f;

    [Tooltip("Stop when within this distance.")]
    [Min(0.1f)] public float stopDistance = 1.2f;

    [Tooltip("Max duration before the brain re-evaluates.")]
    [Min(0.1f)] public float maxDuration = 1.5f;

    private Transform _target;
    private float _elapsed;

    public string DisplayName => "Charge";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _target  = context.GetTarget(EnemyContextKeys.Player);
        _elapsed = 0f;
        esm.ts.ea.RootMotionEnabled(false);
        if (animData != null) esm.ts.ea.PlayEnemyAnimation(animData.walkCycle);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;
        if (_target == null) _target = context.GetTarget(EnemyContextKeys.Player);
        if (_target == null) return;
        esm.ChargeToward(_target.position, speedMultiplier);
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm) => esm.Brake();

    public bool IsComplete(EnemyContext context, EnemyStateMachine esm)
    {
        if (_elapsed >= maxDuration || _target == null) return true;
        Vector3 delta = _target.position - esm.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= stopDistance * stopDistance;
    }
}

