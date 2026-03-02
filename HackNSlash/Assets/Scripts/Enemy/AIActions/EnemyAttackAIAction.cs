using Extensions.UtilityAI;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttackAIAction", menuName = "Enemy/AIActions/EnemyAttackAIAction")]
public class EnemyAttackAIAction : EnemyAIActionBase
{
    [Header("Attack Settings")]
    public EnemyAttack attack;
    public EnemyAttackDamageInfo damageInfo;
    public PlayerAttackStats stats;

    private Vector3 _targetPosition;

    protected override void OnEnemyEnter(EnemyContext context, EnemyStateMachine esm)
    {
        Transform target = context.GetTarget(EnemyContextKeys.Player);
        _targetPosition = target != null ? target.position : esm.transform.position;
        esm.ts.onEnemyEvents.TriggerOnEnemyAction(this);
    }

    protected override void OnEnemyUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        if (_targetPosition != default) esm.TurnToPosition(_targetPosition);
    }

    protected override void OnEnemyExit(EnemyContext context, EnemyStateMachine esm) { }
}