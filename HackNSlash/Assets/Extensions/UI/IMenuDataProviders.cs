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
    /// Interface for providing equipment/inventory data to the UI.
    /// </summary>
    public interface IEquipmentDataProvider
    {
        /// <summary>
        /// Gets all accessories in the inventory.
        /// </summary>
        /// <returns>List of accessory item info.</returns>
        List<ItemUIInfo> GetAccessories();
        
        /// <summary>
        /// Gets all passive skills in the inventory.
        /// </summary>
        /// <returns>List of passive skill item info.</returns>
        List<ItemUIInfo> GetPassives();
        
        /// <summary>
        /// Gets the currently equipped accessory at the specified slot.
        /// </summary>
        /// <param name="slotIndex">The slot index (0-2).</param>
        /// <returns>The equipped accessory data or empty if none.</returns>
        EquippedItemDisplayData GetEquippedAccessory(int slotIndex);
        
        /// <summary>
        /// Gets the currently equipped passive at the specified slot.
        /// </summary>
        /// <param name="slotIndex">The slot index (0-2).</param>
        /// <returns>The equipped passive data or empty if none.</returns>
        EquippedItemDisplayData GetEquippedPassive(int slotIndex);
        
        /// <summary>
        /// Equips an accessory to the specified slot.
        /// </summary>
        /// <param name="slotIndex">The slot to equip to (0-2).</param>
        /// <param name="item">The item to equip.</param>
        /// <returns>True if successfully equipped.</returns>
        bool EquipAccessory(int slotIndex, ItemUIInfo item);
        
        /// <summary>
        /// Equips a passive skill to the specified slot.
        /// </summary>
        /// <param name="slotIndex">The slot to equip to (0-2).</param>
        /// <param name="item">The passive to equip.</param>
        /// <returns>True if successfully equipped.</returns>
        bool EquipPassive(int slotIndex, ItemUIInfo item);
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
        List<ItemUIInfo> GetAllItems();
        
        /// <summary>
        /// Gets items filtered by category.
        /// </summary>
        /// <param name="category">The category name (e.g., "Accessories", "Consumables").</param>
        /// <returns>List of items in the specified category.</returns>
        List<ItemUIInfo> GetItemsByCategory(string category);
        
        /// <summary>
        /// Gets all available item categories.
        /// </summary>
        /// <returns>Array of category names.</returns>
        string[] GetCategories();
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

