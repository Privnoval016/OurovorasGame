using Extensions.StateMachine;
using UnityEngine;

public abstract class EnemyState : State
{
    protected EnemyStateMachine ec;
    protected StateController<EnemyState> sc;
    
    public override void OnStateEnter(MonoBehaviour parent)
    {
        ec = parent as EnemyStateMachine;
        sc = ec.sc;
        
        OnEnter();
    }
}
