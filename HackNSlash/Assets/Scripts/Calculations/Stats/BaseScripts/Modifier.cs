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
    
    public bool MarkedForRemoval { get; set; }
    
    public event Action<Modifier<T>> OnDisposed = delegate { };

    private readonly CountdownTimer timer;
    
    protected Modifier(T key, IModifierStrategy strategy, float duration = 0f)
    {
        Key = key;
        Strategy = strategy;
        
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
        
        queryContext.Value = Strategy.Modify(queryContext.Value);
    }
    
    public void Dispose()
    {
        MarkedForRemoval = true;
        OnDisposed?.Invoke(this);
    }
}