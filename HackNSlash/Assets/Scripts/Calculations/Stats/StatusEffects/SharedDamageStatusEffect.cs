using System;
using UnityEngine;


/**
* <summary>
* Shared damage status effect that causes a portion of the damage taken to be shared with allies.
* The shared damage percentage scales with the number of stacks.
* </summary>
*/
[CreateAssetMenu(fileName = "SharedDamage", menuName = "Stats/StatusEffects/SharedDamage", order = 1)]
public class SharedDamageStatusEffect : StatusEffect
{

    public override StatusEffectTargets Target => StatusEffectTargets.DynamicDamage;
    
    [SerializeField] private int maxStacks = 99;
    
    public override int MaxStacks => maxStacks;

    [Tooltip("Percentage of multiplicative damage shared per stack, e.g. 0.1 for 10%")]
    public float sharedDamagePercentagePerStack = 0.01f;

    public override float CalculateMultiplier(int stacks)
    {
        return 1 - Mathf.Pow(1 - sharedDamagePercentagePerStack, stacks);
    }
}
