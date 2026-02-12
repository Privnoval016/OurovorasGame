using System.Collections.Generic;
using System.Linq;
using Extensions.UI;
using Extensions.EventBus;
using UnityEngine;

/// <summary>
/// Implementation of IEquipmentDataProvider that bridges the UI with PlayerInventory.
/// This is the backend connector for Tab 2 (Equipment Selection).
/// Locally managed by OverworldMenuUI - not a global service.
/// </summary>
public class EquipmentDataProvider : MonoBehaviour, IEquipmentDataProvider
{
    private RuntimePlayerStatus runtimePlayerStatus;
    private InventoryInfo inventoryInfo;
    
    #region MonoBehaviour Callbacks
    
    private void Start()
    {
        try
        {
            var playerController = Services.Get<PlayerController>();
            
            if (playerController != null)
            {
                runtimePlayerStatus = playerController.rps;
                inventoryInfo = runtimePlayerStatus.inventoryInfo;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"EquipmentDataProvider: Could not get PlayerController: {e.Message}");
        }
    }
    
    #endregion
    
    #region IEquipmentDataProvider Implementation
    
    /// <summary>
    /// Gets all accessories in the inventory.
    /// </summary>
    public List<ItemUIInfo<InventoryItem>> GetAccessories()
    {
        if (inventoryInfo == null)
        {
            Debug.LogWarning("EquipmentDataProvider: InventoryInfo not available!");
            return new List<ItemUIInfo<InventoryItem>>();
        }
        
        var accessoryStacks = inventoryInfo.GetStacksOfType<Accessory>();
        // Filter out null ItemUIInfo objects
        return accessoryStacks.Select(stack => stack.GetItemUIInfo()).Where(info => info != null).ToList();
    }
    
    /// <summary>
    /// Gets all passive skills in the inventory.
    /// </summary>
    public List<ItemUIInfo<InventoryItem>> GetPassives()
    {
        // TODO: Implement passive skill system
        Debug.LogWarning("EquipmentDataProvider: Passive system not yet implemented!");
        return new List<ItemUIInfo<InventoryItem>>();
    }
    
    /// <summary>
    /// Gets the currently equipped accessory at the specified slot.
    /// </summary>
    public Extensions.UI.EquippedItemDisplayData GetEquippedAccessory(int slotIndex)
    {
        if (runtimePlayerStatus == null || runtimePlayerStatus.CurrentLoadout == null)
            return Extensions.UI.EquippedItemDisplayData.Empty();
        
        if (runtimePlayerStatus.CurrentLoadout.equippedAccessories == null ||
            slotIndex < 0 || slotIndex >= runtimePlayerStatus.CurrentLoadout.equippedAccessories.Length)
            return Extensions.UI.EquippedItemDisplayData.Empty();
        
        Accessory accessory = runtimePlayerStatus.CurrentLoadout.equippedAccessories[slotIndex];
        
        if (accessory == null)
            return Extensions.UI.EquippedItemDisplayData.Empty();
        
        return new Extensions.UI.EquippedItemDisplayData
        {
            icon = accessory.itemIcon,
            itemName = accessory.itemName,
            itemDescription = accessory.itemDescription,
            rarity = accessory.itemRarity,
            isEmpty = false
        };
    }
    
    /// <summary>
    /// Gets the currently equipped passive at the specified slot.
    /// </summary>
    public Extensions.UI.EquippedItemDisplayData GetEquippedPassive(int slotIndex)
    {
        // TODO: Implement passive skill system
        return Extensions.UI.EquippedItemDisplayData.Empty();
    }
    
    /// <summary>
    /// Equips an accessory to the specified slot.
    /// </summary>
    /// <param name="slotIndex">The slot index (0-2)</param>
    /// <param name="item">The item UI info</param>
    /// <returns>True if successfully equipped</returns>
    public bool EquipAccessory(int slotIndex, ItemUIInfo<InventoryItem> item)
    {
        if (runtimePlayerStatus == null || runtimePlayerStatus.CurrentLoadout == null || inventoryInfo == null)
        {
            Debug.LogWarning("EquipmentDataProvider: Cannot equip accessory, data not available!");
            return false;
        }
        
        if (slotIndex < 0 || slotIndex >= runtimePlayerStatus.CurrentLoadout.equippedAccessories.Length)
        {
            Debug.LogWarning($"EquipmentDataProvider: Invalid slot index {slotIndex}!");
            return false;
        }
        
        // CRITICAL: Use itemReference instead of itemName for bijective identification
        if (item.itemReference == null)
        {
            Debug.LogWarning($"EquipmentDataProvider: ItemUIInfo has null itemReference!");
            return false;
        }
        
        Accessory accessory = item.itemReference as Accessory;
        if (accessory == null)
        {
            Debug.LogWarning($"EquipmentDataProvider: Item is not an Accessory!");
            return false;
        }
        
        runtimePlayerStatus.CurrentLoadout.equippedAccessories[slotIndex] = accessory;
        
        Debug.Log($"EquipmentDataProvider: Equipped {accessory.itemName} to accessory slot {slotIndex}");
        
        // Raise event to refresh player stats
        EventBus<PlayerStatsChangedEvent>.Raise(new PlayerStatsChangedEvent("Equipment - Equip Accessory"));
        
        return true;
    }
    
    /// <summary>
    /// Equips a passive skill to the specified slot.
    /// </summary>
    /// <param name="slotIndex">The slot index (0-2)</param>
    /// <param name="item">The item UI info</param>
    /// <returns>True if successfully equipped</returns>
    public bool EquipPassive(int slotIndex, ItemUIInfo<InventoryItem> item)
    {
        // TODO: Implement passive skill system
        Debug.LogWarning("EquipmentDataProvider: Passive system not yet implemented!");
        return false;
    }
    
    /// <summary>
    /// Unequips an accessory from the specified slot.
    /// </summary>
    /// <param name="slotIndex">The slot index (0-2)</param>
    /// <returns>True if successfully unequipped</returns>
    public bool UnequipAccessory(int slotIndex)
    {
        if (runtimePlayerStatus == null)
        {
            Debug.LogWarning("EquipmentDataProvider: RuntimePlayerStatus not available!");
            return false;
        }
        
        if (slotIndex < 0 || slotIndex >= 3)
        {
            Debug.LogWarning($"EquipmentDataProvider: Invalid accessory slot index {slotIndex}");
            return false;
        }
        
        runtimePlayerStatus.CurrentLoadout.equippedAccessories[slotIndex] = null;
        Debug.Log($"EquipmentDataProvider: Unequipped accessory from slot {slotIndex}");
        
        // Raise event to refresh player stats
        EventBus<PlayerStatsChangedEvent>.Raise(new PlayerStatsChangedEvent("Equipment - Unequip Accessory"));
        
        return true;
    }
    
    /// <summary>
    /// Unequips a passive from the specified slot.
    /// </summary>
    /// <param name="slotIndex">The slot index (0-2)</param>
    /// <returns>True if successfully unequipped</returns>
    public bool UnequipPassive(int slotIndex)
    {
        // TODO: Implement when passive system exists
        Debug.LogWarning("EquipmentDataProvider: Passive system not yet implemented!");
        return false;
    }
    
    #endregion
}
