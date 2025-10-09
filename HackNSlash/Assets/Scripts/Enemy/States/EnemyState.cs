using Extensions.StateMachine;
using UnityEngine;

public abstract class EnemyState : State
{
    protected EnemyStateMachine esm;
    protected StateController<EnemyState> sc;
    
    public override void OnStateEnter(MonoBehaviour parent)
    {
        esm = parent as EnemyStateMachine;
        sc = esm.sc;
        
        OnEnter();
    }
}
