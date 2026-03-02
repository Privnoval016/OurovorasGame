using Extensions.EventBus;
using UnityEngine;

// Handles events triggered by the enemy through attacks (projectiles, dashes, etc.)
public class OnEnemyEvents : MonoBehaviour
{
    [HideInInspector] public EnemyController ts;

    private void Awake()
    {
        ts = GetComponent<EnemyController>();
    }

    public void TriggerOnEnemyAction(EnemyAttackAIAction a)
    {
        IEnemyAttackStrategy strategy = a?.attack?.enemyAttack ?? new NoneEnemyAttackStrategy();
        strategy.Execute(this, a);
    }
}

