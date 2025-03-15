using System.Collections.Generic;
using MEC;
using UnityEngine;
using Extensions.Utils;


// Things with health that can take damage
public abstract class LockOnTarget : KinematicBehaviour
{
    public float radius = 3f;
    
    [HideInInspector]
    public Collider col;
    
    [HideInInspector] public bool tookDamageThisAction;
    
    public virtual Vector3 TargetedPosition(float deltaTime = 0)
    {
        return transform.position;
    }

    private void Start()
    {
        tookDamageThisAction = false;
        SetKinematicAttributes();
        OnStart();
    }
    
    private void Update()
    {
        UpdateKinematicAttributes();
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
    
    public virtual void OnHit(PlayerController pc, Attack a, int actionIndex = 0)
    {
        tookDamageThisAction = true;
        this.RunSegmentCoroutine(ResetHit(a));

        TakeDamage();
    }
    
    private IEnumerator<float> ResetHit(Attack a)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.attackCoolDown);
        tookDamageThisAction = false;
    }

    public virtual void TakeDamage()
    {
        // add later
    }
    
}

