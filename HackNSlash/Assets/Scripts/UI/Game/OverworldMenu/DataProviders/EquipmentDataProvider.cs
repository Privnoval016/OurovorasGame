using System.Collections.Generic;
using System.Linq;
using Extensions.UI;
using UnityEngine;

/// <summary>
/// Implementation of IEquipmentDataProvider that bridges the UI with PlayerInventory.
/// This is the backend connector for Tab 2 (Equipment Selection).
/// Locally managed by OverworldMenuUI - not a global service.
/// </summary>
public class EquipmentDataProvider : MonoBehaviour, IEquipmentDataProvider
{
    private PlayerInventory playerInventory;
    private InventoryInfo inventoryInfo;
    
    #region MonoBehaviour Callbacks
    
    private void Start()
    {
        try
        {
            var playerController = Services.Get<PlayerController>();
            
            if (playerController != null)
            {
                playerInventory = playerController.pi;
                inventoryInfo = playerInventory.inventoryInfo;
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
    public List<ItemUIInfo> GetAccessories()
    {
        if (inventoryInfo == null)
        {
            Debug.LogWarning("EquipmentDataProvider: InventoryInfo not available!");
            return new List<ItemUIInfo>();
        }
        
        var accessoryStacks = inventoryInfo.GetStacksOfType<Accessory>();
        return accessoryStacks.Select(stack => stack.GetItemUIInfo()).ToList();
    }
    
    /// <summary>
    /// Gets all passive skills in the inventory.
    /// </summary>
    public List<ItemUIInfo> GetPassives()
    {
        // TODO: Implement passive skill system
        Debug.LogWarning("EquipmentDataProvider: Passive system not yet implemented!");
        return new List<ItemUIInfo>();
    }
    
    /// <summary>
    /// Gets the currently equipped accessory at the specified slot.
    /// </summary>
    public EquippedItemDisplayData GetEquippedAccessory(int slotIndex)
    {
        if (playerInventory == null || playerInventory.CurrentLoadout == null)
            return EquippedItemDisplayData.Empty();
        
        if (playerInventory.CurrentLoadout.equippedAccessories == null ||
            slotIndex < 0 || slotIndex >= playerInventory.CurrentLoadout.equippedAccessories.Length)
            return EquippedItemDisplayData.Empty();
        
        Accessory accessory = playerInventory.CurrentLoadout.equippedAccessories[slotIndex];
        
        if (accessory == null)
            return EquippedItemDisplayData.Empty();
        
        return new EquippedItemDisplayData
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
    public EquippedItemDisplayData GetEquippedPassive(int slotIndex)
    {
        // TODO: Implement passive skill system
        return EquippedItemDisplayData.Empty();
    }
    
    /// <summary>
    /// Equips an accessory to the specified slot.
    /// </summary>
    public void EquipAccessory(int slotIndex, int itemIndex)
    {
        if (playerInventory == null || playerInventory.CurrentLoadout == null || inventoryInfo == null)
        {
            Debug.LogWarning("EquipmentDataProvider: Cannot equip accessory, data not available!");
            return;
        }
        
        var accessoryStacks = inventoryInfo.GetStacksOfType<Accessory>();
        
        if (itemIndex < 0 || itemIndex >= accessoryStacks.Count)
        {
            Debug.LogWarning($"EquipmentDataProvider: Invalid item index {itemIndex}!");
            return;
        }
        
        if (slotIndex < 0 || slotIndex >= playerInventory.CurrentLoadout.equippedAccessories.Length)
        {
            Debug.LogWarning($"EquipmentDataProvider: Invalid slot index {slotIndex}!");
            return;
        }
        
        Accessory accessory = accessoryStacks[itemIndex].item as Accessory;
        playerInventory.CurrentLoadout.equippedAccessories[slotIndex] = accessory;
        
        Debug.Log($"Equipped {accessory.itemName} to accessory slot {slotIndex}");
    }
    
    /// <summary>
    /// Equips a passive skill to the specified slot.
    /// </summary>
    public void EquipPassive(int slotIndex, int itemIndex)
    {
        // TODO: Implement passive skill system
        Debug.LogWarning("EquipmentDataProvider: Passive system not yet implemented!");
    }
    
    #endregion
}

