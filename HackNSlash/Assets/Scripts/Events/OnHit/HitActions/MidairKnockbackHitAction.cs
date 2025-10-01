using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class MidairKnockbackHitAction : IHitAction
{
    [Header("Midair Knockback Parameters")]
    [Tooltip("Distance to move the enemy forward in midair, as a multiple of the player's medium radius")]
    public float forwardDistance = 0.5f;
    [Tooltip("Time taken to complete the midair knockback movement")]
    public float midairKnockbackTime = 0.5f;
    
    public override void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        base.Execute(onHitEvents, enemy, attack, attackerTransform);
        
        ohe.RunSegmentCoroutine(BeginMidairKnockback(), ec.GetInstanceID().ToString()).
            OnDestroy(() => ec.PauseGravity(false, 0));
    }
    
    IEnumerator<float> BeginMidairKnockback()
    {
        Transform targetTransform = t == null ? pc.transform : t;

        ec.PauseGravity(true);
        
        Vector3 pos = ec.TargetedPosition().WithY(targetTransform.position.y) + Vector3.up * forwardDistance * pc.psm.playerData.mediumRadius;
        Vector3 movement = pos - ec.TargetedPosition();
        ec.TraverseDistKnockback(movement.normalized, movement.magnitude, midairKnockbackTime);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false, 0);
        
        yield return Timing.WaitForOneFrame;
    }
}
