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

    public ItemUIInfo GetItemUIInfo()
    {
        return new ItemUIInfo
        {
            itemName = item.itemName,
            itemDescription = item.itemDescription,
            amount = amount,
            isStackable = item.isStackable,
            itemRarity = item.itemRarity,
            icon = item.itemIcon
        };
    }

    public bool CanStackWith(InventoryItem other)
    {
        return item == other && item.isStackable;
    }
}
