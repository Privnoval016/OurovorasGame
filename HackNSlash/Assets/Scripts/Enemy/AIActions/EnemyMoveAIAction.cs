using Extensions.UtilityAI;
using MoreMountains.Tools;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyMoveAIAction", menuName = "Enemy/AIActions/EnemyMoveAIAction", order = 0)]
public class EnemyMoveAIAction : EnemyAIActionBase
{
    public float moveSpeed = 2f;
    
    private Transform target;

    protected override void OnEnemyEnter(Context<EnemyAIContextKey> enemyContext, EnemyStateMachine esm)
    {
        target = enemyContext.Sensor.GetNearestDetectedObject(actionKey);
        esm.ts.ea.RootMotionEnabled(false);
        esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.walkCycle);
    }

    protected override void OnEnemyUpdate(Context<EnemyAIContextKey> enemyContext, EnemyStateMachine esm)
    {
        if (target == null) return;
        
        MoveTowardsTarget(esm);
    }
    
    protected override void OnEnemyExit(Context<EnemyAIContextKey> enemyContext, EnemyStateMachine esm)
    {
        
    }
    
    private void MoveTowardsTarget(EnemyStateMachine esm)
    {
        if (target == null) return;
        
        esm.MoveInDirection(esm.ts.nav.CalculateDirectionToTarget(target.position), esm.enemyData.speed);
    }
}