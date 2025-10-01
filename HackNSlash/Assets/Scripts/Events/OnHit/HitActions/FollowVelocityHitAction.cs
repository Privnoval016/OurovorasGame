using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using PrimeTween;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class FollowVelocityHitAction : IHitAction
{
    [Header("Follow Velocity Parameters")]
    [Tooltip("Multiplier for the player's current velocity to apply to the enemy when hit.")]
    public float followVelocityMultiplier = 1.5f;
    [Tooltip("Force applied to the enemy after following the player's velocity.")]
    public float hitForce = 10f;
    [Tooltip("Direction of the hit force. If zero, uses the player's forward direction.")]
    public Vector3 hitDirection = Vector3.zero;
    
    [Header("Bounce Parameters")]
    
    [Tooltip("Whether the enemy should bounce upon landing.")]
    public bool elasticCollision = true;
    
    [Tooltip("Minimum time of movement before the enemy can bounce.")]
    public float minTimeToBounce = 0.2f;
    
    [Tooltip("Height of the bounce.")]
    public float bounceHeight = 5f;
    [Tooltip("Time taken to complete the bounce.")]
    public float bounceTime = 0.3f;

    public override void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        base.Execute(onHitEvents, enemy, attack, attackerTransform);
        
        ohe.RunSegmentCoroutine(BeginFollowPlayerVelocity(), ec.GetInstanceID().ToString()).
            OnDestroy(() => ec.PauseGravity(false, 0));
    }

    IEnumerator<float> BeginFollowPlayerVelocity()
    {
        ec.PauseGravity(true);
        pc.IgnoreAllCollisionsWithLayer(ec.gameObject.layer, true);

        
        Vector3 lastNonZeroVelocity = Vector3.zero;
        
        while (!pc.psm.canAttack)
        {
            ec.SetVelocityKnockback(pc.discreteVelocity * followVelocityMultiplier);
            
            lastNonZeroVelocity = ec.rb.linearVelocity.magnitude > 0.1f ? ec.rb.linearVelocity : lastNonZeroVelocity;
            
            yield return Timing.WaitForOneFrame;
        }
        
        if (hitDirection != Vector3.zero)
        {
            lastNonZeroVelocity = hitDirection.GetRelativeVector3(t.forward);
        }
        
        ec.ForceKnockback(lastNonZeroVelocity.normalized * hitForce);
        
        float startTime = Time.time;

        bool forceApplied = false;
        while (Time.time - startTime < minTimeToBounce)
        {
            if (ec.IsGrounded && elasticCollision)
            {
                forceApplied = true;
                break;
            }
            yield return Timing.WaitForOneFrame;
        }

        if (forceApplied && elasticCollision)
        {
            Vector3 direction = Vector3.up;
         
            ec.TweenKnockback(direction, bounceHeight, bounceTime, Ease.OutQuad);
        }

        ec.PauseGravity(false, 0);
        pc.IgnoreAllCollisionsWithLayer(ec.gameObject.layer, false);

        
    }
}
