using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[Serializable]
public class RotateTowardsPlayerEnemyAttackStrategy : NoneEnemyAttackStrategy
{
    public float turnDuration = 0.1f;
    
    protected override void OnExecute()
    {
        if (pc == null) return;
        
        oee.RunSegmentCoroutine(RotateTowardsPlayer());
    }
    
    private IEnumerator<float> RotateTowardsPlayer()
    {
        float elapsed = 0f;
        while (elapsed < turnDuration)
        {
            Vector3 toPlayer = pc.transform.position - ts.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
                ts.transform.rotation = Quaternion.Slerp(ts.transform.rotation, targetRot, elapsed / turnDuration);
            }
            
            elapsed += Timing.DeltaTime;
            yield return Timing.WaitForOneFrame;
        }
    }
    
    
}