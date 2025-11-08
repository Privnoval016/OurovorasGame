using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using MEC;
using UnityEngine;

[Serializable]
public class FloorDashAttackAction : IAttackAction
{
    [Header("Dash Attack Settings")]
    [Tooltip("Time taken to complete the dash")]
    public float groundDashTime = 0.2f;
    [Tooltip("Distance to dash")]
    public float groundDashDistance = 25f;
    [Tooltip("Minimum time holding the dash key to perform a longer dash")]
    public float groundDashTimeThreshold = 0.4f;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(DashAttack()).
            OnDestroy(() => pc.IgnoreAllCollisionsWithLayer(GameManager.Instance.enemyLayer, false));
    }
    
     IEnumerator<float> DashAttack()
    {
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        yield return Timing.WaitForSeconds(a.animDelay);
        
        KeyBind[] releaseKeys = InputManager.GetReleaseable(a.keyBinds);
        
        float startTime = Time.time;
        
        yield return Timing.WaitUntilTrue(() => releaseKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
        
        float elapsedTime = Time.time - startTime;
        
        
        pc.pac.ExitTimeAnimation(a.attackClips[1], a.attackClips[2]);


        Vector3 direction = (pc.cam.TargetPosition - pc.transform.position).
            WithY(Mathf.Min(pc.cam.TargetPosition.y, pc.transform.position.y) - pc.transform.position.y);
        pc.transform.LookAt(pc.cam.TargetPosition.WithY(pc.transform.position.y));
        
        float distance;
        RaycastHit[] collidersInPath = null;

        if (elapsedTime >= groundDashTimeThreshold)
        {
            distance = Mathf.Max(Mathf.Min(groundDashDistance * 2, direction.magnitude * 2), groundDashDistance * 0.8f);

            collidersInPath = Physics.SphereCastAll(pc.transform.position, pc.mainCol.radius, 
                direction, distance * 2f, GameManager.Instance.enemyLayer);

            foreach (var hit in collidersInPath)
            {
               pc.IgnoreAllCollisionsWithLayer(hit.collider.gameObject.layer, true);
            }
            pc.IgnoreAllCollisionsWithLayer(pc.cam.TargetedEnemy?.gameObject.layer, true);
        }
        else
        {
            ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = true;
            distance = Mathf.Min(groundDashDistance, direction.magnitude);
        }
        
        yield return Timing.WaitUntilDone(pc.rb.TraverseDistanceInTime(direction.normalized, distance, groundDashTime));
        

        pc.pac.PlayAnimation(a.attackClips[3], a.animFade);
        
        
        oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown));
        
        if (collidersInPath != null)
        {
            foreach (var hit in collidersInPath)
            {
                if (hit.collider.TryGetComponent(out LockOnTarget d)) d.OnHit(pc.pcc.currentElementEffect, pc, a, pc.transform, 1);
                pc.IgnoreAllCollisionsWithLayer(hit.collider.gameObject.layer, false);
            }
        }
        pc.IgnoreAllCollisionsWithLayer(pc.cam.TargetedEnemy?.gameObject.layer, false);
    }
}
