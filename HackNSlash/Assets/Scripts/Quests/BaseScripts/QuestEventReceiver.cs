using System;
using Extensions.EventBus;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

/**
 * Attach to a GameObject to receive and handle quest events based on the given strategy.
 */
public class QuestEventReceiver : MonoBehaviour
{
    [Header("Corresponding Quest")]
    public QuestEventData questEventData;
    
    [Header("Event Strategies")]
    public BroadcastStrategy[] requiredBroadcastStrategies;
    [SerializeReference] public IQuestEventExecutionStrategy questEventExecutionStrategy;

    public bool eventTriggered;

    private void Awake()
    {
        eventTriggered = false;
        RunBroadcastCheck(strategy => strategy.Initialize(this), false);
    }

    private void OnEnable()
    {
        if (questEventData != null)
            questEventData.OnQuestTriggered += OnQuestTriggered;
    }

    private void OnDisable()
    {
        if (questEventData != null)
            questEventData.OnQuestTriggered -= OnQuestTriggered;
    }

    private void Update()
    {
        RunBroadcastCheck(strategy => strategy.Update());
        
        if (eventTriggered)
        {
            questEventExecutionStrategy.Update();
        }
    }

    private void LateUpdate()
    { 
        RunBroadcastCheck(strategy => strategy.LateUpdate());
        
        if (eventTriggered)
        {
            questEventExecutionStrategy.LateUpdate();
        }
    }

    private void FixedUpdate()
    {
        RunBroadcastCheck(strategy => strategy.FixedUpdate());
        
        if (eventTriggered)
        {
            questEventExecutionStrategy.FixedUpdate();
        }
    }

    private void OnDestroy()
    {
        RunBroadcastCheck(strategy => strategy.Destroy(), false);
    }

    private void RunBroadcastCheck(Action<IQuestEventBroadcastStrategy> action, bool skipIfTriggered = true)
    {
        if (skipIfTriggered && eventTriggered) return;
        
        foreach (var strategy in requiredBroadcastStrategies)
        {
            if (strategy.questEventBroadcastStrategy == null || 
                questEventData.IsBroadcastComplete(strategy.questEventBroadcastStrategy.strategyId))
                continue; // Already registered
            
            action(strategy.questEventBroadcastStrategy);
            
        }
    }
    
    private void OnQuestTriggered()
    {
        eventTriggered = true;
        questEventExecutionStrategy.Initialize(this);
    }
    
    public void RegisterBroadcast(string broadcastId)
    {
        if (questEventData.IsBroadcastComplete(broadcastId)) return; // Already registered
        
        questEventData.RegisterEvent(broadcastId);
    }
}

/**
 * Wrapper struct for serializing different broadcast strategies (without it, Unity does some weird stuff).
 */
[Serializable]
public struct BroadcastStrategy
{
    [SerializeReference] public IQuestEventBroadcastStrategy questEventBroadcastStrategy;
}