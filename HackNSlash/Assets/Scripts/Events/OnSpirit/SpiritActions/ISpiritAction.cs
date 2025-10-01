using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

public abstract class ISpiritAction
{
    protected OnSpiritEvents ose;
    protected ElementalSpirit spirit;
    protected SpiritAttack a;
    
    public virtual void Execute(OnSpiritEvents onSpiritEvents, SpiritAttack attack)
    {
        ose = onSpiritEvents;
        spirit = ose.spirit;
        a = attack;
    }
    
    public IEnumerator<float> ResumeMoving(float time, Action action = null)
    {
        yield return Timing.WaitForSeconds(time);
        action?.Invoke();
        spirit.canAttack = true;
    }
    
    public VFXController CreateVFX(int i, TransformInfo overrideTransform = default)
    {
        return OnVFXEvents.Instance.SpawnPlayerVFX(spirit.pc, a, i, overrideTransform);
    }
}
