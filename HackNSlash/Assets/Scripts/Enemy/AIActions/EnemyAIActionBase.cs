using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Base class for all enemy AI actions.
 * Bridges <see cref="AIActionBase"/> with the concrete <see cref="EnemyStateMachine"/> API.
 * </summary>
 */
public abstract class EnemyAIActionBase : AIActionBase
{
    [Header("Enemy AI Action Settings")]
    [field: SerializeField] public bool AggroedAction { get; private set; } = true;

    public void OnEnter(EnemyContext context, EnemyStateMachine esm) => OnEnemyEnter(context, esm);
    public void OnUpdate(EnemyContext context, EnemyStateMachine esm) => OnEnemyUpdate(context, esm);
    public void OnExit(EnemyContext context, EnemyStateMachine esm) => OnEnemyExit(context, esm);

    protected abstract void OnEnemyEnter(EnemyContext context, EnemyStateMachine esm);
    protected abstract void OnEnemyUpdate(EnemyContext context, EnemyStateMachine esm);
    protected abstract void OnEnemyExit(EnemyContext context, EnemyStateMachine esm);
}