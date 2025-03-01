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
    MidairKnockback
}

public class OnHitEvents : MonoBehaviour
{
    public static OnHitEvents Instance { get; private set; }
    
    public static Dictionary<OnHitActions, Action<PlayerController, IDamageable, Attack>> OnHitActionMap;

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
    }
    
    public void EndObjectCoroutines()
    {
        Timing.KillCoroutines(gameObject);
    }

    #region Basic Knockback
    
    private void BasicKnockBack(PlayerController pc, IDamageable enemy, Attack a)
    {
        Timing.RunCoroutine(BeginBasicKnockBack(pc, enemy, a));
    }
    
    IEnumerator<float> BeginBasicKnockBack(PlayerController pc, IDamageable enemy, Attack a)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);
        
        if (!enemy.TryGetComponent(out EnemyController ec)) yield break;
        
        ec.pauseGravity = true;

        Vector3 direction = Vector3.zero;
        
        if (a.hitInfo.hitDirection != Vector3.zero)
        {
            direction = a.hitInfo.hitDirection.GetRelativeVector3(pc.transform.forward).normalized;
        }
        else
        {
            direction = (enemy.transform.position - pc.transform.position).normalized;
        }
        
        ec.ForceKnockback(direction * a.hitInfo.hitForce);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.pauseGravity = false;
        
    }
    
    #endregion
    
    #region Midair Knockback
    
    [Header("Midair Knockback Attack")]
    [SerializeField] private float midairKnockbackTime = 0.1f;
    
    private void MidairKnockback(PlayerController pc, IDamageable enemy, Attack a)
    {
        Timing.RunCoroutine(BeginMidairKnockback(pc, enemy, a));
    }
    
    IEnumerator<float> BeginMidairKnockback(PlayerController pc, IDamageable enemy, Attack a)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitDelay);
        
        if (!enemy.TryGetComponent(out EnemyController ec)) yield break;
        
        ec.pauseGravity = true;

        Vector3 pos = ec.transform.position.WithY(pc.transform.position.y) + Vector3.up * 0.5f * pc.playerRadius;
        Vector3 movement = pos - ec.transform.position;
        ec.TraverseDistKnockback(movement.normalized, movement.magnitude, midairKnockbackTime);
        
        Timing.WaitUntilTrue(() => pc.psm.canAttack);
        ec.pauseGravity = false;
        
    }

    #endregion
    
    #region Launch Up

    [Header("Launch Up Attack")] 
    [SerializeField] private float launchUpTime = 0.5f;
    [SerializeField] private float launchUpHeight = 10f;
    
    private void LaunchUp(PlayerController pc, IDamageable enemy, Attack a)
    {
        Timing.RunCoroutine(BeginLaunchUp(pc, enemy, a));
    }
    
    IEnumerator<float> BeginLaunchUp(PlayerController pc, IDamageable enemy, Attack a)
    {
        if (!enemy.gameObject.TryGetComponent(out EnemyController ec)) yield break;
        
        yield return Timing.WaitUntilTrue(() => pc.psm.canAttack);

        Vector3 direction = Vector3.up;
        
        ec.TraverseDistKnockback(direction, launchUpHeight, launchUpTime);
    }
    
    #endregion


    #region Follow Player Velocity
    
    [Header("Follow Player Velocity Attack")]
    [SerializeField] private float followVelocityMult = 1.2f;
    
    private void FollowPlayerVelocity(PlayerController pc, IDamageable enemy, Attack a)
    {
        Timing.RunCoroutine(BeginFollowPlayerVelocity(pc, enemy, a));
    }
    
    IEnumerator<float> BeginFollowPlayerVelocity(PlayerController pc, IDamageable enemy, Attack a)
    {
        if (!enemy.gameObject.TryGetComponent(out EnemyController ec)) yield break;
        
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
        
        Debug.Log(lastNonZeroVelocity);
        
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
    
    private void LaunchDown(PlayerController pc, IDamageable enemy, Attack a)
    {
        Timing.RunCoroutine(BeginLaunchDown(pc, enemy, a));
    }
    
    IEnumerator<float> BeginLaunchDown(PlayerController pc, IDamageable enemy, Attack a)
    {
        Debug.Log("Begin Launch Down");
        
        if (!enemy.gameObject.TryGetComponent(out EnemyController ec)) yield break;
        
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
}
