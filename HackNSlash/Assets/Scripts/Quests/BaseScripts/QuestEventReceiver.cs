using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.CustomMath.LogicComposition;
using Sirenix.OdinInspector;
using UnityEngine;

/**
 * Attach to a GameObject to receive and handle quest events based on the given strategy.
 */
public class QuestEventReceiver : MonoBehaviour
{
    [Header("Corresponding Quest")]
    public QuestEventData questEventData;
    
    [Header("Event Strategies")]
    [SerializeReference] 
    [ValueDropdown("GetBroadcastStrategies")]
    public ICondition<QuestEventData> questEventCondition;
    [SerializeReference] public IQuestEventExecutionStrategy questEventExecutionStrategy;
    
    private List<IQuestEventBroadcastStrategy> broadcastStrategies;

    public bool eventTriggered;

    private void Awake()
    {
        eventTriggered = false;
        Collect(questEventCondition, broadcastStrategies);
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
        
        foreach (var strategy in broadcastStrategies)
        {
            if (strategy == null || 
                questEventData.IsBroadcastComplete(strategy.strategyId))
                continue; // Already registered
            
            action(strategy);
            
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
    
    private void Collect<TContext>(ICondition<TContext> condition, List<IQuestEventBroadcastStrategy> list)
    {
        switch (condition)
        {
            case IQuestEventBroadcastStrategy leaf:
                list.Add(leaf);
                break;

            case AndCondition<TContext> andC:
                foreach (var child in andC.children)
                    Collect(child, list);
                break;

            case OrCondition<TContext> orC:
                foreach (var child in orC.children)
                    Collect(child, list);
                break;

            case NotCondition<TContext> notC:
                if (notC.child != null)
                    Collect(notC.child, list);
                break;
        }
    }
    
    private IEnumerable<ValueDropdownItem> GetBroadcastStrategies()
    {
        return ICondition<QuestEventData>.GetBroadcastStrategies();
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