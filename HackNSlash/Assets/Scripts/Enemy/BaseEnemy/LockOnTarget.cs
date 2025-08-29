using System.Collections.Generic;
using MEC;
using UnityEngine;
using Extensions.Utils;


// Things with health that can take damage
public abstract class LockOnTarget : KinematicBehaviour
{
    public float radius = 3f;
    
    public Collider col;
    
    private Dictionary<Attack, DamageCooldown> damageCooldowns = new Dictionary<Attack, DamageCooldown>();
    public bool IsMidAttack => damageCooldowns.Count > 0;
    
    public virtual Vector3 TargetedPosition(float deltaTime = 0)
    {
        return transform.position;
    }

    private void Start()
    {
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
            damageCooldowns.Add(a, new DamageCooldown(a.hitInfo.attackCoolDown, Time.time));
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

        TakeDamage(element, pc, a);
    }
    
    private IEnumerator<float> ResetHit(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.hitInfo.attackCoolDown);
        if (damageCooldowns.ContainsKey(a))
        {
            damageCooldowns.Remove(a);
            if (damageCooldowns.Count == 0)
            {
                pc.psm.EnemiesInHit.Remove(this);
            }
        }
    }

    public virtual void TakeDamage(ElementEffect element, PlayerController pc, Attack a)
    {
        
        Debug.Log($"{gameObject.name} took {a.name} attack from {pc.gameObject.name} with element {element}.");
        
        // Override this method to implement damage logic
    }
    
    public class DamageCooldown
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

