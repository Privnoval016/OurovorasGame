using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using UnityEngine;
using MEC;
using PrimeTween;

public enum OnHitActions
{
    BasicKnockBack,
    LaunchUp,
    FollowPlayerVelocity,
    LaunchDown,
    MidairKnockback,
    CrossSlash,
    Grapple,
    LockPhysics,
    PushUntilDistance,
    ForwardKnockback,
    VerticalKnockback,
    Marked,
    PullThenFollow
}

public class OnHitEvents : Singleton<OnHitEvents>
{
    public static Dictionary<OnHitActions, Action<PlayerController, PhysicsEnemy, Attack, Transform>> OnHitActionMap;
    
    public OnHitParameters parameters;

    protected override void Awake()
    {
        base.Awake();
        AddOnHitMethods();
    }
    
    private void AddOnHitMethods()
    {
        if (OnHitActionMap != null) return;
        
        OnHitActionMap = new();
        
        OnHitActionMap.Add(OnHitActions.BasicKnockBack, BasicKnockBack);
        OnHitActionMap.Add(OnHitActions.LaunchUp, LaunchUp);
        OnHitActionMap.Add(OnHitActions.FollowPlayerVelocity, FollowPlayerVelocity);
        OnHitActionMap.Add(OnHitActions.LaunchDown, LaunchDown);
        OnHitActionMap.Add(OnHitActions.MidairKnockback, MidairKnockback);
        OnHitActionMap.Add(OnHitActions.CrossSlash, CrossSlash);
        OnHitActionMap.Add(OnHitActions.Grapple, Grapple);
        OnHitActionMap.Add(OnHitActions.LockPhysics, LockPhysics);
        OnHitActionMap.Add(OnHitActions.PushUntilDistance, PushUntilDistance);
        OnHitActionMap.Add(OnHitActions.ForwardKnockback, ForwardKnockback);
        OnHitActionMap.Add(OnHitActions.VerticalKnockback, VerticalKnockback);
        OnHitActionMap.Add(OnHitActions.Marked, Marked);
        OnHitActionMap.Add(OnHitActions.PullThenFollow, PullThenFollow);
    }

    #region Basic Knockback
    
    private void BasicKnockBack(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginBasicKnockBack(pc, ec, a, t), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginBasicKnockBack(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);
        
        Transform targetTransform = t == null ? pc.transform : t;
        
        ec.PauseGravity(true);

        Vector3 direction;
        
        if (a.hitInfo.hitDirection != Vector3.zero)
        {
            direction = a.hitInfo.hitDirection.GetRelativeVector3(targetTransform.forward).normalized;
        }
        else if (!ec.IsGrounded || pc.transform == targetTransform && pc.psm.IsMidair)
        {
            direction = Vector3.up;
        }
        else
        {
            direction = (ec.TargetedPosition() - targetTransform.position).ZeroVector3Axis().normalized;
        }
        
        ec.ForceKnockback(direction * a.hitInfo.hitForce);
        
        //CombatManager.Instance.HitStop(a, false, 0);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false, 0);
        
    }
    
    #endregion
    
    #region Midair Knockback
    
    private void MidairKnockback(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginMidairKnockback(pc, ec, a, t), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginMidairKnockback(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);
        
        Transform targetTransform = t == null ? pc.transform : t;

        ec.PauseGravity(true);
        
        Vector3 pos = ec.TargetedPosition().WithY(targetTransform.position.y) + Vector3.up * 0.5f * pc.psm.playerData.mediumRadius;
        Vector3 movement = pos - ec.TargetedPosition();
        ec.TraverseDistKnockback(movement.normalized, movement.magnitude, parameters.midairKnockbackTime);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false, 0);

    }

    #endregion
    
    #region Launch Up
    
    private void LaunchUp(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginLaunchUp(pc, ec, a, t), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginLaunchUp(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);

        Vector3 direction = Vector3.up;
        
        ec.TraverseDistKnockback(direction, parameters.launchUpHeight, parameters.launchUpTime);
    }
    
    #endregion
    
    #region Follow Player Velocity
    
    private void FollowPlayerVelocity(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginFollowPlayerVelocity(pc, ec, a, t), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginFollowPlayerVelocity(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        ec.PauseGravity(true);
        pc.IgnoreAllCollisionsWithLayer(ec.gameObject.layer, true);

        
        Vector3 lastNonZeroVelocity = Vector3.zero;
        
        while (!pc.psm.canAttack)
        {
            ec.SetVelocityKnockback(pc.discreteVelocity * parameters.followVelocityMult);
            
            lastNonZeroVelocity = ec.rb.linearVelocity.magnitude > 0.1f ? ec.rb.linearVelocity : lastNonZeroVelocity;
            
            yield return Timing.WaitForOneFrame;
        }
        
        if (a.hitInfo.hitDirection != Vector3.zero)
        {
            lastNonZeroVelocity = a.hitInfo.hitDirection.GetRelativeVector3(t.forward);
        }
        
        ec.ForceKnockback(lastNonZeroVelocity.normalized * a.hitInfo.hitForce);
        
        float startTime = Time.time;

        bool forceApplied = false;
        while (Time.time - startTime < a.hitInfo.hitDelay)
        {
         if (ec.IsGrounded && a.hitInfo.elasticCollision)
         {
             forceApplied = true;
             break;
         }
         yield return Timing.WaitForOneFrame;
        }

        if (forceApplied && a.hitInfo.elasticCollision)
        {
         Vector3 direction = Vector3.up;
         
         ec.TweenKnockback(direction, parameters.bounceHeight, parameters.bounceTime, Ease.OutQuad);
        }

        ec.PauseGravity(false, 0);
        pc.IgnoreAllCollisionsWithLayer(ec.gameObject.layer, false);

        
    }

    #endregion
    
    #region Launch Down
    
    private void LaunchDown(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginLaunchDown(pc, ec, a, t), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginLaunchDown(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        ec.PauseGravity(true);
        pc.IgnoreAllCollisionsWithLayer(ec.gameObject.layer, true);

        
        bool forceApplied = false;

        Vector3 vel = Vector3.zero;
        
        while (!pc.psm.canAttack)
        {
            vel = pc.rb.linearVelocity.magnitude > 0f ? pc.rb.linearVelocity * parameters.launchDownVelocityMult : ec.rb.linearVelocity; 
            
            if (ec.IsGrounded && a.hitInfo.elasticCollision)
            {
                forceApplied = true;
                break;
            }
            else
            {
                ec.SetVelocityKnockback(vel);
            }
            
            yield return Timing.WaitForOneFrame;
        }

        ec.SetVelocityKnockback(vel);

        if (forceApplied && a.hitInfo.elasticCollision)
        {
            Vector3 direction = Vector3.up;
            
            ec.TweenKnockback(direction, parameters.bounceHeight, parameters.bounceTime, Ease.OutQuad);
        }

        ec.PauseGravity(false);
        pc.IgnoreAllCollisionsWithLayer(ec.gameObject.layer, true);

        
        
        
    }

    #endregion
    
    #region Cross Slash


    
    private void CrossSlash(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginCrossSlash(pc, ec, a, t), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginCrossSlash(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    { 
        
        ec.PauseGravity(true);
        ec.rb.linearVelocity = Vector3.zero;
        
        yield return Timing.WaitForSeconds(parameters.crossSlashHitboxDelay);
        
        Vector3 direction = (ec.TargetedPosition() - t.position).normalized;
        if (!ec.IsGrounded) direction = Vector3.up;
        
        ec.ForceKnockback(direction * parameters.crossSlashHitStrength);
        
        ec.PauseGravity(false);
    }
    
    #endregion
    
    #region Grapple
    
    private void Grapple(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginGrapple(pc, ec, a, t), ec.GetInstanceID().ToString());
    }

    IEnumerator<float> BeginGrapple(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        ec.PauseGravity(true);

        Vector3 playerPos = pc.psm.TruePlayerForward.FindRadialVector3(pc.psm.playerData.smallRadius, 0) +
                            pc.transform.position;

        Vector3 direction = playerPos - ec.TargetedPosition();
        
        float grappleTime = direction.magnitude / parameters.grappleSpeed;

        ec.TweenKnockback(direction.normalized, direction.magnitude, grappleTime);

        yield return Timing.WaitForSeconds(grappleTime);

        ec.PauseGravity(false);
    }

    #endregion
    
    #region Lock Knockback

    private void LockPhysics(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginLockPhysics(pc, ec, a, t), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginLockPhysics(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);
        
        ec.LockPhysics(a.hitInfo.hitForce);
        
        yield return Timing.WaitForSeconds(a.hitInfo.attackCoolDown);

    }

    #endregion
    
    #region Push Until Distance
    
    private void PushUntilDistance(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginPushUntilDistance(pc, ec, a, t), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginPushUntilDistance(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);
        
        ec.PauseGravity(true);
        
        Vector3 direction;

        if (a.hitInfo.hitDirection != Vector3.zero)
        {
            direction = a.hitInfo.hitDirection.GetRelativeVector3(t.forward).normalized;
        }
        else
        {
            direction = (ec.TargetedPosition() - t.position).normalized;
        }
        
        direction *= Mathf.Sign(a.hitInfo.hitForce);
        
        float distance = Mathf.Sign(a.hitInfo.hitForce) >= 0 ?
            a.hitInfo.lateralRadius - (ec.TargetedPosition() - t.position).magnitude :
            (ec.TargetedPosition() - t.position).magnitude;
        
        ec.TraverseDistKnockback(direction.normalized, distance, a.hitInfo.hitDelay);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false);
    }
    
    #endregion
    
    #region Forward Knockback
    
    private void ForwardKnockback(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginForwardKnockback(pc, ec, a, t), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginForwardKnockback(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);
        
        Transform targetTransform = t == null ? pc.transform : t;
        
        ec.PauseGravity(true);

        Vector3 direction;
        
        if (a.hitInfo.hitDirection != Vector3.zero)
        {
            direction = a.hitInfo.hitDirection.GetRelativeVector3(targetTransform.forward).normalized;
        }
        else
        {
            direction = (ec.TargetedPosition() - targetTransform.position).normalized;
        }
        
        ec.ForceKnockback(direction * a.hitInfo.hitForce);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false, 0);
        
    }
    
    #endregion
    
    
    #region Vertical Knockback
    
    private void VerticalKnockback(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginVerticalKnockback(pc, ec, a, t), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginVerticalKnockback(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);
        
        ec.PauseGravity(true);

        Vector3 direction = Vector3.up;
        
        ec.ForceKnockback(direction * a.hitInfo.hitForce);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false, 0);
        
    }
    
    #endregion
    
    #region Marked
    
    private void Marked(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        var vfx = OnVFXEvents.Instance.SpawnPlayerVFX(pc, a, 1, 
            new TransformInfo(ec.TargetedPosition(), Quaternion.identity, Vector3.one));
        
        
        vfx.transform.SetParent(ec.transform);

        vfx.activeHitbox = false;

        vfx.RunSegmentCoroutine(ActivateDelayedHit(vfx, a).CancelWith(vfx), vfx.GetInstanceID().ToString());
    }
    
    IEnumerator<float> ActivateDelayedHit(VFXController vfx, Attack a)
    {
        yield return Timing.WaitForSeconds(vfx.timeActive);
        
        vfx.activeHitbox = true;
        
        yield return Timing.WaitForSeconds(0.3f);
        
        if (vfx != null)
            vfx.activeHitbox = false;
    }
    
    #endregion
    
    #region PullThenFollow
    
    private void PullThenFollow(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginPullThenFollow(pc, ec, a, t), ec.GetInstanceID().ToString());
    }
    
    IEnumerator<float> BeginPullThenFollow(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        if (!t.TryGetComponent(out KinematicBehaviour kb))
        {
            yield break;
        }

        if (kb.discreteVelocity.magnitude > 1000)
        {
            yield break;
        }
        
        ec.PauseGravity(true);

        float magnitude = kb.discreteVelocity.magnitude > 0.1f ? a.hitInfo.hitForce : a.hitInfo.hitForce * 0.5f;
        Vector3 directionToAttack = -(ec.TargetedPosition() - t.position).normalized;
        directionToAttack = (directionToAttack + kb.discreteVelocity.normalized).normalized;
        ec.SetVelocityKnockback(directionToAttack * magnitude);

        
        ec.PauseGravity(false, 0);
        
    }
    
    #endregion
}
