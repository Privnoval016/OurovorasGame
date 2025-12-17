using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class PlayerAttackStats : AttackStats
{
    [Header("Charge Stats")]
    public bool restoreCharge = true;
    [FormerlySerializedAs("chargeRequired")] public float charge = 0f;
    public float ultimateCharge = 8f;
    
    [Header("Style Stats")]
    
    public float baseStyleGain = 10f;
    
    public PlayerAttackStats(float damage, int statusEffectStacks = 0, float statusEffectDuration = 0) 
        : base(damage, statusEffectStacks, statusEffectDuration)
    {
    }
    
    public PlayerAttackStats() : base(0f, 0, 0f)
    {
    }
}