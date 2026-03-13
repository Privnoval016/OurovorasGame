using Extensions.UtilityAI;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttackAIAction", menuName = "Enemy/AIActions/EnemyAttackAIAction")]
public class EnemyAttackAIAction : EnemyAIActionBase
{
    [Header("Attack Settings")]
    public EnemyAttack attack;
    public EnemyAttackDamageInfo damageInfo;
    public PlayerAttackStats stats;

    public override bool IsInterruptible  => attack.isInterruptible;

    protected override void OnEnemyEnter(EnemyContext context, EnemyStateMachine esm)
    {
        // Face the player once at the moment the attack begins.
        // Do NOT rotate every frame in OnUpdate — the strategy (e.g. DashLunge)
        // owns rotation during execution, and continuous rotation fights the dash direction.
        Transform target = context.GetTarget(EnemyContextKeys.Player);
        if (target != null) esm.TurnToPosition(target.position);

        esm.ts.onEnemyEvents.TriggerOnEnemyAction(this);
    }

    protected override void OnEnemyUpdate(EnemyContext context, EnemyStateMachine esm) { }
    protected override void OnEnemyExit(EnemyContext context, EnemyStateMachine esm) { }
}