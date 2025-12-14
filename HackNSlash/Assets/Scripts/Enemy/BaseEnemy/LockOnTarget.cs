using System.Collections.Generic;
using MEC;
using UnityEngine;
using Extensions.Utils;

/**
 * <summary>
 * Base class for enemies that can be locked onto by the player.
 * </summary>
 */
public abstract class LockOnTarget : KinematicBehaviour
{
    [Header("Lock On Target Parameters")]
    
    public IDamageable damageable;
    
    private StatusEffectModifierFactory statusEffectModifierFactory;
    
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
        damageable = GetComponent<IDamageable>();
        
        statusEffectModifierFactory = new StatusEffectModifierFactory();
        
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
        
        StatusEffectChange effect = new StatusEffectChange
        {
            StatusEffect = Services.Get<ElementSystem>().GetElementData(element).GetStatusEffect(),
            stacks = a.stats.statusEffectStacks,
            duration = a.stats.statusEffectDuration
        };

        var statusEffectModifier = statusEffectModifierFactory.Create(effect);
        damageable?.ApplyStatusEffect(statusEffectModifier);
        damageable?.TakeDamage(element, a.stats.damage);
    }
    
    public virtual void OnStagger(ElementEffect element, PlayerController pc, Attack a, Transform attackerTransform, int actionIndex = 0)
    {
        OnHit(element, pc, a, attackerTransform, actionIndex);
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