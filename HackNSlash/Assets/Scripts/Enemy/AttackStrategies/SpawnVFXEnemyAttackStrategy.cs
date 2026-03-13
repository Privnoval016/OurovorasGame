using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[Serializable]
public class SpawnVFXEnemyAttackStrategy : IEnemyAttackStrategy
{
    public int VFXSpawnIndex;
    
    protected override bool AutoPlayAttackClips => true;
    public float rotationTime = 0.1f;

    protected override void OnExecute()
    {
        if (pc == null) return;
        
        oee.RunSegmentCoroutine(BeginSpawnVFXAtHitbox());
    }

    private IEnumerator<float> BeginSpawnVFXAtHitbox()
    {
        VFXSpawnInfo vfxInfo = a.attack.vfxInfos[VFXSpawnIndex];
        
        // rotate to face the player such that the hitbox forward direction is facing the player, but only rotate on the horizontal plane
        
        float elapsed = 0f;
        Transform hitboxTransform = ts.attackHitboxes[vfxInfo.vfxEnemyActionIndex].transform;
        Vector3 hitboxOffsetToEnemy = hitboxTransform.position - ts.transform.position;
        while (elapsed < rotationTime && !ts.attackHitboxes[vfxInfo.vfxEnemyActionIndex].activeHitbox)
        {
            // calculate the direction the enemy should face such that the hitbox position is facing the player

            Vector3 toPlayer = pc.transform.position - ts.transform.position;
            toPlayer.y = 0f;
            Vector3 hitboxOffsetToEnemyHorizontal = hitboxOffsetToEnemy.ZeroVector3Axis();
            Vector3 desiredFacingDirection = (toPlayer - hitboxOffsetToEnemyHorizontal).normalized;
            if (desiredFacingDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desiredFacingDirection);
                ts.transform.rotation = Quaternion.Slerp(ts.transform.rotation, targetRotation, elapsed / rotationTime);

            }

            elapsed += Timing.DeltaTime;
            yield return Timing.WaitForOneFrame;
        }

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
