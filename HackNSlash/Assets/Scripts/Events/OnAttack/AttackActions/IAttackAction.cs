using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

public abstract class IAttackAction
{
    protected OnAttackEvents oae;
    protected PlayerController pc;
    protected PlayerAttack a;
    
    public virtual void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        oae = onAttackEvents;
        pc = oae.pc;
        a = attack;
    }
    
    public IEnumerator<float> ResumeMoving(float time, Action action = null)
    {
        yield return Timing.WaitForSeconds(time);
        
        pc.pi.isInvincible = false;
        
        action?.Invoke();
        pc.psm.canAttack = true;
    }
    
    public VFXController CreateVFX(int index, TransformInfo start = default)
    {
        return OnVFXEvents.Instance.SpawnPlayerVFX(pc, a, index, start);
    }
    
    public VFXController CreateVFX(VFXSpawnInfo vfxInfo, TransformInfo start = default)
    {
        return OnVFXEvents.Instance.SpawnParriedProjectileVFX(pc, a, vfxInfo, start);
    }
}
