using System;
using Extensions.Timers;

/**
 * <summary>
 * Represents a modifier that can alter a value based on a specific strategy and can be applied for a certain duration.
 * </summary>
 */
public class Modifier<T> where T : IQueryKey<T>
{
    public T Key { get; }
    public IModifierStrategy Strategy { get; }
    
    public int Priority { get; set; } = 0;
    
    public bool MarkedForRemoval { get; set; }
    
    public event Action<Modifier<T>> OnDisposed = delegate { };

    private readonly CountdownTimer timer;
    
    public Modifier(T key, IModifierStrategy strategy, float duration = 0f, int priority = 0)
    {
        Key = key;
        Strategy = strategy;
        Priority = priority;
        
        if (duration <= 0f) return;
        
        timer = new CountdownTimer(duration);
        timer.OnTimerStop += () => MarkedForRemoval = true;
        timer.Start();
    }
    
    public void Update()
    {
        timer?.Tick();
    }

    public void Handle(object sender, QueryContext<T> queryContext)
    {
        if (!Key.Equals(queryContext.Key)) return;
        
        (queryContext.BaseValue, queryContext.CurrentValue) = 
            Strategy.Modify(queryContext.BaseValue, queryContext.CurrentValue);
    }
    
    public void Dispose()
    {
        MarkedForRemoval = true;
        OnDisposed?.Invoke(this);
    }
}