using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using MEC;
using UnityEngine;

[Serializable]
public class BladeBeamAttackAction : IAttackAction
{
    
    [Header("Blade Beam Settings")]
    [Tooltip("Time the key must be held to fire the blade beam")]
    public float bladeBeamHoldTime = 0.2f;
    [Tooltip("Interval between blade beam projectiles")]
    public float bladeBeamInterval = 0.3f;
    [Tooltip("Duration of the cross slash animation")]
    public float crossSlashDuration = 0.3f;
    [Tooltip("Number of targets the cross slash can hit")]
    public int numTargets = 3;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(BeginBladeBeam());
    }
    
    private IEnumerator<float> BeginBladeBeam()
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        pc.psm.pauseComboReset = true; 
        pc.psm.TurnToLook();
        
        KeyBind[] holdKeys = InputManager.GetReleaseable(a.keyBinds);
        float startTime = Time.time;
        yield return Timing.WaitUntilTrue(() => holdKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
        float elapsedTime = Time.time - startTime;

        if (elapsedTime >= bladeBeamHoldTime && pc.cam.IsLockedOn)
        {
            pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
            
            HashSet<LockOnTarget> enemies = pc.HitScanEnemies(numTargets, pc.psm.playerData.lockOnRange, pc.psm.playerData.lockOnRange, 360, a);
        
            foreach (LockOnTarget enemy in enemies)
            {
                Vector3 direction =
                    (enemy.transform.position.ZeroVector3Axis() - pc.transform.position.ZeroVector3Axis()).normalized;
                TransformInfo targetTransform = new TransformInfo(enemy.transform.position - direction * 1.5f, 
                    Quaternion.LookRotation(direction), 
                    Vector3.one);
                CreateVFX(3, targetTransform);
            }
            
            oae.RunSegmentCoroutine(ResumeMoving(crossSlashDuration, () => pc.psm.pauseComboReset = false));
        }
        else
        {
            pc.pac.PlayAnimation(a.attackClips[1], a.animFade);

            for (int i = 1; i <= 2; i++)
            {
                Vector3 startPos = pc.transform.forward.FindRadialVector3(pc.psm.playerData.mediumRadius, 0) +
                                   pc.transform.position;
                Quaternion startRot = pc.transform.rotation;
                if (pc.cam.IsLockedOn)
                {
                    Vector3 targetPos = pc.cam.TargetPosition;

                    startRot = Quaternion.LookRotation(targetPos - pc.transform.position);
                }

                CreateVFX(i, new TransformInfo(startPos, startRot, Vector3.one));
                
                yield return Timing.WaitForSeconds(bladeBeamInterval);
            }

            oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
        }
    }
}
