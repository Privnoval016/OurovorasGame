using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using UnityEngine;
using MEC;

public enum OnSpiritActions
{
    None,
    RangedAttack,
    SpawnVFX,
    HitScanVFX
}

public class OnSpiritEvents : MonoBehaviour
{
    [HideInInspector] public ElementalSpirit spirit;
    
    private void Awake()
    {
        spirit = GetComponent<ElementalSpirit>();
    }
    
    public void InvokeOnSpiritAction(SpiritAttack a)
    {
        foreach (SpiritActionInfo actionInfo in a.spiritActions)
        {
            if (actionInfo.spiritAction == null) continue;
            this.RunSegmentCoroutine(PlaySpiritAction(actionInfo.spiritAction, a));
        }
    }
    
    private IEnumerator<float> PlaySpiritAction(ISpiritAction action, SpiritAttack a)
    {
        action.Execute(this, a);
        
        yield return Timing.WaitForOneFrame;
    }
}
