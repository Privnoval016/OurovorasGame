using Extensions.StateMachine;
using UnityEngine;

public class SpiritState : State
{
    protected ElementalSpirit spirit;
    protected StateController<SpiritState> sc;

    public override void OnStateEnter(MonoBehaviour parent)
    {
        spirit = parent as ElementalSpirit;
        sc = spirit.sc;
        
        OnEnter();
    }
}
