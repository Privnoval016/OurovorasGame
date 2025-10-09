using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[Serializable]
public class DodgeAttackAction : IAttackAction
{
    public enum DodgeType
    {
        Regular,
        TargetDodge,
        DodgeDown,
        TeleportsBehindYou
    }
    
    [Header("Dodge Settings")]
    [Tooltip("Type of dodge to perform")]
    public DodgeType attackEventIndex = DodgeType.Regular;
    [Tooltip("Distance to dodge")]
    public float dodgeDistance = 8f;
    [Tooltip("Time taken to complete the dodge")]
    public float dodgeTime = 0.2f;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        Dodge();
    }
    
    private void Dodge()
    {
        
        IEnumerator<float> attack = BeginRegularDodge();
        
        pc.ps.isInvincible = true;
        
        switch (attackEventIndex)
        {
            case DodgeType.Regular:
                attack = BeginRegularDodge();
                break;
            case DodgeType.TargetDodge:
                attack = BeginTargetDodge();
                break;
            case DodgeType.DodgeDown:
                attack = BeginDodgeDown();
                break;
            case DodgeType.TeleportsBehindYou:
                attack = BeginTeleportsBehindYou();
                break;
        }
        
        oae.RunSegmentCoroutine(attack).
            OnDestroy(() => pc.IgnoreAllCollisionsWithLayer(GameManager.Instance.enemyLayer, false));

    }

    IEnumerator<float> BeginRegularDodge()
    { 
        yield return Timing.WaitForSeconds(a.animDelay);
        
        Vector3 dodgeDirection = pc.psm.moveDirection.ZeroVector3Axis().normalized;
        float distance = dodgeDistance;
        float moveTime = dodgeTime;
        
        if (pc.psm.StandardizedMoveDir.magnitude < 0.1f)
        {
            dodgeDirection = pc.psm.IsMidair ? Vector3.down : Vector3.up;
        }
        
        pc.IgnoreAllCollisionsWithLayer(pc.cam.TargetedEnemy?.gameObject.layer, true);
        
        oae.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        pc.IgnoreAllCollisionsWithLayer(pc.cam.TargetedEnemy?.gameObject.layer, false);

        yield return Timing.WaitForSeconds(moveTime);
        
        oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown));

    }
    
    IEnumerator<float> BeginTargetDodge()
    {
        
        yield return Timing.WaitForSeconds(a.animDelay);

        Vector3 teleportedPosition = pc.cam.TargetedEnemy.transform.position +
                                     (pc.transform.position - pc.cam.TargetedEnemy.transform.position)
                                     .ZeroVector3Axis().normalized * pc.psm.playerData.mediumRadius;

        Vector3 dodgeDirection = teleportedPosition - pc.transform.position;

        float distance = dodgeDirection.magnitude;
        dodgeDirection.Normalize();

        float moveTime = dodgeTime / 2;


        (pc.cam.TargetedEnemy as PhysicsEnemy)?.ResetMovement();
        
        pc.IgnoreAllCollisionsWithLayer(pc.cam.TargetedEnemy?.gameObject.layer, true);
        
        oae.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        yield return Timing.WaitForSeconds(moveTime);
        
        pc.IgnoreAllCollisionsWithLayer(pc.cam.TargetedEnemy?.gameObject.layer, false);
        
        oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown));
    }
    
    IEnumerator<float> BeginTeleportsBehindYou()
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        Vector3 targetPos = pc.cam.TargetPosition +
                            (pc.cam.TargetPosition - pc.transform.position).ZeroVector3Axis()
                            .normalized * pc.psm.playerData.mediumRadius * 0.5f;

        Vector3 dodgeDirection = targetPos - pc.transform.position;
        float distance = dodgeDirection.magnitude;
        dodgeDirection.Normalize();

        float moveTime = dodgeTime;
        
        (pc.cam.TargetedEnemy as PhysicsEnemy)?.ResetMovement();

        pc.IgnoreAllCollisionsWithLayer(pc.cam.TargetedEnemy?.gameObject.layer, true);
        
        oae.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        yield return Timing.WaitForSeconds(moveTime);
        
        pc.IgnoreAllCollisionsWithLayer(pc.cam.TargetedEnemy?.gameObject.layer, false);
        
        oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown));

    }
    
    IEnumerator<float> BeginDodgeDown()
    {
        yield return Timing.WaitForSeconds(a.animDelay);

        Vector3 dodgeDirection;
        float distance;
        float moveTime = dodgeTime;
        
        if (pc.psm.IsMidair)
        {
            Vector3 targetPos = -pc.transform.forward.normalized * dodgeDistance + pc.transform.position;
            bool isGround = Physics.Raycast(targetPos, Vector3.down,
                out RaycastHit hit, 100f, GameManager.Instance.groundLayer);
            Vector3 point = isGround ? hit.point : targetPos;

            dodgeDirection = point - pc.transform.position;
            distance = dodgeDirection.magnitude;
            dodgeDirection.Normalize();
        }
        else
        {
            dodgeDirection = -pc.transform.forward.normalized;
            distance = dodgeDistance;
        }

        pc.IgnoreAllCollisionsWithLayer(pc.cam.TargetedEnemy?.gameObject.layer, true);
        
        oae.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        yield return Timing.WaitForSeconds(moveTime);
        
        pc.IgnoreAllCollisionsWithLayer(pc.cam.TargetedEnemy?.gameObject.layer, false);

        
        oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown));

    }
}
