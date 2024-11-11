using System;
using System.Collections.Generic;
using UnityEngine;

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
            rb.AddForce(direction * a.knockBackForce, ForceMode.Impulse);
            
            Debug.Log("Basic Knockback");
        }
    }

    #endregion
    
    #region Launch Up
    
    private void LaunchUp(PlayerController pc, IDamageable enemy, Attack a)
    {
        pc.rb.AddForce(Vector3.up * a.knockBackForce, ForceMode.Impulse);
    }
    
    #endregion
}
