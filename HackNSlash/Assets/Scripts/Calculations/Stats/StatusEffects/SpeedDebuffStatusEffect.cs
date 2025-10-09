using System;
using Sirenix.OdinInspector;
using UnityEngine;


/**
* <summary>
* Speed debuff status effect that reduces the target's speed.
* The reduction is multiplicative and scales with the number of stacks.
* </summary>
*/
[CreateAssetMenu(fileName = "SpeedDebuff", menuName = "Stats/StatusEffects/SpeedDebuff", order = 1)]
public class SpeedDebuffStatusEffect : StatusEffect
{

    [ReadOnly] public override StatusEffectTargets Target => StatusEffectTargets.Speed;
    
    [SerializeField] private int maxStacks = 99;
    
    public override int MaxStacks => maxStacks;

    [Tooltip("Percentage of multiplicative speed reduction per stack, e.g. 0.1 for 10%")]
    public float speedDebuffMultiplierPerStack = 0.01f;

    public override float CalculateMultiplier(int stacks)
    {
        return Mathf.Pow(1 - speedDebuffMultiplierPerStack, stacks);
    }
}
