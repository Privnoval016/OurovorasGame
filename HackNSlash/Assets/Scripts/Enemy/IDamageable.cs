using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using MEC;
using UnityEngine;


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

    public virtual void OnStart()
    {
        
    }

    public virtual void OnUpdate()
    {
        
    }
    
    public virtual void OnFixedUpdate()
    {
        
    }


    public void OnHit(PlayerController pc, Attack a)
    {
        tookDamageThisAction = true;
        Timing.RunCoroutine(ResetHit(a));
        
        OnHitEvents.OnHitActionMap[a.hitInfo.onHitAction](pc, this, a);
    }
    
    private IEnumerator<float> ResetHit(Attack a)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.attackCoolDown);
        tookDamageThisAction = false;
    }
}

