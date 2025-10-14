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
    
    public InventoryStack<T> GetStackOfType<T>(T item) where T : InventoryItem
    {
        List<InventoryStack<T>> stackList = item switch
        {
            Accessory => accessories as List<InventoryStack<T>>,
            Consumable => consumables as List<InventoryStack<T>>,
            Resource => resources as List<InventoryStack<T>>,
            KeyItem => keyItems as List<InventoryStack<T>>,
            _ => accessories as List<InventoryStack<T>>
        };

        return stackList?.Find(stack => stack.item == item);
    }
    
    public int GetIndexOfStack<T>(T item) where T : InventoryItem
    {
        List<InventoryStack<T>> stackList = item switch
        {
            Accessory => accessories as List<InventoryStack<T>>,
            Consumable => consumables as List<InventoryStack<T>>,
            Resource => resources as List<InventoryStack<T>>,
            KeyItem => keyItems as List<InventoryStack<T>>,
            _ => accessories as List<InventoryStack<T>>
        };

        return stackList?.FindIndex(stack => stack.item == item) ?? -1;
    }
}

[Serializable]
public class InventoryStack<T> where T : InventoryItem
{
    public T item;
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
}
