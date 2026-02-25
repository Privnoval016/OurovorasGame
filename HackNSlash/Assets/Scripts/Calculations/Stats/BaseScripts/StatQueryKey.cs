using Extensions.Modifiers;

/**
 * <summary>
 * Generic wrapper for stat queries, containing the key (the stat to be queried).
 * </summary>
 */
public class StatQueryKey : IQueryKey<StatQueryKey>
{
    public InnateStat Key { get; }

    public StatQueryKey(InnateStat key)
    {
        Key = key;
    }
    
    protected override bool OnEquals(object obj)
    {
        return obj is StatQueryKey other && Key == other.Key;
    }
    
    protected override int OnGetHashCode()
    {
        return Key.GetHashCode();
    }
}