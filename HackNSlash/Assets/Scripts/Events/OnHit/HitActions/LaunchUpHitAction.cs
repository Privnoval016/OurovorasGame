using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class LaunchUpHitAction : IHitAction
{
    [Header("Launch Up Parameters")]
    [Tooltip("Delay before the launch is applied")]
    public float hitDelay = 0f;
    [Tooltip("Height to launch the enemy upwards")]
    public float launchUpHeight = 15;
    [Tooltip("Time taken to reach the launch up height")]
    public float launchUpTime = 0.3f;
    
    public override void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        base.Execute(onHitEvents, enemy, attack, attackerTransform);
        
        ohe.RunSegmentCoroutine(BeginLaunchUp(), ec.GetInstanceID().ToString());
    }
    
    
    IEnumerator<float> BeginLaunchUp()
    {
        yield return Timing.WaitForSeconds(hitDelay);
        
        Vector3 direction = Vector3.up;
        
        ec.TraverseDistKnockback(direction, launchUpHeight, launchUpTime);

        yield return Timing.WaitForOneFrame;
    }
}
