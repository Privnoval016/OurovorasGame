using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[Serializable]
public class PlungeAttackAction : IAttackAction
{
    [Header("Plunge Attack Settings")]
    [Tooltip("Speed at which the player plunges downwards")]
    public float plungeSpeed = 75f;
    [Tooltip("Minimum time the plunge attack should last")]
    public float minPlungeTime = 0.02f;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(BeginPlungeAttack()).
            OnDestroy(() => pc.IgnoreAllCollisionsWithLayer(GameManager.Instance.enemyLayer, false));
    }
    
    private IEnumerator<float> BeginPlungeAttack()
    {
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        pc.pac.PlayAnimation(a.attackClips[0], a.animFade);
        yield return Timing.WaitForSeconds(a.attackClips[0].length);
        
        yield return Timing.WaitForSeconds(a.animDelay);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = true;
        pc.pac.PlayAnimation(a.attackClips[1], a.animFade);
        
        pc.rb.linearVelocity = Vector3.zero;

        float minAnimTime = minPlungeTime;
        float startTime = Time.time;

        Func<bool> loopCondition = () => Time.time - startTime < minAnimTime || pc.psm.IsMidair &&
            pc.psm.StandardizedMoveDir.normalized.IsInDirectionCone(a.inputDirection.normalized, 92f);
        
        oae.RunSegmentCoroutine(pc.rb.TraverseWithVelocity(Vector3.down, plungeSpeed, loopCondition));
        
        yield return Timing.WaitUntilTrue(() => !loopCondition());
        pc.rb.linearVelocity = Vector3.zero;

        if (pc.psm.IsGrounded)
        {
            pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
            oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown));
        }
        else
        {
            oae.RunSegmentCoroutine(ResumeMoving(minAnimTime));
        }
        
        
    }
}
