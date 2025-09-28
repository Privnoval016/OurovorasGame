using System.Collections.Generic;
using MEC;
using UnityEngine;
using Extensions.Utils;


// Things with health that can take damage
public abstract class LockOnTarget : KinematicBehaviour
{
    [Header("Lock On Target Parameters")]
    
    public EnemyController ts;
    
    public float radius = 3f;
    
    public Collider col;
    
    private Dictionary<Attack, DamageCooldown> damageCooldowns = new();
    public bool IsMidAttack => damageCooldowns.Count > 0;
    
    public virtual Vector3 TargetedPosition(float deltaTime = 0)
    {
        return transform.position;
    }

    private void Start()
    {
        ts = GetComponent<EnemyController>();
        
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
    
    private void SetDamageCooldown(Attack a)
    {
        if (damageCooldowns.ContainsKey(a))
        {
            damageCooldowns[a].lastHitTimestamp = Time.time;
        }
        else
        {
            damageCooldowns.Add(a, new DamageCooldown(a.hitInfo.hitCoolDown, Time.time));
        }
    }
    
    public int GetDamageCooldownCount()
    {
        return damageCooldowns.Count;
    }

    public bool TookDamageThisAction(Attack a)
    {
        return damageCooldowns.ContainsKey(a);
    }
    
    public virtual void OnHit(ElementEffect element, PlayerController pc, Attack a, Transform attackerTransform, int actionIndex = 0)
    {
        SetDamageCooldown(a);
        pc.psm.EnemiesInHit.Add(this);
        this.RunSegmentCoroutine(ResetHit(pc, a));

        ts.TakeDamage(element, pc, a);
    }
    
    private IEnumerator<float> ResetHit(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.hitCoolDown);
        if (damageCooldowns.ContainsKey(a))
        {
            damageCooldowns.Remove(a);
            if (damageCooldowns.Count == 0)
            {
                pc.psm.EnemiesInHit.Remove(this);
            }
        }
    }
    
    private class DamageCooldown
    {
        public float cooldownTime;
        public float lastHitTimestamp;

        public DamageCooldown(float cooldownTime, float lastHitTimestamp)
        {
            this.cooldownTime = cooldownTime;
            this.lastHitTimestamp = lastHitTimestamp;
        }
    }
    
}

