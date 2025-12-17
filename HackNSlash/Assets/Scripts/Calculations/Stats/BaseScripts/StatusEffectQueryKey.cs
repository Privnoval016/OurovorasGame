using Extensions.Modifiers;

/**
 * <summary>
 * Generic wrapper for status effect queries, containing the key (the status effect to be queried).
 * </summary>
 */
public class StatusEffectQueryKey : IQueryKey<StatusEffectQueryKey>
{
    public StatusEffect Key { get; }

    public StatusEffectQueryKey(StatusEffect key)
    {
        Key = key;;
    }

    protected override bool OnEquals(object obj)
    {
        return obj is StatusEffectQueryKey other && Key.Equals(other.Key);
    }
    
    protected override int OnGetHashCode()
    {
        return Key.GetHashCode();
    }
}