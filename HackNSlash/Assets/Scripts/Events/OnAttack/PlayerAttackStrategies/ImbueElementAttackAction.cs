using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[Serializable]
public class ImbueElementAttackAction : IAttackAction
{
    
    [Header("Imbue Element Settings")]
    [Tooltip("Duration of the imbued element effect")]
    public float imbueElementDuration = 10f;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);

        oae.RunSegmentCoroutine(BeginImbueElement());
    }
    
    private IEnumerator<float> BeginImbueElement()
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        if (pc.pcc.currentElementEffect == ElementEffect.None) yield break;
        
        pc.KillObjectCoroutines(nameof(ResetImbuedElement));

        float delay = a.hitInfo.attackCoolDown;

        if (pc.pcc.imbuedElementEffect != pc.pcc.currentElementEffect)
        {
            pc.pac.PlayAnimation(a.attackClips[pc.psm.IsMidair ? 1 : 0], a.animFade);
            pc.pcc.imbuedElementEffect = pc.pcc.currentElementEffect;
        }
        else
        {
            delay = 0;
            pc.pcc.imbuedElementEffect = ElementEffect.None;
        }
        
        pc.RunSegmentCoroutine(ResetImbuedElement(imbueElementDuration), nameof(ResetImbuedElement));

        oae.RunSegmentCoroutine(ResumeMoving(delay));
    }
    
    private IEnumerator<float> ResetImbuedElement(float time)
    {
        yield return Timing.WaitForSeconds(time);
        pc.pcc.imbuedElementEffect = ElementEffect.None;
        pc.wc.DeactivateAllWeaponVFX();
    }
}
