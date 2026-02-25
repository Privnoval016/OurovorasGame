using System;
using UnityEngine;


/**
* <summary>
* Damage over time status effect that deals a portion of the target's max health as damage over time.
* The damage over time percentage scales with the number of stacks.
* </summary>
*/
[CreateAssetMenu(fileName = "DamageOverTime", menuName = "Stats/StatusEffects/DamageOverTime", order = 1)]
public class DamageOverTimeStatusEffect : StatusEffect
{
    public override StatusEffectTargets Target => StatusEffectTargets.DynamicDamage;
    
    [SerializeField] private int maxStacks = 99;
    
    public override int MaxStacks => maxStacks;

    [Tooltip("Percentage of max health dealt as damage over time per stack per tick, e.g. 0.1 for 10%")]
    public float dotDamageMultiplier = 0.01f;

    public override float CalculateMultiplier(int stacks)
    {
        return dotDamageMultiplier * stacks;
    }
}
