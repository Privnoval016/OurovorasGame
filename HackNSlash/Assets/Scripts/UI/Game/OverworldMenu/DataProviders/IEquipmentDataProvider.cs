using System.Collections.Generic;
using Extensions.UI;

/// <summary>
/// Interface for providing equipment data to the UI.
/// Separates UI from game logic following MVVM pattern.
/// </summary>
public interface IEquipmentDataProvider
{
    /// <summary>
    /// Gets all available accessories from inventory.
    /// </summary>
    List<ItemUIInfo<InventoryItem>> GetAccessories();
    
    /// <summary>
    /// Gets all available passive skills from inventory.
    /// </summary>
    List<ItemUIInfo<InventoryItem>> GetPassives();
    
    /// <summary>
    /// Gets the currently equipped accessory at the specified slot.
    /// </summary>
    Extensions.UI.EquippedItemDisplayData GetEquippedAccessory(int slotIndex);
    
    /// <summary>
    /// Gets the currently equipped passive at the specified slot.
    /// </summary>
    Extensions.UI.EquippedItemDisplayData GetEquippedPassive(int slotIndex);
    
    /// <summary>
    /// Equips an accessory to the specified slot.
    /// </summary>
    /// <param name="slotIndex">The slot to equip to (0-2)</param>
    /// <param name="item">The item UI info of the accessory to equip</param>
    /// <returns>True if equipped successfully</returns>
    bool EquipAccessory(int slotIndex, ItemUIInfo<InventoryItem> item);
    
    /// <summary>
    /// Equips a passive skill to the specified slot.
    /// </summary>
    /// <param name="slotIndex">The slot to equip to (0-2)</param>
    /// <param name="item">The item UI info of the passive to equip</param>
    /// <returns>True if equipped successfully</returns>
    bool EquipPassive(int slotIndex, ItemUIInfo<InventoryItem> item);
    
    /// <summary>
    /// Unequips the accessory from the specified slot.
    /// </summary>
    /// <param name="slotIndex">The slot to unequip from (0-2)</param>
    /// <returns>True if unequipped successfully</returns>
    bool UnequipAccessory(int slotIndex);
    
    
    /// <summary>
    /// Unequips the passive skill from the specified slot.
    /// </summary>
    /// <param name="slotIndex">The slot to unequip from (0-2)</param>
    /// <returns>True if unequipped successfully</returns> 
    bool UnequipPassive(int slotIndex);
}


