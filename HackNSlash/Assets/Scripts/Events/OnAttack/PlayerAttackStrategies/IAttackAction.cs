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
    
    protected IEnumerator<float> ResumeMoving(float time, Action action = null)
    {
        yield return Timing.WaitForSeconds(time);
        
        pc.ps.isInvincible = false;
        
        action?.Invoke();
        pc.psm.canAttack = true;
    }
    
    protected VFXController CreateVFX(int index, TransformInfo start = default)
    {
        return OnVFXEvents.Instance.SpawnPlayerVFX(pc, a, index, start);
    }
    
    protected VFXController CreateVFX(VFXSpawnInfo vfxInfo, TransformInfo start = default)
    {
        return OnVFXEvents.Instance.SpawnParriedProjectileVFX(pc, a, vfxInfo, start);
    }
}
