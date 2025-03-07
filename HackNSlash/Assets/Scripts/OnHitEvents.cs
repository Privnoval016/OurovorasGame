using System;
using System.Collections.Generic;
using ExtensionUtils;
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
    LockPhysics
}

public class OnHitEvents : MonoBehaviour
{
    public static OnHitEvents Instance { get; private set; }
    
    public static Dictionary<OnHitActions, Action<PlayerController, PhysicsEnemy, Attack>> OnHitActionMap;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
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
    }

    #region Basic Knockback
    
    private void BasicKnockBack(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        this.RunSegmentCoroutine(BeginBasicKnockBack(pc, ec, a));
    }
    
    IEnumerator<float> BeginBasicKnockBack(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);
        
        
        ec.pauseGravity = true;

        Vector3 direction = Vector3.zero;
        
        if (a.hitInfo.hitDirection != Vector3.zero)
        {
            direction = a.hitInfo.hitDirection.GetRelativeVector3(pc.transform.forward).normalized;
        }
        else
        {
            direction = (ec.TargetedPosition() - pc.transform.position).normalized;
        }
        
        ec.ForceKnockback(direction * a.hitInfo.hitForce);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.pauseGravity = false;
        
    }
    
    #endregion
    
    #region Midair Knockback
    
    [Header("Midair Knockback Attack")]
    [SerializeField] private float midairKnockbackTime = 0.1f;
    
    private void MidairKnockback(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        this.RunSegmentCoroutine(BeginMidairKnockback(pc, ec, a));
    }
    
    IEnumerator<float> BeginMidairKnockback(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);

        ec.pauseGravity = true;
        
        Vector3 pos = ec.TargetedPosition().WithY(pc.transform.position.y) + Vector3.up * 0.5f * pc.psm.playerData.mediumRadius;
        Vector3 movement = pos - ec.TargetedPosition();
        ec.TraverseDistKnockback(movement.normalized, movement.magnitude, midairKnockbackTime);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.pauseGravity = false;
        
    }

    #endregion
    
    #region Launch Up

    [Header("Launch Up Attack")] 
    [SerializeField] private float launchUpTime = 0.5f;
    [SerializeField] private float launchUpHeight = 10f;
    
    private void LaunchUp(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        this.RunSegmentCoroutine(BeginLaunchUp(pc, ec, a));
    }
    
    IEnumerator<float> BeginLaunchUp(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        yield return Timing.WaitUntilTrue(() => pc.psm.canAttack);

        Vector3 direction = Vector3.up;
        
        ec.TraverseDistKnockback(direction, launchUpHeight, launchUpTime);
    }
    
    #endregion
    
    #region Follow Player Velocity
    
    [Header("Follow Player Velocity Attack")]
    [SerializeField] private float followVelocityMult = 1.2f;
    
    private void FollowPlayerVelocity(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        this.RunSegmentCoroutine(BeginFollowPlayerVelocity(pc, ec, a));
    }
    
    IEnumerator<float> BeginFollowPlayerVelocity(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        ec.pauseGravity = true;
        pc.IgnoreCollision(ec.col, true);
        
        Vector3 lastNonZeroVelocity = Vector3.zero;
        
        while (!pc.psm.canAttack)
        {
            ec.SetVelocityKnockback(pc.psm.discreteVelocity * followVelocityMult);
            
            lastNonZeroVelocity = ec.rb.linearVelocity.magnitude > 0.1f ? ec.rb.linearVelocity : lastNonZeroVelocity;
            
            yield return Timing.WaitForOneFrame;
        }
        
        if (a.hitInfo.hitDirection != Vector3.zero)
        {
            lastNonZeroVelocity = a.hitInfo.hitDirection.GetRelativeVector3(pc.transform.forward);
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
             
             ec.TweenKnockback(direction, bounceHeight, bounceTime, Ease.OutQuad);
         }
        
        ec.pauseGravity = false;
        pc.IgnoreCollision(ec.col, false);
        
    }

    #endregion
    
    #region Launch Down
    
    [Header("Launch Down Attack")]
    [SerializeField] private float launchDownVelocityMult = 1.2f;
    [SerializeField] private float bounceCheckTime = 0.5f;
    [SerializeField] private float bounceTime = 0.3f;
    [SerializeField] private float bounceHeight = 15f;
    
    private void LaunchDown(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        this.RunSegmentCoroutine(BeginLaunchDown(pc, ec, a));
    }
    
    IEnumerator<float> BeginLaunchDown(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        ec.pauseGravity = true;
        pc.IgnoreCollision(ec.col, true);
        
        bool forceApplied = false;
        
        while (!pc.psm.canAttack)
        {
            Vector3 vel = pc.rb.linearVelocity.magnitude > 0f ? pc.rb.linearVelocity * launchDownVelocityMult : ec.rb.linearVelocity; 
            
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

        if (forceApplied && a.hitInfo.elasticCollision)
        {
            Vector3 direction = Vector3.up;
            
            ec.TweenKnockback(direction, bounceHeight, bounceTime, Ease.OutQuad);
        }

        ec.pauseGravity = false;
        pc.IgnoreCollision(ec.col, false);
        
        
        
    }

    #endregion
    
    #region Cross Slash

    [Header("Cross Slash")] [SerializeField]
    private float crossSlashHitboxDelay = 0.4f;

    private float crossSlashHitStrength = 10f;
    
    private void CrossSlash(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        this.RunSegmentCoroutine(BeginCrossSlash(pc, ec, a));
    }
    
    IEnumerator<float> BeginCrossSlash(PlayerController pc, PhysicsEnemy ec, Attack a)
    { 
        
        
        ec.pauseGravity = true;
        ec.rb.linearVelocity = Vector3.zero;
        
        yield return Timing.WaitForSeconds(crossSlashHitboxDelay);
        
        Vector3 direction = (ec.TargetedPosition() - pc.transform.position).normalized;
        if (!ec.IsGrounded) direction = Vector3.up;
        
        ec.ForceKnockback(direction * crossSlashHitStrength);
        
        ec.pauseGravity = false;
    }
    
    #endregion
    
    #region Grapple

    [Header("Grapple Attack")] 
    [SerializeField] private float grappleTime = 0.1f;
    
    private void Grapple(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        this.RunSegmentCoroutine(BeginGrapple(pc, ec, a));
    }

    IEnumerator<float> BeginGrapple(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        ec.pauseGravity = true;

        Vector3 playerPos = pc.transform.forward.FindRadialVector3(pc.psm.playerData.smallRadius, 0) +
                            pc.transform.position;

        Vector3 direction = playerPos - ec.TargetedPosition();

        ec.TweenKnockback(direction.normalized, direction.magnitude, grappleTime);

        yield return Timing.WaitForSeconds(grappleTime);

        ec.pauseGravity = false;
    }

    #endregion
    
    [Header("Lock Knockback")]
    [SerializeField] private float releaseKnockbackTime = 2f;


    #region Lock Knockback

    private void LockPhysics(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        this.RunSegmentCoroutine(BeginLockPhysics(pc, ec, a));
    }
    
    IEnumerator<float> BeginLockPhysics(PlayerController pc, PhysicsEnemy ec, Attack a)
    {
        ec.physicsInteract = false;
        ec.rb.linearVelocity = Vector3.zero;

        ec.KillObjectCoroutines(nameof(ReleasePhysicsLock));
        ec.RunSegmentCoroutine(ReleasePhysicsLock(ec), nameof(ReleasePhysicsLock));
        
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);

    }

    IEnumerator<float> ReleasePhysicsLock(PhysicsEnemy ec)
    {
        yield return Timing.WaitForSeconds(releaseKnockbackTime);
        
        ec.physicsInteract = true;
    }

    #endregion
}
