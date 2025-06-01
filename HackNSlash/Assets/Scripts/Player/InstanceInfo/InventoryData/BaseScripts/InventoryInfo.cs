using System;
using System.Collections.Generic;using Extensions.UI;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryInfo", menuName = "Inventory/InventoryInfo", order = 0)]
public class InventoryInfo : ScriptableObject
{
    public List<InventoryStack<Accessory>> accessories = new List<InventoryStack<Accessory>>();
    public List<InventoryStack<Consumable>> consumables = new List<InventoryStack<Consumable>>();
    public List<InventoryStack<Resource>> resources = new List<InventoryStack<Resource>>();
    public List<InventoryStack<KeyItem>> keyItems = new List<InventoryStack<KeyItem>>();
}

[Serializable]
public class InventoryStack<T> : ScrollItem where T : InventoryItem
{
    public T item;
    public int amount;

    public void OnScrollActive()
    {
        
    }
    
    public void OnScrollInactive()
    {
        
    }
    
    public ItemUIInfo GetItemUIInfo()
    {
        return new ItemUIInfo
        {
            itemName = item.itemName,
            itemDescription = item.itemDescription,
            amount = amount
            //itemRarity = item.itemRarity
        };
    }
}
