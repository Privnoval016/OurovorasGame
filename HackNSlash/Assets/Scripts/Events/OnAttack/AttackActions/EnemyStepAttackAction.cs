using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[Serializable]
public class EnemyStepAttackAction : IAttackAction
{
    [Header("Enemy Step Settings")]
    [Tooltip("Height to step up")]
    public float enemyStepHeight = 4f;
    [Tooltip("Time taken to complete the step")]
    public float enemyStepTime = 0.3f;
    [Tooltip("Pushback away from the enemy")]
    public float enemyStepPushBack = 0f;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(BeginEnemyStep());
    }
    
    private IEnumerator<float> BeginEnemyStep()
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        Vector3 enemyPos = pc.psm.GetClosestEnemyInCapsule(pc.psm.playerData.mediumRadius, pc.psm.playerData.heightRadius) != null ? 
            pc.psm.GetClosestEnemyInCapsule(pc.psm.playerData.mediumRadius, pc.psm.playerData.heightRadius).TargetedPosition() : pc.transform.position;
        Vector3 direction = (pc.transform.position - enemyPos).ZeroVector3Axis().normalized;
        direction = (Vector3.up + direction * enemyStepPushBack).normalized;
        
        pc.rb.linearVelocity = pc.rb.linearVelocity.ZeroVector3Axis();
		
        oae.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(direction, enemyStepHeight, enemyStepTime));
    }
}
