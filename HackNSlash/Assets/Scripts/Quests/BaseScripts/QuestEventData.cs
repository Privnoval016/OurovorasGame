using System;
using System.Collections.Generic;
using Extensions.CustomMath.LogicComposition;
using UnityEngine;

/**
 * ScriptableObject to hold quest event data (acts as an index for quest-related information). Also acts
 * as a persistent data holder for quest progress tracking (across scene loads, etc).
 */
[CreateAssetMenu(fileName = "New Quest Event Data", menuName = "Quests/Quest Event Data")]
public class QuestEventData : ScriptableObject
{
    public string description;
    
    [Header("Quest Requirements")]
    public int requiredEvents = 1;
    
    private HashSet<string> completedBroadcasts = new HashSet<string>();
    
    public event Action OnQuestTriggered;

    public void RegisterEvent(string broadcastId)
    {
        if (!completedBroadcasts.Add(broadcastId))
        {
            return; // Event from this broadcastId has already been counted
        }

        if (completedBroadcasts.Count >= requiredEvents)
        {
            Debug.Log("Quest Event activated: " + name);
            if (OnQuestTriggered?.Target != null)
                OnQuestTriggered?.Invoke();
        }
    }

    public bool IsBroadcastComplete(string broadcastId) =>
        completedBroadcasts.Contains(broadcastId);

    public void ResetProgress()
    {
        completedBroadcasts.Clear();
    }
    
    public override bool Equals(object other)
    {
        if (other is QuestEventData otherQuestEventData)
        {
            return this.name == otherQuestEventData.name;
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return name.GetHashCode();
    }
}