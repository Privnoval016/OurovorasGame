using System;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public enum OnHitActions
{
    BasicKnockBack,
    LaunchUp,
    FollowPlayerVelocity
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
    }

    #region Basic Knockback

    [Header("Basic Knockback Attack")] 
    [SerializeField][Range(0, 1)] private float midairLift = 0.85f;
    
    private void BasicKnockBack(PlayerController pc, IDamageable enemy, Attack a)
    {
        enemy.TryGetComponent(out EnemyController ec);
        if (ec != null) ec.pauseGravity = true;
        
        if (enemy.gameObject.TryGetComponent(out Rigidbody rb))
        {
            Vector3 direction = (enemy.transform.position - pc.transform.position).normalized;
            if (ec != null && !ec.IsGrounded)
            {
                direction = Vector3.up;
            }
            
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(direction * a.hitInfo.knockBackForce, ForceMode.VelocityChange);
            
        }

        if (ec != null) 
        {
            Timing.WaitUntilTrue(() => pc.canAttack);
            ec.pauseGravity = false;
        }
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
        if (!enemy.gameObject.TryGetComponent(out Rigidbody rb)) yield break;
        
        yield return Timing.WaitUntilTrue(() => pc.canAttack);

        Vector3 direction = Vector3.up;
        rb.linearVelocity = Vector3.zero;
        
        Timing.RunCoroutine(GameManager.TraverseDistanceInTime(rb, direction, launchUpHeight, launchUpTime), Segment.FixedUpdate);
    }
    
    #endregion


    #region Follow Player Velocity
    
    private void FollowPlayerVelocity(PlayerController pc, IDamageable enemy, Attack a)
    {
        Timing.RunCoroutine(BeginFollowPlayerVelocity(pc, enemy, a));
    }
    
    IEnumerator<float> BeginFollowPlayerVelocity(PlayerController pc, IDamageable enemy, Attack a)
    {
        if (!enemy.gameObject.TryGetComponent(out Rigidbody rb)) yield break;
        if (enemy.gameObject.TryGetComponent(out EnemyController ec)) ec.pauseGravity = true;
        if (enemy.gameObject.TryGetComponent(out Collider col)) Physics.IgnoreCollision(pc.GetComponent<Collider>(), col);
        
        while (pc.canAttack)
        {
            rb.linearVelocity = pc.rb.linearVelocity;
            Debug.Log(pc.rb.linearVelocity);
            yield return Timing.WaitForOneFrame;
        }
        
        if (ec != null) ec.pauseGravity = false;
        if (col != null) Physics.IgnoreCollision(pc.GetComponent<Collider>(), col, false);
    }

    #endregion
}
