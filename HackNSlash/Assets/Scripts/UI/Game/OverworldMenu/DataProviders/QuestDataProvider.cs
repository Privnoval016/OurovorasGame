using System.Collections.Generic;
using Extensions.Patterns;
using Extensions.UI;
using UnityEngine;

/// <summary>
/// Implementation of IQuestDataProvider that bridges the UI with the quest system.
/// This is the backend connector for Tab 6 (Missions).
/// NOTE: Quest system not yet implemented, this provides placeholder data.
/// </summary>
public class QuestDataProvider : MonoBehaviour, IQuestDataProvider, IService
{
    [Header("Placeholder Quest Data")]
    [SerializeField] private List<QuestDisplayData> placeholderMainQuests = new List<QuestDisplayData>();
    [SerializeField] private List<QuestDisplayData> placeholderSideQuests = new List<QuestDisplayData>();
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        Services.Register<QuestDataProvider>(this);
        InitializePlaceholderData();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializePlaceholderData()
    {
        // Create placeholder quests if none exist
        if (placeholderMainQuests.Count == 0)
        {
            placeholderMainQuests.Add(QuestDisplayData.Default("The Ancient Prophecy", true));
            placeholderMainQuests.Add(QuestDisplayData.Default("Elemental Awakening", true));
            placeholderMainQuests.Add(QuestDisplayData.Default("The Final Confrontation", true));
        }
        
        if (placeholderSideQuests.Count == 0)
        {
            placeholderSideQuests.Add(QuestDisplayData.Default("Lost Artifact", false));
            placeholderSideQuests.Add(QuestDisplayData.Default("Village in Peril", false));
            placeholderSideQuests.Add(QuestDisplayData.Default("Master's Training", false));
        }
    }
    
    #endregion
    
    #region IQuestDataProvider Implementation
    
    /// <summary>
    /// Gets all main quests.
    /// </summary>
    public List<QuestDisplayData> GetMainQuests()
    {
        // TODO: Replace with actual quest system
        return new List<QuestDisplayData>(placeholderMainQuests);
    }
    
    /// <summary>
    /// Gets all side quests.
    /// </summary>
    public List<QuestDisplayData> GetSideQuests()
    {
        // TODO: Replace with actual quest system
        return new List<QuestDisplayData>(placeholderSideQuests);
    }
    
    /// <summary>
    /// Gets a specific quest by index.
    /// </summary>
    public QuestDisplayData GetQuest(int questIndex, bool isMainQuest)
    {
        List<QuestDisplayData> quests = isMainQuest ? placeholderMainQuests : placeholderSideQuests;
        
        if (questIndex < 0 || questIndex >= quests.Count)
        {
            Debug.LogWarning($"QuestDataProvider: Invalid quest index {questIndex}!");
            return QuestDisplayData.Default("Unknown Quest", isMainQuest);
        }
        
        return quests[questIndex];
    }
    
    #endregion
}

