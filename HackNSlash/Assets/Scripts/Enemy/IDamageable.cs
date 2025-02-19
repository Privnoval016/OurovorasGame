using System;
using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;
using UnityEngine.PlayerLoop;


public abstract class IDamageable : MonoBehaviour, ITargetable
{
    [HideInInspector] public bool tookDamageThisAction;
    

    private void Start()
    {
        tookDamageThisAction = false;
        OnStart();
    }
    
    private void Update()
    {
        OnUpdate();
    }

    private void FixedUpdate()
    {
        OnFixedUpdate();
    }

    private void LateUpdate()
    {
        OnLateUpdate();
    }

    public virtual void OnStart()
    {
        
    }

    public virtual void OnUpdate()
    {
        
    }
    
    public virtual void OnFixedUpdate()
    {
        
    }
    
    public virtual void OnLateUpdate()
    {
        
    }


    public void OnHit(PlayerController pc, Attack a, int actionIndex = 0)
    {
        tookDamageThisAction = true;
        Timing.RunCoroutine(ResetHit(a));
        
        OnHitEvents.OnHitActionMap[a.hitInfo.onHitActions[actionIndex]](pc, this, a);
    }
    
    private IEnumerator<float> ResetHit(Attack a)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.attackCoolDown);
        tookDamageThisAction = false;
    }
    
}

