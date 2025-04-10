using Extensions.StateMachine;
using UnityEngine;

public abstract class EnemyState : State
{
    protected StandardEnemy ec;
    protected StateController<EnemyState> sc;
    
    public override void OnStateEnter(MonoBehaviour parent)
    {
        ec = parent as StandardEnemy;
        sc = ec.sc;
        
        OnEnter();
    }
}
