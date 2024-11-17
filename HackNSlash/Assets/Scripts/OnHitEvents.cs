using System;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public enum OnHitActions
{
    BasicKnockBack,
    LaunchUp
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
    }

    #region Basic Knockback
    
    private void BasicKnockBack(PlayerController pc, IDamageable enemy, Attack a)
    {
        if (enemy.gameObject.TryGetComponent(out Rigidbody rb))
        {
            Vector3 direction = (enemy.transform.position - pc.transform.position).normalized;
            rb.AddForce(direction * (a.knockBackForce), ForceMode.VelocityChange);
            
            Debug.Log("Basic Knockback");
        }
    }

    #endregion
    
    #region Launch Up

    [Header("Launch Up Attack")] 
    [SerializeField] private float launchUpForce = 40f;
    
    private void LaunchUp(PlayerController pc, IDamageable enemy, Attack a)
    {
        Timing.RunCoroutine(BeginLaunchUp(pc, enemy, a));
    }
    
    IEnumerator<float> BeginLaunchUp(PlayerController pc, IDamageable enemy, Attack a)
    {
        if (!enemy.gameObject.TryGetComponent(out Rigidbody rb)) yield break;
        
        yield return Timing.WaitUntilTrue(() => pc.canAttack);
        

        float force = launchUpForce;
        
        Debug.Log(force + " b");
        if (rb.linearVelocity.y < 0)
            force -= rb.linearVelocity.y;
		
        rb.AddForce(Vector3.up * force * rb.mass, ForceMode.Impulse);
    }
    
    #endregion
}
