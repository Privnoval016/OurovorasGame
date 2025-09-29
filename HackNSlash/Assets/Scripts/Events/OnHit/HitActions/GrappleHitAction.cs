using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class GrappleHitAction : IHitAction
{
    [Header("Grapple Parameters")]
    [Tooltip("Speed at which the enemy is pulled towards the player")]
    public float grappleSpeed = 150;
    
    public override void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        base.Execute(onHitEvents, enemy, attack, attackerTransform);
        
        ohe.RunSegmentCoroutine(BeginGrapple(), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginGrapple()
    {
        ec.PauseGravity(true);

        Vector3 playerPos = pc.psm.TruePlayerForward.FindRadialVector3(pc.psm.playerData.smallRadius, 0) +
                            pc.transform.position;

        Vector3 direction = playerPos - ec.TargetedPosition();
        
        float grappleTime = direction.magnitude / grappleSpeed;

        ec.TweenKnockback(direction.normalized, direction.magnitude, grappleTime);

        yield return Timing.WaitForSeconds(grappleTime);

        ec.PauseGravity(false);
    }
}
