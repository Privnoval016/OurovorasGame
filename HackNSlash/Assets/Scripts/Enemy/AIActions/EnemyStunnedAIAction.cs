using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * AI action that fully immobilises the enemy — no movement and rotation is locked.
 * This is the canonical implementation for shield-break stun and similar vulnerability windows.
 *
 * The action completes when <see cref="exitCondition"/> is satisfied:
 * <list type="bullet">
 *   <item><see cref="ExitCondition.Duration"/> — exits after a fixed time (useful for testing).</item>
 *   <item><see cref="ExitCondition.ShieldRestored"/> — exits when the shield percentage rises
 *         back above zero (correct for Calliope's shield-break stun).</item>
 *   <item><see cref="ExitCondition.Never"/> — never self-completes; relies on the brain
 *         overriding it when the gate consideration returns 0.</item>
 * </list>
 *
 * Because the consideration gate returns an overwhelming score (e.g. 10) while the shield is
 * broken, and 0 the moment it is restored, the brain will immediately switch away as soon
 * as the shield restores — no special callback is required.
 * </summary>
 */
[CreateAssetMenu(fileName = "EnemyStunnedAction", menuName = "Enemy/AIActions/EnemyStunnedAction")]
public sealed class EnemyStunnedAIAction : EnemyAIActionBase
{
    public enum ExitCondition { Duration, ShieldRestored, Never }

    [Header("Stun Settings")]
    [Tooltip("How the stun decides it is complete.")]
    public ExitCondition exitCondition = ExitCondition.ShieldRestored;

    [Tooltip("Duration in seconds — only used when exitCondition = Duration.")]
    [Min(0f)] public float duration = 2f;

    [Tooltip("Optional animation to play on enter. Leave null to keep whatever is playing.")]
    public Animancer.ClipTransition stunClip;

    private float _elapsed;

    protected override void OnEnemyEnter(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed = 0f;
        esm.StopMovement();
        esm.Brake();
        if (stunClip != null) esm.ts.ea.PlayEnemyAnimation(stunClip);
    }

    protected override void OnEnemyUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;
        // Continuously kill velocity so physics cannot push the enemy while stunned.
        esm.StopMovement();
    }

    protected override void OnEnemyExit(EnemyContext context, EnemyStateMachine esm) { }
}



