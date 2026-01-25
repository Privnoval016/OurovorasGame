using Extensions.UtilityAI;
using UnityEngine;

public abstract class EnemyAIActionBase : AIAction<EnemyAIContextKey>
{
    [Header("Enemy AI Action Settings")]
    [field: SerializeField] public bool AggroedAction { get; private set; } = true;
    
    public void OnEnter(Context<EnemyAIContextKey> context, EnemyStateMachine esm)
    {
        OnEnemyEnter(context, esm);
    }
    
    public void OnUpdate(Context<EnemyAIContextKey> context, EnemyStateMachine esm)
    {
        OnEnemyUpdate(context, esm);
    }
    
    public void OnExit(Context<EnemyAIContextKey> context, EnemyStateMachine esm)
    {
        OnEnemyExit(context, esm);
    }
    
    protected abstract void OnEnemyEnter(Context<EnemyAIContextKey> context, EnemyStateMachine esm);
    protected abstract void OnEnemyUpdate(Context<EnemyAIContextKey> context, EnemyStateMachine esm);
    
    protected abstract void OnEnemyExit(Context<EnemyAIContextKey> context, EnemyStateMachine esm);
}