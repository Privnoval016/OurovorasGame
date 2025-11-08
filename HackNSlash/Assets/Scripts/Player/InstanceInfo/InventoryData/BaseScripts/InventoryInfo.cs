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
    
    public void AddItem<T>(T item, int amount = 1) where T : InventoryItem
    {
        List<InventoryStack<T>> stackList = item switch
        {
            Accessory => accessories as List<InventoryStack<T>>,
            Consumable => consumables as List<InventoryStack<T>>,
            Resource => resources as List<InventoryStack<T>>,
            KeyItem => keyItems as List<InventoryStack<T>>,
            _ => accessories as List<InventoryStack<T>>
        };

        InventoryStack<T> existingStack = stackList?.Find(stack => stack.item == item);
        if (existingStack != null)
        {
            existingStack.amount += amount;
        }
        else
        {
            stackList?.Add(new InventoryStack<T> { item = item, amount = amount });
        }
    }
    
    public T RemoveItem<T>(T item, int amount = 1) where T : InventoryItem
    {
        List<InventoryStack<T>> stackList = item switch
        {
            Accessory => accessories as List<InventoryStack<T>>,
            Consumable => consumables as List<InventoryStack<T>>,
            Resource => resources as List<InventoryStack<T>>,
            KeyItem => keyItems as List<InventoryStack<T>>,
            _ => accessories as List<InventoryStack<T>>
        };

        InventoryStack<T> existingStack = stackList?.Find(stack => stack.item == item);
        if (existingStack != null && existingStack.amount >= amount)
        {
            existingStack.amount -= amount;
            if (existingStack.amount <= 0)
            {
                stackList?.Remove(existingStack);
            }
            return item;
        }
        return null;
    }
}

/**
 * Serializable class representing a stack of items of a specific subtype to ensure uniformity.
 */
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

/**
 * Serializable struct representing a stack of items with a required amount. Does not need to be of the same subtype.
 */
[Serializable]
public struct ItemStack
{
    public InventoryItem item;
    public int requiredAmount;
    
    public ItemStack(InventoryItem item, int requiredAmount)
    {
        this.item = item;
        this.requiredAmount = requiredAmount;
    }
}
