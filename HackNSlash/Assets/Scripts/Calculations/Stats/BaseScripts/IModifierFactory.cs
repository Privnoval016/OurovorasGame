using UnityEngine;

public interface IModifierFactory<T> where T : IQueryKey<T>
{
    Modifier<T> Create(IStatChangeProvider statChange);
}

public class StatModifierFactory : IModifierFactory<StatQueryKey>
{
    public Modifier<StatQueryKey> Create(IStatChangeProvider statChange)
    {
        if (statChange is not InnateStatChange innateStatChange) return null;
        
        IModifierStrategy strategy = new StatChangeModifierStrategy(
            innateStatChange.changeType, innateStatChange.value);
        
        var key = new StatQueryKey(innateStatChange.innateStat);
        
        int priority = innateStatChange.changeType switch
        {
            ChangeType.Flat => 0,
            ChangeType.AdditivePercent => 1,
            ChangeType.MultiplicativePercent => 2,
            _ => 0
        };
        
        return new Modifier<StatQueryKey>(key, strategy, innateStatChange.duration, priority);
    }
}

public class StatusEffectModifierFactory : IModifierFactory<StatusEffectQueryKey>
{
    public Modifier<StatusEffectQueryKey> Create(IStatChangeProvider statChange)
    {
        if (statChange is not StatusEffectChange statusEffectChange) return null;
        
        Debug.LogWarning("Creating StatusEffectModifier for " + statusEffectChange.StatusEffect.name + 
                         " with stacks: " + statusEffectChange.stacks + 
                         " and duration: " + statusEffectChange.duration);
        
        IModifierStrategy strategy = new StatusEffectModifierStrategy(
            statusEffectChange.stacks, 0,  statusEffectChange.StatusEffect.MaxStacks);
        
        var key = new StatusEffectQueryKey(statusEffectChange.StatusEffect);
        
        return new Modifier<StatusEffectQueryKey>(key, strategy, statusEffectChange.duration);
    }
}