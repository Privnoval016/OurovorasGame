using Sirenix.OdinInspector;
using UnityEngine;


/**
* <summary>
* Status effects that can be applied to entities, affecting their stats or behavior.
* Each status effect has a target (the stat it affects), a maximum number of stacks,
* and a method to calculate the effect's multiplier based on the number of stacks.
* </summary>
*/
public abstract class StatusEffect : ScriptableObject
{
    [Header("UI Parameters")]
    public Sprite icon;
    public Color backgroundColor = Color.white;
    
    [ReadOnly] public abstract StatusEffectTargets Target { get; }
    public abstract int MaxStacks { get; }

    public abstract float CalculateMultiplier(int stacks);

    public override bool Equals(object other)
    {
        return other?.GetType() == GetType();
    }
    
    public override int GetHashCode()
    {
        return GetType().GetHashCode();
    }
}