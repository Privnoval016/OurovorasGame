using System;
using UnityEngine;
using UnityEngine.Serialization;

/**
 * <summary>
 * Status effects that can be applied to entities, affecting their stats or behavior.
 * Each status effect has a target (the stat it affects), a maximum number of stacks,
 * and a method to calculate the effect's multiplier based on the number of stacks.
 * </summary>
 */
public abstract class StatusEffect
{
    public abstract StatusEffectTargets Target { get; }
    public abstract int MaxStacks { get; }
    
    public abstract float CalculateMultiplier(int stacks);
}

[Serializable]
public class NoStatusEffect : StatusEffect
{
    public override StatusEffectTargets Target => StatusEffectTargets.None;
    public override int MaxStacks => 0;
    
    public override float CalculateMultiplier(int stacks)
    {
        return 1f;
    }

    public override bool Equals(object obj)
    {
        return obj is NoStatusEffect;
    }
    
    public override int GetHashCode()
    {
        return GetType().GetHashCode();
    }
}

/**
 * <summary>
 * Speed debuff status effect that reduces the target's speed.
 * The reduction is multiplicative and scales with the number of stacks.
 * </summary>
 */
[Serializable]
public class SpeedDebuffStatusEffect : StatusEffect
{
    
    public override StatusEffectTargets Target => StatusEffectTargets.Speed;
    public override int MaxStacks => 99;

    [Tooltip("Percentage of multiplicative speed reduction per stack, e.g. 0.1 for 10%")]
    public float speedDebuffMultiplierPerStack = 0.01f;
    
    public override float CalculateMultiplier(int stacks)
    {
        return Mathf.Pow(1 - speedDebuffMultiplierPerStack, stacks);
    }
    
    public override bool Equals(object obj)
    {
        return obj is SpeedDebuffStatusEffect;
    }
    
    public override int GetHashCode()
    {
        return GetType().GetHashCode();
    }
}

/**
 * <summary>
 * Vulnerability status effect that increases the damage taken by the target.
 * The increase is multiplicative and scales with the number of stacks.
 * </summary>
 */
[Serializable]
public class VulnerabilityStatusEffect : StatusEffect
{
    
    public override StatusEffectTargets Target => StatusEffectTargets.DamageTaken;
    public override int MaxStacks => 99;
    
    [Tooltip("Percentage of multiplicative extra damage taken per stack, e.g. 0.1 for 10%")]
    public float vulnerabilityMultiplierPerStack = 0.01f;
    
    public override float CalculateMultiplier(int stacks)
    {
        return Mathf.Pow(1 + vulnerabilityMultiplierPerStack, stacks);
    }
    
    public override bool Equals(object obj)
    {
        return obj is VulnerabilityStatusEffect;
    }
    
    public override int GetHashCode()
    {
        return GetType().GetHashCode();
    }
}

/**
 * <summary>
 * Shared damage status effect that causes a portion of the damage taken to be shared with allies.
 * The shared damage percentage scales with the number of stacks.
 * </summary>
 */
[Serializable]
public class SharedDamageStatusEffect : StatusEffect
{
    
    public override StatusEffectTargets Target => StatusEffectTargets.DynamicDamage;
    public override int MaxStacks => 99;
    
    [Tooltip("Percentage of multiplicative damage shared per stack, e.g. 0.1 for 10%")]
    public float sharedDamagePercentagePerStack = 0.01f;
    
    public override float CalculateMultiplier(int stacks)
    {
        return 1 - Mathf.Pow(1 - sharedDamagePercentagePerStack, stacks);
    }
    
    public override bool Equals(object obj)
    {
        return obj is SharedDamageStatusEffect;
    }
    
    public override int GetHashCode()
    {
        return GetType().GetHashCode();
    }
}

/**
 * <summary>
 * Damage over time status effect that deals a portion of the target's max health as damage over time.
 * The damage over time percentage scales with the number of stacks.
 * </summary>
 */
[Serializable]
public class DamageOverTimeStatusEffect : StatusEffect
{
    
    public override StatusEffectTargets Target => StatusEffectTargets.DynamicDamage;
    public override int MaxStacks => 99;
    
    [Tooltip("Percentage of max health dealt as damage over time per stack per tick, e.g. 0.1 for 10%")]
    public float dotDamageMultiplier = 0.01f;
    
    public override float CalculateMultiplier(int stacks)
    {
        return dotDamageMultiplier * stacks;
    }
    
    public override bool Equals(object obj)
    {
        return obj is DamageOverTimeStatusEffect;
    }
    
    public override int GetHashCode()
    {
        return GetType().GetHashCode();
    }
}