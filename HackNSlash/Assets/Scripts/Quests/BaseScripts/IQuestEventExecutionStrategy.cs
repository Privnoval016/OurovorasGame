using System;

/**
 * Strategy pattern for defining how quest event receivers handle triggering events (spawn something, shake player, etc)
 */
[Serializable]
public abstract class IQuestEventExecutionStrategy
{
    protected QuestEventReceiver questEventReceiver;
    
    public void Initialize(QuestEventReceiver receiver)
    {
        questEventReceiver = receiver;
        OnInitialize();
    }
    
    protected virtual void OnInitialize() { }
    
    public void Update() => OnUpdate();
    
    protected virtual void OnUpdate() { }
    
    public void LateUpdate() => OnLateUpdate();
    
    protected virtual void OnLateUpdate() { }
    
    public void FixedUpdate() => OnFixedUpdate();
    
    protected virtual void OnFixedUpdate() { }
}