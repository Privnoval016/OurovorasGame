using System.Collections.Generic;
using UnityEngine;

namespace Extensions.UI
{
    /// <summary>
    /// Interface for providing player character data to the UI.
    /// Implementations should retrieve data from PlayerStats, PlayerInventory, etc.
    /// </summary>
    public interface IPlayerDataProvider
    {
        /// <summary>
        /// Gets the current player stats for display.
        /// </summary>
        /// <returns>Player stats display data.</returns>
        PlayerStatsDisplayData GetPlayerStats();
        
        /// <summary>
        /// Gets the equipped items for all slots.
        /// </summary>
        /// <returns>Array of equipped item data.</returns>
        EquippedItemDisplayData[] GetEquippedItems();
    }
    

    
    /// <summary>
    /// Interface for providing element progression data to the UI.
    /// </summary>
    public interface IElementProgressDataProvider
    {
        /// <summary>
        /// Gets the progression data for a specific element.
        /// </summary>
        /// <param name="element">The element type.</param>
        /// <returns>Element progress data.</returns>
        ElementProgressData GetElementProgress(ElementEffect element);
        
        /// <summary>
        /// Gets all available elements.
        /// </summary>
        /// <returns>Array of available element types.</returns>
        ElementEffect[] GetAvailableElements();
        
        /// <summary>
        /// Gets the attacks assigned to the specified element and button.
        /// </summary>
        /// <param name="element">The element type.</param>
        /// <returns>Array of attack display data (3 attacks for X, Y, A buttons).</returns>
        AttackDisplayData[] GetAssignedAttacks(ElementEffect element);
        
        /// <summary>
        /// Gets all available attacks for the specified element.
        /// </summary>
        /// <param name="element">The element type.</param>
        /// <returns>List of available attacks for this element.</returns>
        List<AttackDisplayData> GetAvailableAttacks(ElementEffect element);
        
        /// <summary>
        /// Assigns an attack to a button for the specified element.
        /// </summary>
        /// <param name="element">The element type.</param>
        /// <param name="buttonIndex">The button index (0=X, 1=Y, 2=A).</param>
        /// <param name="attackIndex">The index of the attack to assign.</param>
        bool AssignAttack(ElementEffect element, int buttonIndex, int attackIndex);
    }
    
    /// <summary>
    /// Interface for providing skill tree data to the UI.
    /// </summary>
    public interface ISkillTreeDataProvider
    {
        /// <summary>
        /// Gets all skill nodes in the tree.
        /// </summary>
        /// <returns>List of skill node display data.</returns>
        List<SkillNodeDisplayData> GetAllNodes();
        
        /// <summary>
        /// Gets a specific skill node by ID.
        /// </summary>
        /// <param name="nodeId">The node ID.</param>
        /// <returns>The skill node data.</returns>
        SkillNodeDisplayData GetNode(string nodeId);
        
        /// <summary>
        /// Gets the start node of the skill tree.
        /// </summary>
        /// <returns>The start node data.</returns>
        SkillNodeDisplayData GetStartNode();
        
        /// <summary>
        /// Gets the nearest node in a given direction from the current node.
        /// </summary>
        /// <param name="currentNodeId">Current node ID.</param>
        /// <param name="direction">Direction to search.</param>
        /// <returns>Nearest node in that direction.</returns>
        SkillNodeDisplayData GetNearestNodeInDirection(string currentNodeId, Vector2 direction);
        
        /// <summary>
        /// Checks if a node is unlocked.
        /// </summary>
        bool IsNodeUnlocked(string nodeId);
        
        /// <summary>
        /// Checks if a node is activated.
        /// </summary>
        bool IsNodeActivated(string nodeId);
        
        /// <summary>
        /// Attempts to unlock a node (spend points).
        /// </summary>
        bool UnlockNode(string nodeId);
        
        /// <summary>
        /// Activates a node (toggle on).
        /// </summary>
        bool ActivateNode(string nodeId);
        
        /// <summary>
        /// Deactivates a node (toggle off).
        /// </summary>
        bool DeactivateNode(string nodeId);
        
        /// <summary>
        /// Gets available skill points.
        /// </summary>
        int GetAvailableSkillPoints();
    }
    
    /// <summary>
    /// Interface for providing inventory data to the UI.
    /// </summary>
    public interface IInventoryDataProvider
    {
        /// <summary>
        /// Gets all items in the inventory.
        /// </summary>
        /// <returns>List of all inventory items.</returns>
        List<ItemUIInfo<InventoryStack>> GetAllItems();
        
        /// <summary>
        /// Gets items filtered by category.
        /// </summary>
        /// <param name="category">The category name (e.g., "Accessories", "Consumables").</param>
        /// <returns>List of items in the specified category.</returns>
        List<ItemUIInfo<InventoryStack>> GetItemsByCategory(string category);
        
        /// <summary>
        /// Gets all available item categories.
        /// </summary>
        /// <returns>Array of category names.</returns>
        string[] GetCategories();
        
        /// <summary>
        /// Uses an item (calls its usage strategy).
        /// </summary>
        /// <param name="itemName">The name of the item to use.</param>
        /// <returns>True if the item was used successfully.</returns>
        bool UseItem(string itemName);
        
        /// <summary>
        /// Discards an item from the inventory.
        /// </summary>
        /// <param name="itemName">The name of the item to discard.</param>
        /// <returns>True if the item was discarded successfully.</returns>
        bool DiscardItem(string itemName);
        
        /// <summary>
        /// Checks if an item can be used.
        /// </summary>
        /// <param name="itemName">The name of the item.</param>
        /// <returns>True if the item has a usable strategy.</returns>
        bool CanItemBeUsed(string itemName);
    }
    
    /// <summary>
    /// Interface for providing quest/mission data to the UI.
    /// </summary>
    public interface IQuestDataProvider
    {
        /// <summary>
        /// Gets all main quests.
        /// </summary>
        /// <returns>List of main quest data.</returns>
        List<QuestDisplayData> GetMainQuests();
        
        /// <summary>
        /// Gets all side quests.
        /// </summary>
        /// <returns>List of side quest data.</returns>
        List<QuestDisplayData> GetSideQuests();
        
        /// <summary>
        /// Gets a specific quest by index.
        /// </summary>
        /// <param name="questIndex">The quest index.</param>
        /// <param name="isMainQuest">Whether this is a main quest.</param>
        /// <returns>The quest data.</returns>
        QuestDisplayData GetQuest(int questIndex, bool isMainQuest);
    }
    
    /// <summary>
    /// Interface for providing compendium data to the UI.
    /// </summary>
    public interface ICompendiumDataProvider
    {
        /// <summary>
        /// Gets all compendium entries.
        /// </summary>
        /// <returns>List of all compendium entries.</returns>
        List<CompendiumEntryData> GetAllEntries();
        
        /// <summary>
        /// Gets entries filtered by category.
        /// </summary>
        /// <param name="category">The category name.</param>
        /// <returns>List of entries in the specified category.</returns>
        List<CompendiumEntryData> GetEntriesByCategory(string category);
        
        /// <summary>
        /// Gets all available compendium categories.
        /// </summary>
        /// <returns>Array of category names.</returns>
        string[] GetCategories();
    }
}

