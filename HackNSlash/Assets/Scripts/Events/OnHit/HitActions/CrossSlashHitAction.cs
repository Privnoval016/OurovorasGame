using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[Serializable]
public class CrossSlashHitAction : IHitAction
{
    [Header("Cross Slash Parameters")]
    public float crossSlashHitboxDelay = 0.4f;
    public float crossSlashHitStrength = 10f;
    
    public override void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        base.Execute(onHitEvents, enemy, attack, attackerTransform);
        
        ohe.RunSegmentCoroutine(BeginCrossSlash(), ec.GetInstanceID().ToString());
    }
    
    
    IEnumerator<float> BeginCrossSlash()
    { 
        
        ec.PauseGravity(true);
        ec.rb.linearVelocity = Vector3.zero;
        
        yield return Timing.WaitForSeconds(crossSlashHitboxDelay);
        
        Vector3 direction = (ec.TargetedPosition() - t.position).normalized;
        if (!ec.IsGrounded) direction = Vector3.up;
        
        ec.ForceKnockback(direction * crossSlashHitStrength);
        
        ec.PauseGravity(false);
    }
}
