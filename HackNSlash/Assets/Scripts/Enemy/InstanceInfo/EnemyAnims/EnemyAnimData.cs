using Animancer;
using UnityEngine;

/** <summary>
 * All animation data for an enemy instance.
 * Assigned on the enemy prefab's <see cref="EnemyStateMachine"/> and passed
 * into every movement strategy and action so the action assets themselves
 * remain model-agnostic.
 * </summary>
 */
[CreateAssetMenu(fileName = "EnemyAnimData", menuName = "Enemy/EnemyAnimData", order = 1)]
public class EnemyAnimData : ScriptableObject
{
    [Header("Idle")]
    public ClipTransition idleClip;

    [Header("Walking / Chase")]
    [Tooltip("Start / loop / end clips for forward walking. Used by Chase and Charge strategies.")]
    public AnimLoop walkCycle;

    [Tooltip("Animancer parameter name driven by normalised XZ speed to scale the walk cycle playback rate. " +
             "Leave blank to skip speed scaling.")]
    public StringAsset walkSpeedParam;

    [Tooltip("Actual world speed (m/s) that corresponds to the walk animation's authored playback rate (Speed=1). " +
             "AnimState.Speed = actualSpeed / walkSpeedReference. Tune so feet don't slide.")]
    [Min(0.1f)] public float walkSpeedReference = 3f;

    [Header("Strafe / Orbit")]
    [Tooltip("2D blend-tree loop for strafing motion. MoveX/MoveZ are driven by the lateral direction " +
             "relative to the enemy's forward so all four cardinal strafe directions blend correctly.")]
    public AnimMixerLoop strafeLoop;

    [Tooltip("Animancer parameter name for the strafe blend tree's horizontal (X) axis.")]
    public StringAsset strafeMoveXParam;

    [Tooltip("Animancer parameter name for the strafe blend tree's vertical (Z) axis.")]
    public StringAsset strafeMoveZParam;

    [Tooltip("Fallback simple loop used for orbiting when a full 2D blend tree is not needed. " +
             "If null, strafeLoop is used instead.")]
    public AnimLoop orbitLoop;

    [Header("Retreat")]
    [Tooltip("Loop played while the enemy is backing away from the player.")]
    public AnimLoop retreatLoop;

    [Header("Hit / Stagger")]
    public ClipTransition groundHitClip;
    public ClipTransition airHitClip;
    public ClipTransition getUpClip;
    public ClipTransition staggerClip;

    // ── Fallback helpers ─────────────────────────────────────────────────────

    /** <summary>
     * Returns the first <see cref="ITransition"/> from <paramref name="candidates"/>
     * whose inner <see cref="AnimationClip"/> is actually assigned, skipping any
     * whose clip is null (unassigned in the inspector).
     * Prevents the Animancer <c>ArgumentException: ClipTransition.Clip is null</c>.
     * </summary>
     */
    public static ITransition SafeResolve(params ITransition[] candidates)
    {
        foreach (var t in candidates)
        {
            if (t == null) continue;
            if (t is ClipTransition ct && ct.Clip == null) continue;
            return t;
        }
        return null;
    }

    /** <summary>Returns the best available walk-cycle clip: <c>walkCycle</c>.</summary> */
    public ITransition WalkClip() => SafeResolve(walkCycle?.LoopClip);

    /** <summary>Returns the best available orbit clip: <c>orbitLoop → strafeLoop → walkCycle</c>.</summary> */
    public ITransition OrbitClip() => SafeResolve(
        orbitLoop?.LoopClip,
        strafeLoop?.LoopClip,
        walkCycle?.LoopClip);

    /** <summary>Returns the best available strafe clip: <c>strafeLoop → walkCycle</c>.</summary> */
    public ITransition StrafeClip() => SafeResolve(
        strafeLoop?.LoopClip,
        walkCycle?.LoopClip);

    /** <summary>Returns the best available retreat clip: <c>retreatLoop → walkCycle</c>.</summary> */
    public ITransition RetreatClip() => SafeResolve(
        retreatLoop?.LoopClip,
        walkCycle?.LoopClip);
}
