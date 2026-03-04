using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Defines a single discrete movement behaviour that an <see cref="EnemyMovementAIAction"/>
 * can delegate its per-frame motion to.
 *
 * Strategies are plain serializable classes — no MonoBehaviour, no ScriptableObject.
 * They hold only parameters and stateless-ish per-frame logic; any state that must
 * survive across frames (timers, cached targets) should be stored on the strategy itself
 * since a new instance is created for every action execution.
 * </summary>
 */
public interface IMovementStrategy
{
    /** <summary>Called once when the action is entered. Cache targets and reset state here.</summary> */
    void OnEnter(EnemyContext context, EnemyStateMachine esm);

    /** <summary>Called every Update tick. Drive motor output here.</summary> */
    void OnUpdate(EnemyContext context, EnemyStateMachine esm);

    /** <summary>Called once when the action exits. Clean up motor state here.</summary> */
    void OnExit(EnemyContext context, EnemyStateMachine esm);

    /** <summary>
     * Returns true when this strategy considers its work complete and the action
     * should be released back to the AI brain for re-evaluation.
     * </summary>
     */
    bool IsComplete(EnemyContext context, EnemyStateMachine esm);

    /** <summary>Human-readable name shown in the Brain Debugger and action inspector.</summary> */
    string DisplayName { get; }
}

