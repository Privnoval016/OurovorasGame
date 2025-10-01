using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class PushUntilDistanceHitAction : IHitAction
{
    [Header("Push Until Distance Parameters")]
    [Tooltip("Delay before the push is applied")]
    public float hitDelay = 0.1f;
    [Tooltip("Direction to push the enemy in, relative to the attacker's forward direction. If zero, push directly away from the attacker.")]
    public Vector3 hitDirection = Vector3.zero;
    [Tooltip("Distance to push the enemy")]
    public float pushDistance = 8f;
    [Tooltip("Time taken to complete the push")]
    public float pushTime = 0.3f;
    [Tooltip("If true, push away from the attacker. If false, pull towards the attacker.")]
    public bool pushAway = true;
    
    public override void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        base.Execute(onHitEvents, enemy, attack, attackerTransform);
        
        ohe.RunSegmentCoroutine(BeginPushUntilDistance(), ec.GetInstanceID().ToString()).
            OnDestroy(() => ec.PauseGravity(false));
    }
    
    IEnumerator<float> BeginPushUntilDistance()
    {
        yield return Timing.WaitForSeconds(hitDelay);
        
        ec.PauseGravity(true);
        
        Vector3 direction;

        if (hitDirection != Vector3.zero)
        {
            direction = hitDirection.GetRelativeVector3(t.forward).normalized;
        }
        else
        {
            direction = (ec.TargetedPosition() - t.position).normalized;
        }
        
        direction *= pushAway ? 1 : -1;
        
        float distance = pushAway ?
            pushDistance - (ec.TargetedPosition() - t.position).magnitude :
            (ec.TargetedPosition() - t.position).magnitude;
        
        ec.TraverseDistKnockback(direction.normalized, distance, pushTime);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false);
    }
}
