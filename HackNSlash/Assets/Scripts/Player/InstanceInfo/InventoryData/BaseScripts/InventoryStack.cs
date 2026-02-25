using System;
using System.Collections;
using Extensions.UI;

/**
 * Serializable class representing a stack of items of a specific subtype to ensure uniformity.
 */
[Serializable]
public class InventoryStack
{
    public InventoryItem item; // only one type per stack
    public int amount;

    public ItemUIInfo<InventoryStack> GetItemUIInfo()
    {
        if (item == null)
            return null;

        return new ItemUIInfo<InventoryStack>
        {
            guid = GenerateGuid(), // CRITICAL: Unique identifier for this stack
            itemReference = this, // Store stack reference for data access
            itemName = item.itemName,
            itemDescription = item.itemDescription,
            amount = amount,
            isStackable = item.isStackable,
            rarity = item.itemRarity,
            icon = item.itemIcon,
            category = GetItemCategory(item)
        };
    }
    
    /// <summary>
    /// Generates a unique GUID for this stack based on the item's instance ID.
    /// This ensures each unique item has a consistent identifier.
    /// </summary>
    private string GenerateGuid()
    {
        if (item == null) return Guid.NewGuid().ToString();
        
        // Use item's GetInstanceID() for Unity objects, or generate new GUID
        // This creates a stable identifier as long as the item reference exists
        return $"item_{item.GetInstanceID()}_{amount}";
    }
    
    private string GetItemCategory(InventoryItem inventoryItem)
    {
        string typeName = inventoryItem.GetType().Name;
        
        return typeName switch
        {
            "Accessory" => "Accessories",
            "Consumable" => "Consumables",
            "Resource" => "Resources",
            "KeyItem" => "Key Items",
            _ => "Other"
        };
    }

    public bool CanStackWith(InventoryItem other)
    {
        return item == other && item.isStackable;
    }
}
