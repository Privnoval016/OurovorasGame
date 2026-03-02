using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[Serializable]
public class BasicFinisherAttackAction : IAttackAction
{
    [Header("Finisher Settings")]
    
    [Tooltip("Time taken to dodge to the target")]
    public float dodgeTime = 0.8f;
    
    [Tooltip("Time scale during the finisher attack")]
    public float finisherTimeScale = 0.7f;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(BeginBasicFinisher(pc, a));
    }
    
    private IEnumerator<float> BeginBasicFinisher(PlayerController pc, PlayerAttack a)
    {
        #region Initial Setup
        
        LockOnTarget target = pc.psm.NearestHEnemy;
        
        if (target == null)
        {
            Debug.LogWarning("No target found for finisher!");
            yield break;
        }
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        pc.cam.FinisherTarget = target;
        pc.cam.SwitchState(PlayerCamStates.FinisherCloseUp);
        
        pc.psm.SwapToUltimate();
        
        #endregion
        
        #region Move to Target

        Vector3 teleportedPosition = pc.cam.FinisherTarget.transform.position +
                                     (pc.transform.position - pc.cam.FinisherTarget.transform.position)
                                     .ZeroVector3Axis().normalized * pc.psm.playerData.mediumRadius;

        Vector3 dodgeDirection = teleportedPosition - pc.transform.position;

        float distance = dodgeDirection.magnitude;
        dodgeDirection.Normalize();

        float moveTime = dodgeTime / 2;


        (pc.cam.TargetedEnemy as PhysicsEnemy)?.ResetMovement();
        
        pc.pac.PlayAnimation(a.attackTransitions[0]);
        
        
        oae.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        yield return Timing.WaitForSeconds(moveTime);
        
        #endregion
        
        #region Perform Finisher
        
        float timeScale = finisherTimeScale != 0 ? finisherTimeScale : 1;
        
        Services.Get<CombatSystem>().ApplySlowedTimescale(true, timeScale);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = true;
        
        pc.pac.PlayAnimation(a.attackClips[0], a.animFade);

        yield return Timing.WaitForSeconds(a.attackClips[0].length / timeScale);
        
        Services.Get<CombatSystem>().ApplySlowedTimescale(false);
        
        #endregion
        
        #region Final Setup
        
        pc.psm.SwapToUltimate();
        
        // ForceExitToDefault restores invincibility and clears FinisherTarget safely.
        pc.cam.ForceExitToDefault();
        
        oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown));
        
        #endregion
        
    }
}
