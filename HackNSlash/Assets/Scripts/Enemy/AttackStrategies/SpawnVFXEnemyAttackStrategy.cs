using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[Serializable]
public class SpawnVFXEnemyAttackStrategy : IEnemyAttackStrategy
{
    public int VFXSpawnIndex;

    protected override void OnExecute()
    {
        if (pc == null) return;
        
        oee.RunSegmentCoroutine(BeginSpawnVFXAtHitbox());
    }

    private IEnumerator<float> BeginSpawnVFXAtHitbox()
    {
        VFXSpawnInfo vfxInfo = a.attack.vfxInfos[VFXSpawnIndex];
        yield return Timing.WaitUntilTrue(() => ts.attackHitboxes[vfxInfo.vfxEnemyActionIndex].activeHitbox);
    
        // calculating direction to shoot from hitbox to player, but keeping the horizontal direction of the enemy's forward vector
        
        Vector3 shootPosition = ts.attackHitboxes[vfxInfo.vfxEnemyActionIndex].transform.position;
        Vector3 toB = pc.transform.position - shootPosition;
        Vector3 toBHorizontal = toB.ZeroVector3Axis();
        float horizontalDist = toBHorizontal.magnitude;

        Vector3 forwardXZ = ts.transform.forward.ZeroVector3Axis().normalized;

        Vector3 shootDirection = forwardXZ.WithY(toB.y / horizontalDist);
        
        CreateVFX(VFXSpawnIndex,
            new TransformInfo(shootPosition, Quaternion.LookRotation(shootDirection), Vector3.one)
        );
    }
}
