using Extensions.Modifiers;
using UnityEngine;

public class StatusEffectModifierStrategy : IModifierStrategy
{
    private int stacksToAdd;
    private int min, max;
    
    public StatusEffectModifierStrategy(int stacksToAdd, int min, int max)
    {
        this.stacksToAdd = stacksToAdd;
        this.min = min;
        this.max = max;
    }
    
    public override (int, int) Modify(int baseValue, int currentValue)
    {
        currentValue += stacksToAdd;
        if (currentValue > max) currentValue = max;
        if (currentValue < min) currentValue = min;
        return (baseValue, currentValue);
    }
}