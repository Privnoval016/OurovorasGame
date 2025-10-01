using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class LockPhysicsHitAction : IHitAction
{
    [Header("Lock Physics Parameters")]
    [Tooltip("Delay before the physics lock is applied")]
    public float hitDelay = 0.1f;
    [Tooltip("Force to apply when locking the physics")]
    public float hitForce = 10f;
    
    public override void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        base.Execute(onHitEvents, enemy, attack, attackerTransform);
        
        ohe.RunSegmentCoroutine(BeginLockPhysics(), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginLockPhysics()
    {
        yield return Timing.WaitForSeconds(hitDelay);
        
        ec.LockPhysics(hitForce);
        
        yield return Timing.WaitForSeconds(a.hitInfo.attackCoolDown);

    }
}
