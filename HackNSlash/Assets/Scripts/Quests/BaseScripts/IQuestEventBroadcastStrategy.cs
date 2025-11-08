using System;
using Extensions.CustomMath.LogicComposition;
using UnityEngine;

/**
 * Strategy pattern for defining when quest event broadcasts are triggered (e.g., collision, interaction,
 * holding something, etc.).
 */
[Serializable]
public abstract class IQuestEventBroadcastStrategy : ICondition<QuestEventData>
{
    [Header("Strategy Identifier")]
    [Tooltip("Unique identifier for this broadcast strategy (doesn't have to be globally unique, just unique per quest event receiver).")]
    public string strategyId;
    [Space(10)]
    
    protected QuestEventReceiver Receiver;
    /**
     * Called when the strategy is started.
     */
    public void Initialize(QuestEventReceiver r)
    {
        Receiver = r;
        OnInitialize();
    }

    /**
     * Called every frame to determine whether to broadcast the event.
     */
    public void Update() => OnUpdate();

    public void LateUpdate() => OnLateUpdate();
    
    public void Destroy() => OnDestroy();

    /**
     * Called at fixed intervals for determining whether to broadcast the event.
     */
    public void FixedUpdate() => OnFixedUpdate();
    
    /**
     * Override this method to implement custom initialization logic.
     */
    protected virtual void OnInitialize() { }

    
    /**
     * Override this method to implement custom update logic.
     */
    protected virtual void OnUpdate()
    {
        
    }
    
    /**
     * Override this method to implement custom late update logic.
     */
    protected virtual void OnLateUpdate()
    {

    }

    /**
     * Override this method to implement custom fixed update logic.
     */
    protected virtual void OnFixedUpdate()
    {

    }
    
    /**
     * Override this method to implement custom destruction logic.
     */
    protected virtual void OnDestroy() { }
    
    /**
     * Call this method to broadcast the event to the receiver.
     */
    protected void Broadcast(QuestEventReceiver receiver)
    {
        receiver.RegisterBroadcast(strategyId);
    }

    public bool Evaluate(QuestEventData context)
    {
        return context.IsBroadcastComplete(strategyId);
    }
}