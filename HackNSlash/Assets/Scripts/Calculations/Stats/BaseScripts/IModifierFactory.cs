public interface IModifierFactory<T> where T : IQueryKey<T>
{
    Modifier<T> Create(T key, float value, float duration = 0f);
}

public class StatModifierFactory : IModifierFactory<StatQueryKey>
{
    public Modifier<StatQueryKey> Create(StatQueryKey key, float value, float duration = 0f)
    {
        return null; // TODO after implementing strategies
    }
}

public class StatusEffectModifierFactory : IModifierFactory<StatusEffectQueryKey>
{
    public Modifier<StatusEffectQueryKey> Create(StatusEffectQueryKey key, float value, float duration = 0f)
    {
        return null; // TODO after implementing strategies
    }
}