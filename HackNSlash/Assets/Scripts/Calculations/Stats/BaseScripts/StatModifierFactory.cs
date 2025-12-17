using Extensions.Modifiers;

public class StatModifierFactory : IModifierFactory<StatQueryKey>
{
    public Modifier<StatQueryKey> Create(IModifierProvider modifier)
    {
        if (modifier is not InnateStatChange innateStatChange) return null;
        
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