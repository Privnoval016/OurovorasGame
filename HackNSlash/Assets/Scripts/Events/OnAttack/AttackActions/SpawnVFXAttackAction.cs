using System.Collections.Generic;
using Extensions.Utils;
using UnityEngine;
using MEC;

[System.Serializable]
public class SpawnVFXAttackAction : IAttackAction
{
    [Header("Spawn VFX Settings")]
    
    [Tooltip("Indices of the VFX to spawn from the Player's VFX list")]
    public int[] vfxIndices;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(BeginSpawnVFX());
    }
    
    private IEnumerator<float> BeginSpawnVFX()
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        foreach (int index in vfxIndices)
        {
            CreateVFX(index);
        }
        
        oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown));
    }
}
