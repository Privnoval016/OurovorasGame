using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using UnityEngine;
using MEC;

[Serializable]
public class DashToTargetAttackAction : IAttackAction
{
    
    [Header("Dash Settings")]
    [Tooltip("Maximum distance the player can dash in the air")]
    public float maxAirDashDistance = 20f;
    [Tooltip("Speed of the air dash")]
    public float airDashSpeed = 80f;
    [Tooltip("Whether to play the final slash animation after the dash")]
    public bool finalAirSlash = true;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(AirDash());
    }

    IEnumerator<float> AirDash()
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        pc.pac.PlayAnimation(a.attackClips[0], a.animFade);
        yield return Timing.WaitForSeconds(a.attackClips[0].length);
        
        Quaternion originalRotation = pc.pac.animancer.gameObject.transform.rotation;
        
        pc.pac.PlayAnimation(a.attackClips[1], a.animFade);
       
        pc.rb.linearVelocity = Vector3.zero;
        float minAnimTime = 0.05f;
        float startTime = Time.time;
        Vector3 startPos = pc.transform.position;
        
        Vector3 direction = pc.cam.TargetPosition - pc.transform.position;
        
        if (direction.y > 0)
        {
            direction = direction.ZeroVector3Axis();
            direction = direction.Rotate(-45, Vector3.Cross(direction, Vector3.up));
        }
        
        float distToGround = Physics.SphereCast(pc.transform.position, pc.mainCol.radius, direction.normalized, 
            out RaycastHit hit, 100f, GameManager.Instance.groundLayer) ? hit.distance : 
            maxAirDashDistance;
        float dist = Mathf.Min(maxAirDashDistance, Mathf.Max(distToGround, direction.magnitude));
        
        Func<bool> loopCondition = () => Time.time - startTime < minAnimTime || pc.psm.IsMidair &&
            a.keyBinds.Any(k => InputManager.KeyMap[k].holdAction())
                                     && Vector3.Distance(startPos, pc.transform.position) < dist;
        

        pc.pac.animancer.gameObject.transform.LookAt(pc.transform.position + direction);
        
        pc.IgnoreAllCollisionsWithLayer(pc.cam.TargetedEnemy?.gameObject.layer, true);
        
        oae.RunSegmentCoroutine(pc.rb.TraverseWithVelocity(direction.normalized, airDashSpeed, loopCondition));
        yield return Timing.WaitUntilTrue(() => !loopCondition());
        
        pc.rb.linearVelocity = Vector3.zero;

        if (finalAirSlash) pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
        oae.RunSegmentCoroutine(ResumeMoving(!finalAirSlash ? a.hitInfo.attackCoolDown : Mathf.Max(0, a.hitInfo.attackCoolDown + a.attackClips[2].length - 0.1f)));
        
        pc.pac.animancer.gameObject.transform.rotation = originalRotation;
        pc.IgnoreAllCollisionsWithLayer(pc.cam.TargetedEnemy?.gameObject.layer, false);
        
    }
}
