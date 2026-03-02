using UnityEngine;

/** <summary>
 * Retained for backward compatibility. All separation and slope logic is now
 * self-contained in <see cref="CharacterPhysicsBody"/> which runs its own FixedUpdate.
 * This component only holds the shared <see cref="PhysicsConfig"/> reference.
 * </summary>
 */
public class CharacterSeparationSystem : MonoBehaviour
{
    [Tooltip("Shared physics tuning parameters (currently unused — retained for future use).")]
    public PhysicsConfig config;
}
