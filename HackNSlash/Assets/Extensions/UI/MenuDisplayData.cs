using UnityEngine;

namespace Extensions.UI
{
    /// <summary>
    /// Data container for player stats display in the menu.
    /// This is a DTO (Data Transfer Object) to decouple UI from game logic.
    /// </summary>
    [System.Serializable]
    public class PlayerStatsDisplayData
    {
        [Tooltip("Player level")]
        public int level;
        
        [Tooltip("Current experience points")]
        public int currentExperience;
        
        [Tooltip("Experience needed for next level")]
        public int experienceToNextLevel;
        
        [Tooltip("Maximum health")]
        public float maxHealth;
        
        [Tooltip("Current health")]
        public float currentHealth;
        
        [Tooltip("Maximum charge/energy")]
        public float maxCharge;
        
        [Tooltip("Current charge/energy")]
        public float currentCharge;
        
        [Tooltip("Strength stat")]
        public int strength;
        
        [Tooltip("Defense stat")]
        public int defense;
        
        /// <summary>
        /// Creates a default empty stats data.
        /// </summary>
        public static PlayerStatsDisplayData Default()
        {
            return new PlayerStatsDisplayData
            {
                level = 1,
                currentExperience = 0,
                experienceToNextLevel = 100,
                maxHealth = 100f,
                currentHealth = 100f,
                maxCharge = 100f,
                currentCharge = 100f,
                strength = 10,
                defense = 10
            };
        }
    }
    
    /// <summary>
    /// Data container for equipped item display.
    /// </summary>
    [System.Serializable]
    public class EquippedItemDisplayData
    {
        [Tooltip("The item icon")]
        public Sprite icon;
        
        [Tooltip("The item name")]
        public string itemName;
        
        [Tooltip("The item description")]
        public string itemDescription;
        
        [Tooltip("The item rarity")]
        public Rarity rarity;
        
        [Tooltip("Whether this slot is empty")]
        public bool isEmpty;
        
        /// <summary>
        /// Creates an empty slot data.
        /// </summary>
        public static EquippedItemDisplayData Empty()
        {
            return new EquippedItemDisplayData
            {
                isEmpty = true,
                itemName = "Empty",
                itemDescription = "No item equipped",
                rarity = Rarity.Common
            };
        }
    }
}

