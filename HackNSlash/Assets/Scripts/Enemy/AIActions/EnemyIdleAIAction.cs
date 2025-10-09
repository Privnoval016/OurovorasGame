
using Extensions.UtilityAI;
using UnityEngine;


[CreateAssetMenu(fileName = "EnemyIdleAction", menuName = "Enemy/AIActions/EnemyIdleAction", order = 0)]
public class EnemyIdleAIAction : EnemyAIActionBase
{
    protected override void OnEnemyEnter(Context<EnemyAIContextKey> context, EnemyStateMachine esm)
    {
        esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.idleClip);
    }

    protected override void OnEnemyUpdate(Context<EnemyAIContextKey> enemyContext, EnemyStateMachine esm)
    {
        
    }
    
    
    protected override void OnEnemyExit(Context<EnemyAIContextKey> context, EnemyStateMachine esm)
    {
        
    }
}