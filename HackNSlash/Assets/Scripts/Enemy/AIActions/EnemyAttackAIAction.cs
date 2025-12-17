
using Extensions.EventBus;
using Extensions.UtilityAI;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttackAIAction", menuName = "Enemy/AIActions/EnemyAttackAIAction", order = 0)]
public class EnemyAttackAIAction : EnemyAIActionBase
{
    public EnemyAttack attack;
    
    public EnemyAttackDamageInfo damageInfo;

    public PlayerAttackStats stats;
    
    private Vector3 targetPosition;
    
    protected override void OnEnemyEnter(Context<EnemyAIContextKey> enemyContext, EnemyStateMachine esm)
    {
        Debug.Log($"Enemy {esm.ts.name} is attacking with {name}");
        
        targetPosition = enemyContext.Sensor.GetNearestDetectedObject(actionKey).position;
        
        esm.ts.onEnemyEvents.TriggerOnEnemyAction(this);
    }
    
    protected override void OnEnemyUpdate(Context<EnemyAIContextKey> enemyContext, EnemyStateMachine esm)
    {
        if (targetPosition != default) esm.TurnToPosition(targetPosition);
    }
    
    protected override void OnEnemyExit(Context<EnemyAIContextKey> enemyContext, EnemyStateMachine esm)
    {
        
    }
}