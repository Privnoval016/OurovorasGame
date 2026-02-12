using System.Collections.Generic;
using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tab 5: Inventory tab for viewing and managing all collected items.
/// Shows category filters on left, scrolling item list in center, character model on right.
/// </summary>
public class InventoryTab : TabSelection, IScrollMenuAuthority
{
    [Header("Data Provider")]
    [SerializeField] private MonoBehaviour inventoryDataProviderObject;
    private IInventoryDataProvider inventoryDataProvider;
    
    [Header("UI Components - Category Selection")]
    [SerializeField] private CategoryButton[] categoryButtons;
    
    [Header("UI Components - Item Display")]
    [SerializeField] private ScrollMenu itemScrollMenu;
    [SerializeField] private GameObject scrollMenuContainer;
    [SerializeField] private TextMeshProUGUI selectedItemNameText;
    [SerializeField] private TextMeshProUGUI selectedItemDescriptionText;
    [SerializeField] private Image selectedItemIcon;
    [SerializeField] private TextMeshProUGUI selectedItemQuantityText;
    
    [Header("UI Components - Action Menu")]
    [SerializeField] private ItemActionMenu itemActionMenu;
    [SerializeField] private GameObject actionMenuContainer;
    
    [Header("UI Components - Sort")]
    [SerializeField] private TextMeshProUGUI sortMethodText; // Shows current sort method
    [SerializeField] private GameObject sortIndicator; // Container for sort display
    
    [Header("UI Components - Character Model")]
    [SerializeField] private RenderTextureDisplay characterModelDisplay;
    
    [Header("Data Providers for Equipment Check")]
    [SerializeField] private MonoBehaviour equipmentDataProviderObject;
    private IEquipmentDataProvider equipmentDataProvider;
    
    [Header("Default Button")]
    [SerializeField] private Selectable defaultButton;
    
    private string currentCategory = "All";
    private bool isScrollMenuActive;
    private bool isScrollMenuFocused; // CRITICAL: Tracks if user has entered scroll menu (not just visible)
    private bool isActionMenuActive;
    private ItemUIInfo<InventoryStack> currentSelectedItem;
    private InventorySortMethod currentSortMethod = InventorySortMethod.NameAscending;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        if (inventoryDataProviderObject != null)
            inventoryDataProvider = inventoryDataProviderObject as IInventoryDataProvider;
        
        if (equipmentDataProviderObject != null)
            equipmentDataProvider = equipmentDataProviderObject as IEquipmentDataProvider;
        
        // Subscribe to action menu events
        if (itemActionMenu != null)
        {
            itemActionMenu.OnUseRequested += HandleItemUse;
            itemActionMenu.OnDiscardRequested += HandleItemDiscard;
            itemActionMenu.OnMenuClosed += HandleActionMenuClosed;
        }
        
        // Hide action menu initially
        if (actionMenuContainer != null)
            actionMenuContainer.SetActive(false);
        
        // Subscribe to input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.onSelect += OnSelectInput;
            InputManager.Instance.onBack += OnBackInput;
            InputManager.Instance.onSort += OnSortInput;
        }
        
        // Update sort display
        UpdateSortDisplay();
    }
    
    private void OnDestroy()
    {
        if (itemActionMenu != null)
        {
            itemActionMenu.OnUseRequested -= HandleItemUse;
            itemActionMenu.OnDiscardRequested -= HandleItemDiscard;
            itemActionMenu.OnMenuClosed -= HandleActionMenuClosed;
        }
        
        if (InputManager.Instance != null)
        {
            InputManager.Instance.onSelect -= OnSelectInput;
            InputManager.Instance.onBack -= OnBackInput;
            InputManager.Instance.onSort -= OnSortInput;
        }
    }
    
    #endregion
    
    #region TabSelection Overrides
    
    /// <summary>
    /// Called when this tab is selected.
    /// </summary>
    public override void OnTabSelect()
    {
        base.OnTabSelect();
        
        inventoryDataProvider ??= inventoryDataProviderObject as IInventoryDataProvider;
        
        // Activate character model
        if (characterModelDisplay != null)
            characterModelDisplay.Activate();
        
        // Initialize category buttons
        InitializeCategoryButtons();
        
        // Show all items by default
        SelectCategory("All");
        
        // Set default button
        if (defaultButton != null)
            defaultButton.Select();
    }
    
    /// <summary>
    /// Called when this tab is deselected.
    /// </summary>
    public override void OnTabDeselect()
    {
        base.OnTabDeselect();
        
        if (isScrollMenuActive)
            DeactivateScrollMenu();
        
        if (characterModelDisplay != null)
            characterModelDisplay.Deactivate();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeCategoryButtons()
    {
        if (inventoryDataProvider == null)
            return;
        
        string[] categories = inventoryDataProvider.GetCategories();
        
        // Add "All" category
        string[] allCategories = new string[categories.Length + 1];
        allCategories[0] = "All";
        System.Array.Copy(categories, 0, allCategories, 1, categories.Length);
        
        for (int i = 0; i < categoryButtons.Length && i < allCategories.Length; i++)
        {
            if (categoryButtons[i] != null)
            {
                string category = allCategories[i];
                
                // Initialize button with category name
                categoryButtons[i].Initialize(category);
                
                // Subscribe to selection event
                categoryButtons[i].AddListener(() => SelectCategory(category));
            }
        }
    }
    
    #endregion
    
    #region Category Selection
    
    /// <summary>
    /// Selects a category and updates the item display.
    /// If the same category is selected again, focus the scroll menu.
    /// </summary>
    /// <param name="category">The category to display.</param>
    public void SelectCategory(string category)
    {
        // If selecting the same category again, enter scroll menu focus
        if (currentCategory == category && isScrollMenuActive && !isScrollMenuFocused)
        {
            Debug.Log($"InventoryTab: Category '{category}' already selected, entering scroll menu focus");
            isScrollMenuFocused = true;
            UIAudio.PlayHover();
            return;
        }
        
        currentCategory = category;
        UpdateCategoryButtonVisuals();
        UpdateItemDisplay();
    }
    
    private void UpdateCategoryButtonVisuals()
    {
        if (inventoryDataProvider == null)
            return;
        
        string[] categories = inventoryDataProvider.GetCategories();
        string[] allCategories = new string[categories.Length + 1];
        allCategories[0] = "All";
        System.Array.Copy(categories, 0, allCategories, 1, categories.Length);
        
        for (int i = 0; i < categoryButtons.Length && i < allCategories.Length; i++)
        {
            if (categoryButtons[i] != null)
            {
                bool isCurrent = allCategories[i] == currentCategory;
                categoryButtons[i].SetAsCurrentCategory(isCurrent);
            }
        }
    }
    
    #endregion
    
    #region Item Display
    
    private void UpdateItemDisplay()
    {
        DeactivateScrollMenu();
        ActivateScrollMenu();
    }
    
    private void ActivateScrollMenu()
    {
        if (itemScrollMenu == null || inventoryDataProvider == null)
            return;
        
        List<ItemUIInfo<InventoryStack>> items;
        
        if (currentCategory == "All")
            items = inventoryDataProvider.GetAllItems();
        else
            items = inventoryDataProvider.GetItemsByCategory(currentCategory);
        
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning($"InventoryTab: No items in category '{currentCategory}'");
            
            // Hide scroll menu container when no items
            if (scrollMenuContainer != null)
                scrollMenuContainer.SetActive(false);
            
            // Clear item info display
            ClearItemInfo();
            
            isScrollMenuActive = false;
            return;
        }
        
        // Sort items based on current sort method
        items = SortItems(items);
        
        if (scrollMenuContainer != null)
            scrollMenuContainer.SetActive(true);
        
        itemScrollMenu.Activate(items, 0, this);
        
        // Set equipped check callback so scroll panels show equipped indicator
        itemScrollMenu.SetEquippedCheckCallback(obj => IsItemEquipped(obj as ItemUIInfo<InventoryStack>));
        
        isScrollMenuActive = true;
        isScrollMenuFocused = false; // CRITICAL: Not focused until user presses A to enter
        
        // Display first item info
        UpdateSelectedItemInfo(items[0]);
    }
    
    private void DeactivateScrollMenu()
    {
        if (itemScrollMenu != null)
            itemScrollMenu.Deactivate();
        
        isScrollMenuActive = false;
        isScrollMenuFocused = false;
    }
    
    private void UpdateSelectedItemInfo(ItemUIInfo<InventoryStack> itemInfo)
    {
        if (itemInfo == null)
        {
            ClearItemInfo();
            return;
        }
        
        currentSelectedItem = itemInfo;
        
        if (selectedItemNameText != null)
            selectedItemNameText.text = itemInfo.itemName;
        
        if (selectedItemDescriptionText != null)
            selectedItemDescriptionText.text = itemInfo.itemDescription;
        
        if (selectedItemIcon != null)
        {
            selectedItemIcon.sprite = itemInfo.icon;
            selectedItemIcon.enabled = itemInfo.icon != null;
        }
        
        if (selectedItemQuantityText != null)
        {
            selectedItemQuantityText.text = itemInfo.isStackable
                ? $"x{itemInfo.amount}"
                : "";
        }
    }
    
    private void ClearItemInfo()
    {
        if (selectedItemNameText != null)
            selectedItemNameText.text = "";
        
        if (selectedItemDescriptionText != null)
            selectedItemDescriptionText.text = "No items available";
        
        if (selectedItemIcon != null)
            selectedItemIcon.enabled = false;
        
        if (selectedItemQuantityText != null)
            selectedItemQuantityText.text = "";
    }
    
    #endregion
    
    #region Input Handling
    
    private void OnSelectInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        
        // If action menu is active, it will handle the input itself
        if (isActionMenuActive)
            return;
        
        // If scroll menu is active and focused, open action menu
        if (isScrollMenuActive && isScrollMenuFocused && itemScrollMenu != null)
        {
            currentSelectedItem = itemScrollMenu.GetSelectedItem<InventoryStack>();
            if (currentSelectedItem != null)
            {
                OpenActionMenu();
            }
        }
        // If scroll menu is active but NOT focused, entering it now
        else if (isScrollMenuActive && !isScrollMenuFocused)
        {
            isScrollMenuFocused = true;
            UIAudio.PlayHover();
        }
    }
    
    private void OnBackInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        
        // Priority 1: Close action menu if open
        if (isActionMenuActive)
        {
            CloseActionMenu();
            return;
        }
        
        // Priority 2: Exit scroll menu if focused
        if (isScrollMenuFocused && isScrollMenuActive)
        {
            isScrollMenuFocused = false;
            UIAudio.PlayBack();
            
            // Optionally refocus on default button (category button)
            if (defaultButton != null)
            {
                defaultButton.Select();
            }
        }
    }
    
    private void OnSortInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        
        // Don't allow sorting while action menu is open
        if (isActionMenuActive)
            return;
        
        // Cycle to next sort method
        CycleSortMethod();
        
        // Refresh display with new sort
        UpdateItemDisplay();
        
        UIAudio.PlayHover();
    }
    
    #endregion
    
    #region Sorting
    
    /// <summary>
    /// Cycles to the next sort method.
    /// </summary>
    private void CycleSortMethod()
    {
        int currentIndex = (int)currentSortMethod;
        int nextIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(InventorySortMethod)).Length;
        currentSortMethod = (InventorySortMethod)nextIndex;
        
        UpdateSortDisplay();
    }
    
    /// <summary>
    /// Updates the sort method display text.
    /// </summary>
    private void UpdateSortDisplay()
    {
        if (sortMethodText == null)
            return;
        
        string displayText = currentSortMethod switch
        {
            InventorySortMethod.NameAscending => "Sort: Name (A-Z)",
            InventorySortMethod.NameDescending => "Sort: Name (Z-A)",
            InventorySortMethod.RarityDescending => "Sort: Rarity (High-Low)",
            InventorySortMethod.RarityAscending => "Sort: Rarity (Low-High)",
            InventorySortMethod.QuantityDescending => "Sort: Quantity (High-Low)",
            InventorySortMethod.QuantityAscending => "Sort: Quantity (Low-High)",
            InventorySortMethod.DateObtainedNewest => "Sort: Date (Newest)",
            InventorySortMethod.DateObtainedOldest => "Sort: Date (Oldest)",
            InventorySortMethod.Category => "Sort: Category",
            _ => "Sort: Unknown"
        };
        
        sortMethodText.text = displayText;
    }
    
    /// <summary>
    /// Sorts a list of items based on current sort method.
    /// </summary>
    private List<ItemUIInfo<InventoryStack>> SortItems(List<ItemUIInfo<InventoryStack>> items)
    {
        if (items == null || items.Count == 0)
            return items;
        
        var sortedItems = new List<ItemUIInfo<InventoryStack>>(items);
        
        if (sortedItems.Count <= 1)
            return sortedItems; // No need to sort
        
        switch (currentSortMethod)
        {
            case InventorySortMethod.NameAscending:
                sortedItems.Sort((a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    return string.Compare(a.itemName ?? "", b.itemName ?? "", System.StringComparison.Ordinal);
                });
                break;
            
            case InventorySortMethod.NameDescending:
                sortedItems.Sort((a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    return string.Compare(b.itemName ?? "", a.itemName ?? "", System.StringComparison.Ordinal);
                });
                break;
            
            case InventorySortMethod.RarityDescending:
                sortedItems.Sort((a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    return b.rarity.CompareTo(a.rarity);
                });
                break;
            
            case InventorySortMethod.RarityAscending:
                sortedItems.Sort((a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    return a.rarity.CompareTo(b.rarity);
                });
                break;
            
            case InventorySortMethod.QuantityDescending:
                sortedItems.Sort((a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    return b.amount.CompareTo(a.amount);
                });
                break;
            
            case InventorySortMethod.QuantityAscending:
                sortedItems.Sort((a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    return a.amount.CompareTo(b.amount);
                });
                break;
            
            case InventorySortMethod.DateObtainedNewest:
                // TODO: Implement when date tracking is added to items
                // For now, maintain current order
                break;
            
            case InventorySortMethod.DateObtainedOldest:
                // TODO: Implement when date tracking is added to items
                // For now, maintain current order
                break;
            
            case InventorySortMethod.Category:
                sortedItems.Sort((a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    return string.Compare(a.category ?? "", b.category ?? "", System.StringComparison.Ordinal);
                });
                break;
        }
        
        return sortedItems;
    }
    
    #endregion
    
    #region Action Menu
    
    private void OpenActionMenu()
    {
        if (itemActionMenu == null || currentSelectedItem == null)
            return;
        
        // Check if item can be used
        bool canUse = inventoryDataProvider?.CanItemBeUsed(currentSelectedItem.itemName) ?? false;
        
        // Show action menu
        if (actionMenuContainer != null)
            actionMenuContainer.SetActive(true);
        
        itemActionMenu.Show(canUse);
        isActionMenuActive = true;
        
        UIAudio.PlayHover();
    }
    
    private void CloseActionMenu()
    {
        if (itemActionMenu != null)
            itemActionMenu.Hide();
        
        if (actionMenuContainer != null)
            actionMenuContainer.SetActive(false);
        
        isActionMenuActive = false;
        
        UIAudio.PlayBack();
    }
    
    private void HandleActionMenuClosed()
    {
        isActionMenuActive = false;
        
        if (actionMenuContainer != null)
            actionMenuContainer.SetActive(false);
    }
    
    private void HandleItemUse()
    {
        if (inventoryDataProvider == null || currentSelectedItem == null)
            return;
        
        bool success = inventoryDataProvider.UseItem(currentSelectedItem.itemName);
        
        if (success)
        {
            UIAudio.PlayItemEquip();
            
            // Refresh display
            UpdateItemDisplay();
        }
        else
        {
            UIAudio.PlayError();
        }
        
        CloseActionMenu();
    }
    
    private void HandleItemDiscard()
    {
        if (inventoryDataProvider == null || currentSelectedItem == null)
            return;
        
        // CRITICAL: Check if item is equipped before discarding
        bool isEquipped = IsItemEquipped(currentSelectedItem);
        
        if (isEquipped)
        {
            // Unequip item first using GUID
            UnequipItem(currentSelectedItem.guid);
        }
        
        // Discard item
        bool success = inventoryDataProvider.DiscardItem(currentSelectedItem.itemName);
        
        if (success)
        {
            UIAudio.PlayBack();
            
            // Refresh display
            UpdateItemDisplay();
        }
        else
        {
            UIAudio.PlayError();
        }
        
        CloseActionMenu();
    }
    
    /// <summary>
    /// Checks if an item is currently equipped.
    /// Uses GUID comparison for reliable identification.
    /// </summary>
    private bool IsItemEquipped(ItemUIInfo<InventoryStack> itemUIInfo)
    {
        if (equipmentDataProvider == null || itemUIInfo == null || string.IsNullOrEmpty(itemUIInfo.guid))
            return false;
        
        // CRITICAL: Compare by GUID, not reference!
        // Check accessories
        var accessories = equipmentDataProvider.GetAccessories();
        foreach (var accessory in accessories)
        {
            if (accessory != null && accessory.guid == itemUIInfo.guid)
            {
                // This item is in inventory - check if it's equipped
                for (int i = 0; i < 3; i++)
                {
                    var equippedItem = equipmentDataProvider.GetEquippedAccessory(i);
                    if (equippedItem != null && !equippedItem.isEmpty)
                    {
                        // Match by name (EquippedItemDisplayData doesn't have GUID yet)
                        if (equippedItem.itemName == itemUIInfo.itemName)
                            return true;
                    }
                }
            }
        }
        
        // Check passives
        var passives = equipmentDataProvider.GetPassives();
        foreach (var passive in passives)
        {
            if (passive != null && passive.guid == itemUIInfo.guid)
            {
                for (int i = 0; i < 3; i++)
                {
                    var equippedItem = equipmentDataProvider.GetEquippedPassive(i);
                    if (equippedItem != null && !equippedItem.isEmpty)
                    {
                        if (equippedItem.itemName == itemUIInfo.itemName)
                            return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Unequips an item from all slots.
    /// Uses GUID to identify which item to unequip.
    /// </summary>
    private void UnequipItem(string itemGuid)
    {
        if (equipmentDataProvider == null || string.IsNullOrEmpty(itemGuid))
            return;
        
        // CRITICAL: Use GUID comparison
        // Check and unequip from accessories
        var accessories = equipmentDataProvider.GetAccessories();
        for (int i = 0; i < 3; i++)
        {
            var equippedItem = equipmentDataProvider.GetEquippedAccessory(i);
            if (equippedItem != null && !equippedItem.isEmpty)
            {
                // Find matching GUID in accessories list
                var matchingAccessory = accessories.Find(a => a != null && a.guid == itemGuid);
                if (matchingAccessory != null && matchingAccessory.itemName == equippedItem.itemName)
                {
                    equipmentDataProvider.UnequipAccessory(i);
                }
            }
        }
        
        // Check and unequip from passives
        var passives = equipmentDataProvider.GetPassives();
        for (int i = 0; i < 3; i++)
        {
            var equippedItem = equipmentDataProvider.GetEquippedPassive(i);
            if (equippedItem != null && !equippedItem.isEmpty)
            {
                var matchingPassive = passives.Find(p => p != null && p.guid == itemGuid);
                if (matchingPassive != null && matchingPassive.itemName == equippedItem.itemName)
                {
                    equipmentDataProvider.UnequipPassive(i);
                }
            }
        }
    }
    
    #endregion
    
    #region IScrollMenuAuthority Implementation
    
    private void ScrollDelegate(InputAction.CallbackContext context)
    {
        // CRITICAL: Only forward scroll input when scroll menu is FOCUSED (user has entered it)
        // Not just when it's visible (active)
        if (!isScrollMenuActive || !isScrollMenuFocused || itemScrollMenu == null || isActionMenuActive)
            return;
        
        Vector2 scrollInput = context.ReadValue<Vector2>();
        itemScrollMenu.OnScrollPerformed(scrollInput);
    }
    
    public void SubscribeToScroll(ScrollMenu scrollMenu)
    {
        if (InputManager.Instance != null)
            InputManager.Instance.onScroll += ScrollDelegate;
    }
    
    public void UnsubscribeFromScroll(ScrollMenu scrollMenu)
    {
        if (InputManager.Instance != null)
            InputManager.Instance.onScroll -= ScrollDelegate;
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Sets the inventory data provider for this tab.
    /// </summary>
    /// <param name="provider">The inventory data provider.</param>
    public void SetDataProvider(IInventoryDataProvider provider)
    {
        inventoryDataProvider = provider;
        
        if (gameObject.activeInHierarchy)
        {
            InitializeCategoryButtons();
            UpdateItemDisplay();
        }
    }
    
    #endregion
}

