using System.Collections;
using System.Collections.Generic;
using Extensions.StateMachine;
using UnityEngine;

public abstract class PlayerState : State
{
    protected PlayerController pc;
    protected StateController<PlayerState> sc;
    
    public override void OnStateEnter(MonoBehaviour parent)
    {
        pc = parent as PlayerController;
        sc = pc.sc;
        
        OnEnter();
    }
    
}