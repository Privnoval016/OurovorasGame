using Extensions.StateMachine;
using UnityEngine;

[RequireComponent(typeof(StateController<EnemyState>))]
public class StandardEnemy : PhysicsEnemy
{
    #region State Machine
    [HideInInspector] public StateController<EnemyState> sc;
    #endregion

    public override void OnStart() 
    {
        base.OnStart();
        
        sc = new StateController<EnemyState>(this);
        sc.ChangeState(new EnemyFollow());
    }
}
