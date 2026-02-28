using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class PlayerAttackStats : AttackStats
{
    [Header("Shield Damage")] 
    public float shieldDamage = 10f;
    [Header("Charge Stats")]
    public bool restoreCharge = true;
    [FormerlySerializedAs("chargeRequired")] public float charge = 0f;
    public float ultimateCharge = 8f;
    
    [Header("Style Stats")]
    
    public float baseStyleGain = 10f;
    
    public PlayerAttackStats(float damage, float shieldDamage, int statusEffectStacks = 0, float statusEffectDuration = 0) 
        : base(damage, statusEffectStacks, statusEffectDuration)
    {
        this.shieldDamage = shieldDamage;
    }
    
    public PlayerAttackStats() : base(0f, 0, 0f)
    {
    }
}