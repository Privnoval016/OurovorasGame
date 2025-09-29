using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class VerticalKnockbackHitAction : IHitAction
{
    [Header("Vertical Knockback Parameters")]
    [Tooltip("Force applied to the enemy when hit")]
    public float hitForce = 20f;
    
    public override void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        base.Execute(onHitEvents, enemy, attack, attackerTransform);
        
        ohe.RunSegmentCoroutine(BeginVerticalKnockback(), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginVerticalKnockback()
    {
        ec.PauseGravity(true);

        Vector3 direction = Vector3.up;
        
        ec.ForceKnockback(direction * hitForce);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false, 0);
        
        yield return Timing.WaitForOneFrame;
    }
}
