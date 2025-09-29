using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;


public class OnAttackEvents : MonoBehaviour
{
    [HideInInspector] public PlayerController pc;
    
    private void Awake()
    {
        pc = GetComponent<PlayerController>();
    }
    
    public void InvokeOnAttack(PlayerAttack a)
    {
        foreach (AttackActionInfo info in a.attackActions)
        {
            if (info.attackAction == null) continue;
            this.RunSegmentCoroutine(PlayAttackAction(info.attackAction, a));
        }
    }

    private IEnumerator<float> PlayAttackAction(IAttackAction action, PlayerAttack a)
    {
        action.Execute(this, a);
        
        yield return Timing.WaitForOneFrame;
    }
}
