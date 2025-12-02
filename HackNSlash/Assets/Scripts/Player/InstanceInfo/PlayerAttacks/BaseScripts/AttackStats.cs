using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class AttackStats
{
    [Header("Charge Stats")]
    public bool restoreCharge = true;
    [FormerlySerializedAs("chargeRequired")] public float charge = 0f;
    public float ultimateCharge = 8f;
    
    [Header("Damage Stats")]
    public float damage = 0f;

    public int statusEffectStacks = 1;
    
    public float statusEffectDuration = 5f;
    
    [Header("Style Stats")]
    
    public float baseStyleGain = 10f;


    public AttackStats()
    {
    }

    public AttackStats(AttackStats a)
    {
        restoreCharge = a.restoreCharge;
        charge = a.charge;
        damage = a.damage;
    }
}