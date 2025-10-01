using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using PrimeTween;
using UnityEngine;

[System.Serializable]
public class LaunchDownHitAction : IHitAction
{
    
    [Header("Launch Down Parameters")]
    [Tooltip("Multiplier for the downward launch velocity based on current velocity")]
    public float launchDownVelocityMultiplier = 1.5f;
    [Tooltip("Height of the bounce after landing")]
    public float bounceHeight = 5f;
    [Tooltip("Time taken to reach the peak of the bounce")]
    public float bounceTime = 0.3f;
    [Tooltip("Whether the enemy should bounce upon landing")]
    public bool elasticCollision = true;
    
    public override void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        base.Execute(onHitEvents, enemy, attack, attackerTransform);
        
        ohe.RunSegmentCoroutine(BeginLaunchDown(), ec.GetInstanceID().ToString()).
            OnDestroy(() =>
            {
                ec.PauseGravity(false, 0);
                pc.IgnoreAllCollisionsWithLayer(ec.gameObject.layer, false);
            });
    }
    
    IEnumerator<float> BeginLaunchDown()
    {
        ec.PauseGravity(true);
        pc.IgnoreAllCollisionsWithLayer(ec.gameObject.layer, true);

        
        bool forceApplied = false;

        Vector3 vel = Vector3.zero;
        
        while (!pc.psm.canAttack)
        {
            vel = pc.rb.linearVelocity.magnitude > 0f ? pc.rb.linearVelocity * launchDownVelocityMultiplier : ec.rb.linearVelocity; 
            
            if (ec.IsGrounded && elasticCollision)
            {
                forceApplied = true;
                break;
            }
            else
            {
                ec.SetVelocityKnockback(vel);
            }
            
            yield return Timing.WaitForOneFrame;
        }

        ec.SetVelocityKnockback(vel);

        if (forceApplied && elasticCollision)
        {
            Vector3 direction = Vector3.up;
            
            ec.TweenKnockback(direction, bounceHeight, bounceTime, Ease.OutQuad);
        }

        ec.PauseGravity(false);
        pc.IgnoreAllCollisionsWithLayer(ec.gameObject.layer, true);

        
        
        
    }
}
