using System;
using UnityEngine;


[CreateAssetMenu(fileName = "NoStatusEffect", menuName = "Stats/StatusEffects/NoStatusEffect", order = 1)]
public class NoStatusEffect : StatusEffect
{
    public override StatusEffectTargets Target => StatusEffectTargets.None;
    public override int MaxStacks => 0;

    public override float CalculateMultiplier(int stacks)
    {
        return 1f;
    }
}
