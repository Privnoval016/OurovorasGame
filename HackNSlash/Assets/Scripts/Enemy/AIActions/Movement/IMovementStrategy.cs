using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Defines a single discrete movement behaviour for <see cref="EnemyMovementAIAction"/>.
 * Animation data is passed in from the enemy instance so the same action asset
 * works on any enemy prefab regardless of rig.
 * </summary>
 */
public interface IMovementStrategy
{
    void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData);
    void OnUpdate(EnemyContext context, EnemyStateMachine esm);
    void OnExit(EnemyContext context, EnemyStateMachine esm);
    bool IsComplete(EnemyContext context, EnemyStateMachine esm);
    string DisplayName { get; }
}
