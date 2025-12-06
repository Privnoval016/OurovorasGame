using Extensions.Modifiers;
using UnityEngine;

public class StatusEffectModifierFactory : IModifierFactory<StatusEffectQueryKey>
{
    public Modifier<StatusEffectQueryKey> Create(IModifierProvider modifier)
    {
        if (modifier is not StatusEffectChange statusEffectChange) return null;
        
        Debug.LogWarning("Creating StatusEffectModifier for " + statusEffectChange.StatusEffect.name + 
                         " with stacks: " + statusEffectChange.stacks + 
                         " and duration: " + statusEffectChange.duration);
        
        IModifierStrategy strategy = new StatusEffectModifierStrategy(
            statusEffectChange.stacks, 0,  statusEffectChange.StatusEffect.MaxStacks);
        
        var key = new StatusEffectQueryKey(statusEffectChange.StatusEffect);
        
        return new Modifier<StatusEffectQueryKey>(key, strategy, statusEffectChange.duration);
    }
}