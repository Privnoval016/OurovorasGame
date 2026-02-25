using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * <summary>
 * Generic inventory category for items of type T. Used to ensure no mixing of item types within a category.
 * </summary>
 *
 * <typeparam name="T">Type of InventoryItem this category holds.</typeparam>
 */
[Serializable]
public class InventoryCategory<T> : IInventoryCategory where T : InventoryItem
{
    [SerializeField] public List<InventoryStack> stacks = new();

    public Type ItemType => typeof(T);

    public IList GetStacksUntyped() => stacks;

    public void AddItem(InventoryItem item, int amount)
    {
        if (item is not T typedItem) return;

        var existing = stacks.Find(s => s.CanStackWith(typedItem));
        if (existing != null)
        {
            existing.amount += amount;
        }
        else
        {
            stacks.Add(new InventoryStack { item = typedItem, amount = amount });
        }
    }

    public InventoryItem RemoveItem(InventoryItem item, int amount)
    {
        if (item is not T typedItem) return null;

        var stack = stacks.Find(s => s.item == typedItem);
        if (stack == null) return null;

        stack.amount -= amount;
        if (stack.amount <= 0)
            stacks.Remove(stack);

        return typedItem;
    }
    
    public InventoryStack GetStackOfType(InventoryItem item)
    {
        if (item is not T tItem) return null;
        return stacks.Find(s => s.item == tItem);
    }
    
    public List<InventoryStack> GetAllStacks()
    {
        return stacks;
    }
    
    public int GetIndexOfStack(InventoryItem item)
    {
        if (item is not T tItem) return -1;
        return stacks.FindIndex(s => s.item == tItem);
    }
}

[Serializable]
public class AccessoryCategory : InventoryCategory<Accessory> { }

[Serializable]
public class ConsumableCategory : InventoryCategory<Consumable> { }

[Serializable]
public class ResourceCategory : InventoryCategory<Resource> { }

[Serializable]
public class KeyItemCategory : InventoryCategory<KeyItem> { }

/**
 * Interface used for serialization of generic inventory categories (because Unity doesn't support serialization
 * of generics).
 */
public interface IInventoryCategory
{
    Type ItemType { get; }
    IList GetStacksUntyped();
    void AddItem(InventoryItem item, int amount);
    InventoryItem RemoveItem(InventoryItem item, int amount);

    InventoryStack GetStackOfType(InventoryItem item);

    List<InventoryStack> GetAllStacks();

    int GetIndexOfStack(InventoryItem item);
}