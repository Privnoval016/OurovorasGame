using System;
using System.Collections.Generic;
using System.Linq;

/**
 * <summary>
 * Mediator class that manages queries and modifiers. It allows adding modifiers that can modify the outcome of
 * queries. The mediator raises events when a query is performed, and all registered modifiers can handle these events
 * to modify the query context.
 * </summary>
 */
public class Mediator<T> where T : IQueryKey<T>
{
    private readonly List<Modifier<T>> Modifiers = new();
    
    public void PerformQuery(object sender, QueryContext<T> queryContext)
    {
        foreach (var modifier in Modifiers)
        {
            modifier.Handle(sender, queryContext);
        }
    }
    
    public void AddModifier(Modifier<T> mod)
    {
        Modifiers.Add(mod);
        Modifiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        
        mod.MarkedForRemoval = false;

        mod.OnDisposed += _ => Modifiers.Remove(mod);
    }

    public void Update()
    {
        foreach (var mod in Modifiers)
        {
            mod.Update();
        }
        
        foreach (var mod in Modifiers.Where(m => m.MarkedForRemoval).ToList())
        {
            mod.Dispose();
        }
    }
}

/**
 * <summary>
 * Interface for query types. This is a marker interface used to define different types of queries.
 * </summary>
 */
public interface IQueryKey<out T> { }

/**
 * <summary>
 * Context for a query, containing the key and value. This is an important wrapper class because it allows us to
 * abstract away the key of the query, letting us reuse the same Mediator and Modifier classes for different types
 * of queries (e.g., stats, status effects) without needing to create separate classes for each type.
 * </summary>
 */
public class QueryContext<TQueryKey> where TQueryKey : IQueryKey<TQueryKey>
{
    public TQueryKey Key;
    public int BaseValue;
    public int CurrentValue;
    
    public QueryContext(TQueryKey key, int baseValue, int currentValue)
    {
        Key = key;
        BaseValue = baseValue;
        CurrentValue = currentValue;
    }
}

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
}

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
}
