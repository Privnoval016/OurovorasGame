using System;
using System.Collections.Generic;
using Extensions.Utils;
using UnityEngine;

[Serializable]
public class PullThenFollowHitAction : IHitAction
{
    [Header("Pull Then Follow Parameters")]
    [Tooltip("Force applied to the enemy when hit")]
    public float hitForce = 20f;
    
    public override void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        base.Execute(onHitEvents, enemy, attack, attackerTransform);
        
        ohe.RunSegmentCoroutine(BeginPullThenFollow(), ec.GetInstanceID().ToString()).
            OnDestroy(() => ec.PauseGravity(false, 0));
    }
    
    
    IEnumerator<float> BeginPullThenFollow()
    {
        if (!t.TryGetComponent(out KinematicBehaviour kb))
        {
            yield break;
        }

        if (kb.discreteVelocity.magnitude > 1000)
        {
            yield break;
        }
        
        ec.PauseGravity(true);

        float magnitude = kb.discreteVelocity.magnitude > 0.1f ? hitForce : hitForce * 0.5f;
        Vector3 directionToAttack = -(ec.TargetedPosition() - t.position).normalized;
        directionToAttack = (directionToAttack + kb.discreteVelocity.normalized).normalized;
        ec.SetVelocityKnockback(directionToAttack * magnitude);

        
        ec.PauseGravity(false, 0);
        
    }
}
