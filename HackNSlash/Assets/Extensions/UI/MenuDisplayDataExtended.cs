using UnityEngine;
using UnityEngine.Video;

namespace Extensions.UI
{
    /// <summary>
    /// Data container for attack/ability display information.
    /// Used in the element progress and skill tree tabs.
    /// </summary>
    [System.Serializable]
    public class AttackDisplayData
    {
        [Tooltip("The attack/ability name")]
        public string attackName;
        
        [Tooltip("The attack/ability description")]
        public string description;
        
        [Tooltip("The attack icon")]
        public Sprite icon;
        
        [Tooltip("Optional video demonstration of the attack")]
        public VideoClip demonstrationVideo;
        
        [Tooltip("The element associated with this attack")]
        public ElementEffect element;
        
        [Tooltip("Whether this attack is currently unlocked")]
        public bool isUnlocked;
        
        [Tooltip("Whether this attack is currently equipped/assigned")]
        public bool isEquipped;
        
        /// <summary>
        /// Creates an empty attack display data.
        /// </summary>
        public static AttackDisplayData Empty()
        {
            return new AttackDisplayData
            {
                attackName = "Empty",
                description = "No attack assigned",
                element = ElementEffect.None,
                isUnlocked = false,
                isEquipped = false
            };
        }
    }
    
    /// <summary>
    /// Data container for element progress display.
    /// Shows the progression level and benefits for each element.
    /// </summary>
    [System.Serializable]
    public class ElementProgressData
    {
        [Tooltip("The element type")]
        public ElementEffect element;
        
        [Tooltip("Current level (0-10)")]
        [Range(0, 10)]
        public int currentLevel;
        
        [Tooltip("Progress towards next level (0-1)")]
        [Range(0f, 1f)]
        public float progressToNextLevel;
        
        [Tooltip("Descriptions for each level's benefits")]
        public string[] levelDescriptions = new string[10];
        
        /// <summary>
        /// Gets the description for a specific level.
        /// </summary>
        /// <param name="level">The level to get description for (0-10).</param>
        /// <returns>The level description or empty string if invalid.</returns>
        public string GetLevelDescription(int level)
        {
            if (level < 0 || level >= levelDescriptions.Length)
                return "";
                
            return levelDescriptions[level] ?? $"Level {level + 1} - Locked";
        }
    }
    
    /// <summary>
    /// Data container for skill tree node display.
    /// </summary>
    [System.Serializable]
    public class SkillNodeDisplayData
    {
        [Tooltip("Unique identifier for this node")]
        public string nodeId;
        
        [Tooltip("The node name")]
        public string nodeName;
        
        [Tooltip("The node description")]
        public string description;
        
        [Tooltip("The node icon")]
        public Sprite icon;
        
        [Tooltip("UI position in relative screen space (0-1)")]
        public Vector2 uiPosition;
        
        [Tooltip("Whether this node is unlocked (permanently available)")]
        public bool isUnlocked;
        
        [Tooltip("Whether this node is activated (currently equipped)")]
        public bool isActivated;
        
        [Tooltip("Whether this node can be unlocked (prerequisites met)")]
        public bool canUnlock;
        
        [Tooltip("Cost to unlock this node")]
        public int cost;
        
        [Tooltip("Optional video demonstration")]
        public VideoClip demonstrationVideo;
    }
    
    /// <summary>
    /// Data container for quest/mission display.
    /// </summary>
    [System.Serializable]
    public class QuestDisplayData
    {
        [Tooltip("The quest name")]
        public string questName;
        
        [Tooltip("The quest description")]
        public string description;
        
        [Tooltip("Whether this is a main quest or side quest")]
        public bool isMainQuest;
        
        [Tooltip("Whether the quest is completed")]
        public bool isCompleted;
        
        [Tooltip("Current progress (0-1)")]
        [Range(0f, 1f)]
        public float progress;
        
        [Tooltip("Progress text (e.g., '3/5 enemies defeated')")]
        public string progressText;
        
        /// <summary>
        /// Creates a default quest display data.
        /// </summary>
        public static QuestDisplayData Default(string name, bool mainQuest = false)
        {
            return new QuestDisplayData
            {
                questName = name,
                description = "Quest description not available.",
                isMainQuest = mainQuest,
                isCompleted = false,
                progress = 0f,
                progressText = "0%"
            };
        }
    }
    
    /// <summary>
    /// Data container for compendium entry display.
    /// </summary>
    [System.Serializable]
    public class CompendiumEntryData
    {
        [Tooltip("The entry name")]
        public string entryName;
        
        [Tooltip("The entry description")]
        public string description;
        
        [Tooltip("The entry icon/image")]
        public Sprite icon;
        
        [Tooltip("The category this entry belongs to")]
        public string category;
        
        [Tooltip("Whether this entry has been discovered")]
        public bool isDiscovered;
    }
}

