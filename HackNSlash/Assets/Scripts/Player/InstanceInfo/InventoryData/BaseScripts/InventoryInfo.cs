using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryInfo", menuName = "Inventory/InventoryInfo", order = 0)]
public class InventoryInfo : ScriptableObject, ISerializationCallbackReceiver
{
    [Header("Categories")]
    [SerializeReference] public List<IInventoryCategory> categories = new();

    [NonSerialized] private Dictionary<Type, IInventoryCategory> _categoryLookup;

    #region Initialization
    
    // Ensure the category lookup dictionary is initialized
    private void EnsureCache()
    {
        if (_categoryLookup != null) return;

        _categoryLookup = new Dictionary<Type, IInventoryCategory>();
        foreach (var cat in categories)
        {
            if (cat != null)
                _categoryLookup[cat.ItemType] = cat;
        }
    }

    // Ensure default categories are present
    private void EnsureDefaultCategories()
    {
        if (!categories.Exists(c => c is AccessoryCategory)) AddCategory(new AccessoryCategory());
        if (!categories.Exists(c => c is ConsumableCategory)) AddCategory(new ConsumableCategory());
        if (!categories.Exists(c => c is ResourceCategory)) AddCategory(new ResourceCategory());
        if (!categories.Exists(c => c is KeyItemCategory)) AddCategory(new KeyItemCategory());
    }
    
    /**
     * <summary>
     * Initialize the inventory info by ensuring default categories and cache are set up.
     * </summary>
     */
    public void Initialize()
    {
        EnsureDefaultCategories();
        EnsureCache();
    }
    
    #endregion
    
    #region Accessors
    
    /**
     * <summary>
     * Get all item stacks of a specific type T.
     * </summary>
     *
     * <typeparam name="T">The type of InventoryItem to retrieve stacks for.</typeparam>
     */
    public List<InventoryStack> GetStacksOfType<T>() where T : InventoryItem
    {
        return GetStacksOfType(typeof(T));
    }
    
    private List<InventoryStack> GetStacksOfType(Type itemType)
    {
        EnsureCache();
        if (_categoryLookup.TryGetValue(itemType, out var category))
            return category.GetAllStacks();
        return new List<InventoryStack>();
    }
    
    /**
     * <summary>
     * Add an item to the appropriate category in the inventory.
     * </summary>
     *
     * <param name="item">The InventoryItem to add.</param>
     * <param name="amount">The amount of the item to add (default is 1).</param>
     */
    public void AddItem(InventoryItem item, int amount = 1)
    {
        if (item == null) return;

        EnsureCache();
        Type itemType = item.GetType();
        if (_categoryLookup.TryGetValue(itemType, out var category))
        {
            category.AddItem(item, amount);
        }
        else
        {
            Debug.LogWarning($"No category registered for item type {itemType.Name}");
        }
    }

    /**
     * <summary>
     * Remove an item from the appropriate category in the inventory.
     * </summary>
     *
     * <param name="item">The InventoryItem to remove.</param>
     * <param name="amount">The amount of the item to remove (default is 1).</param>
     * <returns>The removed InventoryItem, or null if not found.</returns>
     */
    public InventoryItem RemoveItem(InventoryItem item, int amount = 1)
    {
        if (item == null) return null;

        EnsureCache();
        Type itemType = item.GetType();
        if (_categoryLookup.TryGetValue(itemType, out var category))
        {
            return category.RemoveItem(item, amount) as InventoryItem;
        }

        Debug.LogWarning($"No category registered for item type {itemType.Name}");
        return null;
    }

    /**
     * <summary>
     * Get the inventory stack for a specific item.
     * </summary>
     *
     * <param name="item">The InventoryItem to find the stack for.</param>
     * <returns>The InventoryStack containing the item, or null if not found.</returns>
     */
    public InventoryStack GetStackOfType(InventoryItem item)
    {
        if (item == null) return null;

        EnsureCache();
        Type itemType = item.GetType();
        if (_categoryLookup.TryGetValue(itemType, out var category))
        {
            return category.GetStackOfType(item);
        }

        Debug.LogWarning($"No category registered for item type {itemType.Name}");
        return null;
    }
    
    /**
     * <summary>
     * Get the index of the stack containing the specified item.
     * </summary>
     *
     * <param name="item">The InventoryItem to find the index for.</param>
     * <returns>The index of the stack, or -1 if not found.</returns>
     */
    public int GetIndexOfStack(InventoryItem item)
    {
        if (item == null) return -1;

        EnsureCache();
        Type itemType = item.GetType();
        if (_categoryLookup.TryGetValue(itemType, out var category))
        {
            return category.GetIndexOfStack(item);
        }

        Debug.LogWarning($"No category registered for item type {itemType.Name}");
        return -1;
    }
    
    #endregion
    
    #region Category Management
    
    private void AddCategory(IInventoryCategory category)
    {
        if (category == null || categories.Contains(category)) return;

        categories.Add(category);
        EnsureCache();
        _categoryLookup[category.ItemType] = category;
    }
    
    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize() => _categoryLookup = null;
    
    #endregion
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
