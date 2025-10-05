using Extensions.UtilityAI;
using MoreMountains.Tools;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyMoveAIAction", menuName = "Enemy/AIActions/EnemyMoveAIAction", order = 0)]
public class EnemyMoveAIAction : EnemyAIActionBase
{
    public float moveSpeed = 2f;

    protected override void OnEnemyEnter(Context<EnemyAIContextKey> enemyContext, EnemyStateMachine esm)
    {
        
    }

    protected override void OnEnemyUpdate(Context<EnemyAIContextKey> enemyContext, EnemyStateMachine esm)
    {
        var target = enemyContext.Sensor.GetNearestDetectedObject(actionKey);
        if (target == null) return;
        
        // TODO: Move towards the target
    }
    
    protected override void OnEnemyExit(Context<EnemyAIContextKey> enemyContext, EnemyStateMachine esm)
    {
        
    }
}