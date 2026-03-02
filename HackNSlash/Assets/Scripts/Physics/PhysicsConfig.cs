using UnityEngine;

/** <summary>
 * Central ScriptableObject that houses all shared physics tuning parameters
 * for the hack-and-slash character physics layer.
 * </summary>
 * <remarks>
 * Create via <c>Assets > Create > Physics > PhysicsConfig</c>.
 * Assign to <see cref="CharacterSeparationSystem"/>, <see cref="SlopeHandler"/>,
 * and <see cref="ArenaBoundary"/> from the inspector.
 * </remarks>
 */
[CreateAssetMenu(menuName = "Physics/PhysicsConfig", fileName = "PhysicsConfig")]
public class PhysicsConfig : ScriptableObject
{

    #region Separation

    [Header("Body Separation")]
    [Tooltip("Maximum lateral correction speed in m/s applied when two characters overlap. " +
             "Higher values push bodies apart more aggressively. Start at 4.")]
    public float separationCorrectionSpeed = 4f;

    #endregion

    #region Slope

    [Header("Slope Handling")]
    [Tooltip("Maximum angle (degrees) of a surface the player is allowed to walk on. " +
             "Steeper surfaces slide the character down.")]
    [Range(0f, 89f)]
    public float maxWalkableSlopeAngle = 40f;

    [Tooltip("Acceleration (m/s²) applied along the slope fall-line when on a surface " +
             "steeper than maxWalkableSlopeAngle.")]
    public float slopeSlideForce = 12f;

    [Tooltip("Distance of the sphere cast used to detect the ground normal beneath the character.")]
    public float groundProbeDistance = 0.35f;

    [Tooltip("Radius of the sphere cast for ground normal detection – should roughly match the capsule radius.")]
    public float groundProbeRadius = 0.3f;

    #endregion

    #region Knockback Clamping

    [Header("Knockback Clamping")]
    [Tooltip("Maximum horizontal speed an enemy can reach from any single knockback event.")]
    public float maxEnemyKnockbackSpeedH = 18f;

    [Tooltip("Maximum vertical speed an enemy can reach from any single knockback event.")]
    public float maxEnemyKnockbackSpeedV = 14f;

    #endregion

    #region Boundary

    [Header("Arena Boundary")]
    [Tooltip("Distance from the boundary wall at which the proximity shader effect begins.")]
    public float boundaryProximityShaderDistance = 2.5f;

    [Tooltip("Height of the invisible boundary collider wall.")]
    public float boundaryWallHeight = 8f;

    [Tooltip("Thickness of each boundary wall segment.")]
    public float boundaryWallThickness = 0.3f;

    #endregion
}
