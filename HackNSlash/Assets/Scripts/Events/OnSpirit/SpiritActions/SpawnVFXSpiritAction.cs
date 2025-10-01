using System.Collections.Generic;
using Extensions.Utils;
using UnityEngine;

[System.Serializable]
public class SpawnVFXSpiritAction : ISpiritAction
{
    [Header("Spawn VFX Parameters")]
    [Tooltip("Indices of VFX to spawn from the SpiritAttack's VFX list.")]
    public int[] vfxInstantSpawns;
    
    public override void Execute(OnSpiritEvents onSpiritEvents, SpiritAttack attack)
    {
        base.Execute(onSpiritEvents, attack);
        
        ose.RunSegmentCoroutine(BeginSpawnVFX());
    }
    
    private IEnumerator<float> BeginSpawnVFX()
    {
        foreach (int i in vfxInstantSpawns)
        {
            CreateVFX(i);
        }

        ose.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown));

        yield break;
    }
}
