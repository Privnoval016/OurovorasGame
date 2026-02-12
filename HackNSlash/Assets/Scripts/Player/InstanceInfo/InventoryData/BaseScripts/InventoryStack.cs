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

    public ItemUIInfo<InventoryItem> GetItemUIInfo()
    {
        if (item == null)
            return null;

        return new ItemUIInfo<InventoryItem>
        {
            itemReference = item, // CRITICAL: Store reference for bijective identification
            itemName = item.itemName,
            itemDescription = item.itemDescription,
            amount = amount,
            isStackable = item.isStackable,
            rarity = item.itemRarity,
            icon = item.itemIcon,
            category = GetItemCategory(item)
        };
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
