using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Defines a single discrete movement behaviour that an <see cref="EnemyMovementAIAction"/>
 * can delegate its per-frame motion to.
 *
 * Animation data is passed in via <see cref="OnEnter"/> rather than read from the esm
 * directly, so one action asset can be used on enemies with different rigs.
 * </summary>
 */
public interface IMovementStrategy
{
    /** <summary>
     * Called once when the action is entered.
     * <paramref name="animData"/> is the resolved animation data for this execution —
     * prefer it over <c>esm.enemyAnimData</c> so the action is model-agnostic.
     * </summary>
     */
    void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData);

    /** <summary>Called every Update tick. Drive motor output here.</summary> */
    void OnUpdate(EnemyContext context, EnemyStateMachine esm);

    /** <summary>Called once when the action exits. Clean up motor state here.</summary> */
    void OnExit(EnemyContext context, EnemyStateMachine esm);

    /** <summary>Returns true when this strategy is done and the brain should re-evaluate.</summary> */
    bool IsComplete(EnemyContext context, EnemyStateMachine esm);

    /** <summary>Human-readable name shown in the Brain Debugger.</summary> */
    string DisplayName { get; }
}
