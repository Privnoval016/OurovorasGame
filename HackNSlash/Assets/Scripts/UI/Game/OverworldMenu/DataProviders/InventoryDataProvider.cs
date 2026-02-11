using System.Collections.Generic;
using System.Linq;
using Extensions.Patterns;
using Extensions.UI;
using UnityEngine;

/// <summary>
/// Implementation of IInventoryDataProvider that bridges the UI with PlayerInventory.
/// This is the backend connector for Tab 5 (Inventory).
/// </summary>
public class InventoryDataProvider : MonoBehaviour, IInventoryDataProvider, IService
{
    private RuntimePlayerStatus runtimePlayerStatus;
    private InventoryInfo inventoryInfo;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        Services.Register<InventoryDataProvider>(this);
    }
    
    private void Start()
    {
        var playerController = Services.Get<PlayerController>();
        
        if (playerController != null)
        {
            runtimePlayerStatus = playerController.rps;
            inventoryInfo = runtimePlayerStatus.inventoryInfo;
        }
    }
    
    #endregion
    
    #region IInventoryDataProvider Implementation
    
    /// <summary>
    /// Gets all items in the inventory.
    /// </summary>
    public List<ItemUIInfo> GetAllItems()
    {
        if (inventoryInfo == null)
        {
            Debug.LogWarning("InventoryDataProvider: InventoryInfo not available!");
            return new List<ItemUIInfo>();
        }
        
        var allItems = new List<ItemUIInfo>();
        
        // Get all categories and combine their items
        foreach (var category in inventoryInfo.categories)
        {
            if (category != null)
            {
                var stacks = category.GetAllStacks();
                allItems.AddRange(stacks.Select(stack => stack.GetItemUIInfo()));
            }
        }
        
        return allItems;
    }
    
    /// <summary>
    /// Gets items filtered by category.
    /// </summary>
    public List<ItemUIInfo> GetItemsByCategory(string category)
    {
        if (inventoryInfo == null)
        {
            Debug.LogWarning("InventoryDataProvider: InventoryInfo not available!");
            return new List<ItemUIInfo>();
        }
        
        // Map category name to type
        List<InventoryStack> stacks = category switch
        {
            "Accessories" => inventoryInfo.GetStacksOfType<Accessory>(),
            "Consumables" => inventoryInfo.GetStacksOfType<Consumable>(),
            "Resources" => inventoryInfo.GetStacksOfType<Resource>(),
            "Key Items" => inventoryInfo.GetStacksOfType<KeyItem>(),
            _ => new List<InventoryStack>()
        };
        
        return stacks.Select(stack => stack.GetItemUIInfo()).ToList();
    }
    
    /// <summary>
    /// Gets all available item categories.
    /// </summary>
    public string[] GetCategories()
    {
        return new string[]
        {
            "Accessories",
            "Consumables",
            "Resources",
            "Key Items"
        };
    }
    
    #endregion
}

