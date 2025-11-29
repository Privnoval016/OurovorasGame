using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class GrappleAttackAction : IAttackAction
{
    [Header("Grapple Settings")]
    [Tooltip("Maximum time to hold the grapple")]
    public float grappleMaxTime = 0.2f;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(BeginGrapple());
    }
    
    private IEnumerator<float> BeginGrapple()
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        pc.psm.pauseComboReset = true;
        pc.psm.TurnToLook();
        
        KeyBind[] holdKeys = InputManager.GetReleaseable(a.keyBinds);
        yield return Timing.WaitForSeconds(grappleMaxTime);
        
        if (holdKeys.Any(k => !InputManager.KeyMap[k].holdAction()) || pc.psm.IsMidair)
        {
            pc.pac.PlayAnimation(a.attackClips[1], a.animFade);
            
            var vfx = CreateVFX(1);

            if (pc.cam.IsLockedOn)
            {
                pc.cam.TargetedEnemy.OnHit(pc.pcc.currentElementEffect, pc, a, pc.transform, 0);
                Services.CombatSystem.PlayHitEffects(a.element, pc, a, vfx, true, 0);
            }
            
            oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
            
        }
        else
        {
            yield return Timing.WaitUntilTrue(() => holdKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
            
            pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
            
            Vector3 startPos = pc.transform.forward.FindRadialVector3(pc.psm.playerData.mediumRadius, 0) + pc.transform.position;
            Quaternion startRot = Quaternion.LookRotation((pc.cam.TargetPosition - pc.transform.position).ZeroVector3Axis());
                
            CreateVFX(2);
            
            oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
        }
        
    }
}
