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
    VerticalKnockback
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
    }

    #region Basic Knockback
    
    private void BasicKnockBack(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginBasicKnockBack(pc, ec, a, t));
    }
    
    IEnumerator<float> BeginBasicKnockBack(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);
        
        
        ec.PauseGravity(true);

        Vector3 direction;
        
        if (a.hitInfo.hitDirection != Vector3.zero)
        {
            direction = a.hitInfo.hitDirection.GetRelativeVector3(t.forward).normalized;
        }
        else if (!ec.IsGrounded)
        {
            direction = Vector3.up;
        }
        else
        {
            direction = (ec.TargetedPosition() - t.position).normalized;
        }
        
        ec.ForceKnockback(direction * a.hitInfo.hitForce);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false, 0);
        
    }
    
    #endregion
    
    #region Midair Knockback
    
    private void MidairKnockback(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginMidairKnockback(pc, ec, a, t));
    }
    
    IEnumerator<float> BeginMidairKnockback(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);

        ec.PauseGravity(true);
        
        Vector3 pos = ec.TargetedPosition().WithY(t.position.y) + Vector3.up * 0.5f * pc.psm.playerData.mediumRadius;
        Vector3 movement = pos - ec.TargetedPosition();
        ec.TraverseDistKnockback(movement.normalized, movement.magnitude, parameters.midairKnockbackTime);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false, 0);

    }

    #endregion
    
    #region Launch Up
    
    private void LaunchUp(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginLaunchUp(pc, ec, a, t));
    }
    
    IEnumerator<float> BeginLaunchUp(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        yield return Timing.WaitUntilTrue(() => pc.psm.canAttack);

        Vector3 direction = Vector3.up;
        
        ec.TraverseDistKnockback(direction, parameters.launchUpHeight, parameters.launchUpTime);
    }
    
    #endregion
    
    #region Follow Player Velocity
    
    private void FollowPlayerVelocity(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginFollowPlayerVelocity(pc, ec, a, t));
    }
    
    IEnumerator<float> BeginFollowPlayerVelocity(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        ec.PauseGravity(true);
        pc.IgnoreCollision(ec.col, true);
        
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
        pc.IgnoreCollision(ec.col, false);
        
    }

    #endregion
    
    #region Launch Down
    
    private void LaunchDown(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginLaunchDown(pc, ec, a, t));
    }
    
    IEnumerator<float> BeginLaunchDown(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        ec.PauseGravity(true);
        pc.IgnoreCollision(ec.col, true);
        
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
        pc.IgnoreCollision(ec.col, false);
        
        
        
    }

    #endregion
    
    #region Cross Slash


    
    private void CrossSlash(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginCrossSlash(pc, ec, a, t));
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
        this.RunSegmentCoroutine(BeginGrapple(pc, ec, a, t));
    }

    IEnumerator<float> BeginGrapple(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        ec.PauseGravity(true);

        Vector3 playerPos = pc.transform.forward.FindRadialVector3(pc.psm.playerData.smallRadius, 0) +
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
        this.RunSegmentCoroutine(BeginLockPhysics(pc, ec, a, t));
    }
    
    IEnumerator<float> BeginLockPhysics(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        ec.physicsInteract = false;
        ec.rb.linearVelocity = Vector3.zero;

        ec.KillObjectCoroutines(nameof(ReleasePhysicsLock));
        ec.RunSegmentCoroutine(ReleasePhysicsLock(ec), nameof(ReleasePhysicsLock));
        
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);

    }

    IEnumerator<float> ReleasePhysicsLock(PhysicsEnemy ec)
    {
        float startTime = Time.time;
        
        bool? damageTaken = null;

        while (Time.time - startTime < parameters.releaseKnockbackTime && damageTaken != true)
        {
            if (!ec.tookDamageThisAction) damageTaken = false;
            else if (damageTaken == false && ec.tookDamageThisAction) damageTaken = true;
            yield return Timing.WaitForOneFrame;
        }
        
        ec.physicsInteract = true;
    }

    #endregion
    
    #region Push Until Distance
    
    private void PushUntilDistance(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginPushUntilDistance(pc, ec, a, t));
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
        
        float distance = a.hitInfo.lateralRadius - (ec.TargetedPosition() - t.position).magnitude;
        
        ec.TraverseDistKnockback(direction.normalized, distance, a.hitInfo.hitDelay);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false);
    }
    
    #endregion
    
    #region Forward Knockback
    
    private void ForwardKnockback(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginForwardKnockback(pc, ec, a, t));
    }
    
    IEnumerator<float> BeginForwardKnockback(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
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
        
        ec.ForceKnockback(direction * a.hitInfo.hitForce);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.PauseGravity(false, 0);
        
    }
    
    #endregion
    
    
    #region Vertical Knockback
    
    private void VerticalKnockback(PlayerController pc, PhysicsEnemy ec, Attack a, Transform t)
    {
        this.RunSegmentCoroutine(BeginVerticalKnockback(pc, ec, a, t));
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
}
