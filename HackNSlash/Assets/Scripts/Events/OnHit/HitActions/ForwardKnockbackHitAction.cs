using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[Serializable]
public class ForwardKnockbackHitAction : IHitAction
{
    [Header("Forward Knockback Parameters")]
    [Tooltip("Delay before applying the hit force")]
    public float hitDelay = 0f;
    [Tooltip("Force applied to the enemy when knocked back")]
    public float hitForce = 10f;
    [Tooltip("Direction of the hit force. If zero, uses the attacker's forward direction")]
    public Vector3 hitDirection = Vector3.zero;
    
    public override void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        base.Execute(onHitEvents, enemy, attack, attackerTransform);
        
        ohe.RunSegmentCoroutine(BeginForwardKnockback(), ec.GetInstanceID().ToString());
    }
    
    
    IEnumerator<float> BeginForwardKnockback()
    {
        yield return Timing.WaitForSeconds(hitDelay);
        
        Transform targetTransform = t == null ? pc.transform : t;
        
        ec.PauseGravity(true);

        Vector3 direction;
        
        if (hitDirection != Vector3.zero)
        {
            direction = hitDirection.GetRelativeVector3(targetTransform.forward).normalized;
        }
        else
        {
            direction = (ec.TargetedPosition() - targetTransform.position).normalized;
        }
        
        ec.ForceKnockback(direction * hitForce);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false, 0);
        
        yield return Timing.WaitForOneFrame;
    }
}
