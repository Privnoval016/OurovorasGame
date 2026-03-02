using System.Collections.Generic;
using UnityEngine;

/** <summary>
 * Per-character physics component.
 * <list type="number">
 *   <item>Disables PhysX collisions between all characters so they cannot push
 *         each other or stand on each other.</item>
 *   <item>Every FixedUpdate, enforces XZ separation by directly correcting position
 *         so characters act as walls but never transfer force.</item>
 *   <item>Blocks movement up steep slopes by zeroing into-wall velocity and sliding downhill.</item>
 * </list>
 * </summary>
 */
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(32000)]
public class CharacterPhysicsBody : MonoBehaviour
{
    public static readonly List<CharacterPhysicsBody> Registry = new();

    #region Inspector

    [Header("Separation")]
    [Tooltip("Multiplier on the widest capsule radius for the separation bubble.")]
    [Min(0.1f)] public float radiusMultiplier = 1.15f;

    [Header("Slope")]
    [Tooltip("Enable slope blocking.")]
    public bool enableSlopeDetection = false;

    [Tooltip("Layer mask for ground detection.")]
    public LayerMask groundMask;

    [Tooltip("SphereCast origin offset. Y ≈ half capsule height.")]
    public Vector3 probeOriginOffset = new Vector3(0f, 0.9f, 0f);

    [Tooltip("Max walkable slope angle in degrees.")]
    [Range(0f, 89f)] public float maxSlopeAngle = 40f;

    [Tooltip("Downhill slide acceleration when on a steep slope.")]
    public float slopeSlideForce = 12f;

    [Tooltip("SphereCast radius for ground probe.")]
    public float probeRadius = 0.3f;

    [Tooltip("SphereCast distance for ground probe.")]
    public float probeDistance = 0.35f;

    #endregion

    #region State

    public Rigidbody Rb { get; private set; }
    public float EffectiveRadius { get; private set; }

    // All non-trigger colliders on every character, for IgnoreCollision.
    private readonly List<Collider> _myColliders = new();

    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        RefreshRadius();
    }

    private void OnEnable()
    {
        CacheColliders();
        // Disable PhysX collisions between this character and every existing one.
        // This prevents the contact solver from transferring force or allowing stacking.
        foreach (CharacterPhysicsBody other in Registry)
        {
            if (other == this) continue;
            SetIgnoreCollision(other, true);
        }
        Registry.Add(this);
    }

    private void OnDisable()
    {
        Registry.Remove(this);
        foreach (CharacterPhysicsBody other in Registry)
            SetIgnoreCollision(other, false);
    }

    private void FixedUpdate()
    {
        EnforceSeparation();
        EnforceSlopeBlock();
    }

    #endregion

    #region Separation

    /** <summary>
     * For every other character within the separation bubble, push our position
     * apart so we can never overlap. No velocity is added to the other character.
     * PhysX collisions are disabled between characters, so this is the only thing
     * keeping them apart — and since it only moves position, it cannot shove.
     * </summary>
     */
    private void EnforceSeparation()
    {
        for (int i = 0; i < Registry.Count; i++)
        {
            CharacterPhysicsBody other = Registry[i];
            if (other == this || other.Rb == null) continue;

            Vector3 meToOther = other.Rb.position - Rb.position;
            meToOther.y = 0f;
            float hDist = meToOther.magnitude;
            float minDist = EffectiveRadius + other.EffectiveRadius;

            if (hDist >= minDist) continue;

            Vector3 awayDir = hDist > 0.001f ? -meToOther / hDist : Vector3.right;
            Vector3 towardDir = -awayDir;

            // Check if we are moving toward the other character.
            Vector3 vel = Rb.linearVelocity;
            float towardSpeed = vel.x * towardDir.x + vel.z * towardDir.z;

            if (towardSpeed > 0f)
            {
                // We are the one approaching — push ourselves back the full overlap.
                float overlap = minDist - hDist;
                Rb.position += awayDir * (overlap + 0.001f);

                // Zero our velocity toward them.
                vel.x -= towardDir.x * towardSpeed;
                vel.z -= towardDir.z * towardSpeed;
                Rb.linearVelocity = vel;
            }
            else if (hDist < minDist * 0.5f)
            {
                // We are not moving toward them but deeply overlapping (spawned inside, etc).
                // Push apart by half — both will do this so total = full overlap.
                float overlap = minDist - hDist;
                Rb.position += awayDir * (overlap * 0.5f + 0.001f);
            }
        }
    }

    #endregion

    #region Slope

    /** <summary>
     * If on a slope steeper than <see cref="maxSlopeAngle"/>, zero horizontal
     * velocity into the wall and apply a downhill slide force.
     * </summary>
     */
    private void EnforceSlopeBlock()
    {
        if (!enableSlopeDetection || groundMask == 0) return;

        Vector3 origin = Rb.position + probeOriginOffset;
        if (!Physics.SphereCast(origin, probeRadius, Vector3.down, out RaycastHit hit,
                probeDistance, groundMask, QueryTriggerInteraction.Ignore))
            return;

        float angle = Vector3.Angle(Vector3.up, hit.normal);
        if (angle <= maxSlopeAngle) return;

        Vector3 wallDir = new Vector3(-hit.normal.x, 0f, -hit.normal.z);
        if (wallDir.sqrMagnitude < 0.0001f) return;
        wallDir.Normalize();

        // Zero velocity into the wall.
        Vector3 vel = Rb.linearVelocity;
        float into = vel.x * wallDir.x + vel.z * wallDir.z;
        if (into > 0f)
        {
            vel.x -= wallDir.x * into;
            vel.z -= wallDir.z * into;
            Rb.linearVelocity = vel;
        }

        // Slide downhill.
        Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
        Rb.AddForce(slideDir * slopeSlideForce, ForceMode.Acceleration);
    }

    #endregion

    #region Helpers

    private void CacheColliders()
    {
        _myColliders.Clear();
        foreach (Collider c in GetComponentsInChildren<Collider>())
            if (!c.isTrigger)
                _myColliders.Add(c);
    }

    private void RefreshRadius()
    {
        float maxR = 0.4f;
        foreach (var c in GetComponents<CapsuleCollider>())
            if (!c.isTrigger && c.radius > maxR) maxR = c.radius;
        EffectiveRadius = maxR * radiusMultiplier;
    }

    /** <summary>
     * Enable or disable PhysX collisions between all body colliders on this
     * character and all body colliders on <paramref name="other"/>.
     * </summary>
     */
    private void SetIgnoreCollision(CharacterPhysicsBody other, bool ignore)
    {
        foreach (Collider mine in _myColliders)
            foreach (Collider theirs in other._myColliders)
                if (mine != null && theirs != null)
                    Physics.IgnoreCollision(mine, theirs, ignore);
    }

    #endregion

    #region Gizmos
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;
        Gizmos.color = Color.yellow;
        Vector3 p = rb.position;
        const int seg = 24;
        float step = 360f / seg * Mathf.Deg2Rad;
        Vector3 prev = p + new Vector3(EffectiveRadius, 0, 0);
        for (int i = 1; i <= seg; i++)
        {
            float a = i * step;
            Vector3 next = p + new Vector3(Mathf.Cos(a) * EffectiveRadius, 0,
                Mathf.Sin(a) * EffectiveRadius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
    #endregion
}
