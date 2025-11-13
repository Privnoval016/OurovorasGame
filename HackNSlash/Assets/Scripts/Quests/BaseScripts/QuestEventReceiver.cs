using System;
using Extensions.EventBus;
using UnityEngine;

/**
 * Attach to a GameObject to receive quest events and execute corresponding strategies. Should exist in the
 * scene where the quest event needs to be handled.
 */
public class QuestEventReceiver : MonoBehaviour
{
    public QuestOutcome questOutcome;
    [SerializeReference] public IQuestExecutionStrategy executionStrategy;
    
    public bool canBeDeactivated = true;

    private EventBinding<QuestBroadcastEvent> eventBinding;
    
    private bool isTriggered;

    private void Start()
    {
        Debug.Log($"[QuestEventReceiver] Scene load check: Triggering quest outcome {questOutcome.name} as conditions are already met.");
        TryTrigger();
        
    }

    private void OnEnable()
    {
        eventBinding = new EventBinding<QuestBroadcastEvent>(OnQuestTriggered);
        EventBus<QuestBroadcastEvent>.Register(eventBinding);
    }

    private void OnDisable()
    {
        EventBus<QuestBroadcastEvent>.Deregister(eventBinding);
    }
    

    private void OnQuestTriggered(QuestBroadcastEvent e)
    {
        if (e.objective != questOutcome)
        {
            Debug.LogWarning($"[QuestEventReceiver] Received event for different outcome: {e.objective.name}");
            return;
        }

        bool result = e.Evaluator();
        Debug.Log($"[QuestEventReceiver] Received event for outcome: {questOutcome.name} with result: {result}");

        if (result)
        {
            TryTrigger();
        }
        else
        {
            Deactivate();
        }
    }

    private void TryTrigger()
    {
        if (!isTriggered)
        {
            var instance = QuestManager.Instance.GetOrCreateInstance(questOutcome);
            
            
            if (instance.TryCompleteByReceiver()) // if able to be executed, do so
            {
                Debug.Log($"[QuestEventReceiver] Triggering quest outcome: {questOutcome.name}");
                isTriggered = true;
                executionStrategy?.Initialize(this);
            }
            else
            {
                Debug.LogWarning($"[QuestEventReceiver] Quest outcome {questOutcome.name} does not need to be executed.");
            }
        }
    }
    
    private void Deactivate()
    {
        if (isTriggered && canBeDeactivated)
        {
            isTriggered = false;
            executionStrategy?.Deactivate();
        }
    }

    private void Update()
    {
        if (isTriggered) executionStrategy.Update();
    }

    private void LateUpdate()
    {
        if (isTriggered) executionStrategy.LateUpdate();
    }

    private void FixedUpdate()
    {
        if (isTriggered) executionStrategy.FixedUpdate();
    }
}
