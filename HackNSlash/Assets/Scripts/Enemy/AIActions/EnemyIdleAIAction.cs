using Extensions.UtilityAI;
using UnityEngine;


[CreateAssetMenu(fileName = "EnemyIdleAction", menuName = "Enemy/AIActions/EnemyIdleAction", order = 0)]
public class EnemyIdleAIAction : EnemyAIActionBase
{
    protected override void OnEnemyEnter(EnemyContext context, EnemyStateMachine esm)
        => esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.idleClip);

    protected override void OnEnemyUpdate(EnemyContext context, EnemyStateMachine esm) { }
    protected override void OnEnemyExit(EnemyContext context, EnemyStateMachine esm) { }
}