using System.Collections.Generic;
using Extensions.UI;
using TMPro;
using UnityEngine;
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
    [SerializeField] private Button[] categoryButtons;
    [SerializeField] private TextMeshProUGUI[] categoryButtonTexts;
    [SerializeField] private Color selectedCategoryColor = Color.yellow;
    [SerializeField] private Color unselectedCategoryColor = Color.white;
    
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
    
    [Header("UI Components - Equipped Indicator")]
    [SerializeField] private Image equippedIndicator; // Shows when item is currently equipped
    
    [Header("UI Components - Sort")]
    [SerializeField] private TextMeshProUGUI sortMethodText; // Shows current sort method
    [SerializeField] private GameObject sortIndicator; // Container for sort display
    
    [Header("UI Components - Character Model")]
    [SerializeField] private RenderTextureDisplay characterModelDisplay;
    
    [Header("Data Providers for Equipment Check")]
    [SerializeField] private MonoBehaviour equipmentDataProviderObject;
    private IEquipmentDataProvider equipmentDataProvider;
    
    private string currentCategory = "All";
    private bool isScrollMenuActive;
    private bool isActionMenuActive;
    private ItemUIInfo currentSelectedItem;
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
        
        // Hide equipped indicator initially
        if (equippedIndicator != null)
            equippedIndicator.gameObject.SetActive(false);
        
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
                
                if (categoryButtonTexts != null && i < categoryButtonTexts.Length && categoryButtonTexts[i] != null)
                    categoryButtonTexts[i].text = category;
                
                categoryButtons[i].onClick.AddListener(() => SelectCategory(category));
            }
        }
    }
    
    #endregion
    
    #region Category Selection
    
    /// <summary>
    /// Selects a category and updates the item display.
    /// </summary>
    /// <param name="category">The category to display.</param>
    public void SelectCategory(string category)
    {
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
        
        for (int i = 0; i < categoryButtonTexts.Length && i < allCategories.Length; i++)
        {
            if (categoryButtonTexts[i] != null)
            {
                categoryButtonTexts[i].color = allCategories[i] == currentCategory
                    ? selectedCategoryColor
                    : unselectedCategoryColor;
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
        
        List<ItemUIInfo> items;
        
        if (currentCategory == "All")
            items = inventoryDataProvider.GetAllItems();
        else
            items = inventoryDataProvider.GetItemsByCategory(currentCategory);
        
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning($"InventoryTab: No items in category '{currentCategory}'");
            
            // Clear item info display
            ClearItemInfo();
            return;
        }
        
        // Sort items based on current sort method
        items = SortItems(items);
        
        if (scrollMenuContainer != null)
            scrollMenuContainer.SetActive(true);
        
        itemScrollMenu.Activate(items, 0, item => item, this);
        isScrollMenuActive = true;
        
        // Display first item info
        UpdateSelectedItemInfo(items[0]);
    }
    
    private void DeactivateScrollMenu()
    {
        if (itemScrollMenu != null)
            itemScrollMenu.Deactivate();
        
        isScrollMenuActive = false;
    }
    
    private void UpdateSelectedItemInfo(ItemUIInfo itemInfo)
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
        
        // Update equipped indicator
        UpdateEquippedIndicator();
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
        
        // Hide equipped indicator
        if (equippedIndicator != null)
            equippedIndicator.gameObject.SetActive(false);
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
        
        // If scroll menu is active, open action menu
        if (isScrollMenuActive && itemScrollMenu != null)
        {
            currentSelectedItem = itemScrollMenu.GetSelectedItem();
            if (currentSelectedItem != null)
            {
                OpenActionMenu();
            }
        }
    }
    
    private void OnBackInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        
        // Close action menu if open
        if (isActionMenuActive)
        {
            CloseActionMenu();
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
    private List<ItemUIInfo> SortItems(List<ItemUIInfo> items)
    {
        if (items == null || items.Count == 0)
            return items;
        
        var sortedItems = new List<ItemUIInfo>(items);
        
        switch (currentSortMethod)
        {
            case InventorySortMethod.NameAscending:
                sortedItems.Sort((a, b) => string.Compare(a.itemName, b.itemName, System.StringComparison.Ordinal));
                break;
            
            case InventorySortMethod.NameDescending:
                sortedItems.Sort((a, b) => string.Compare(b.itemName, a.itemName, System.StringComparison.Ordinal));
                break;
            
            case InventorySortMethod.RarityDescending:
                sortedItems.Sort((a, b) => b.itemRarity.CompareTo(a.itemRarity));
                break;
            
            case InventorySortMethod.RarityAscending:
                sortedItems.Sort((a, b) => a.itemRarity.CompareTo(b.itemRarity));
                break;
            
            case InventorySortMethod.QuantityDescending:
                sortedItems.Sort((a, b) => b.amount.CompareTo(a.amount));
                break;
            
            case InventorySortMethod.QuantityAscending:
                sortedItems.Sort((a, b) => a.amount.CompareTo(b.amount));
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
                sortedItems.Sort((a, b) => string.Compare(a.category, b.category, System.StringComparison.Ordinal));
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
        bool isEquipped = IsItemEquipped(currentSelectedItem.itemName);
        
        if (isEquipped)
        {
            // Unequip item first
            UnequipItem(currentSelectedItem.itemName);
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
    /// </summary>
    private bool IsItemEquipped(string itemName)
    {
        if (equipmentDataProvider == null)
            return false;
        
        // Check accessories
        for (int i = 0; i < 3; i++)
        {
            var equippedItem = equipmentDataProvider.GetEquippedAccessory(i);
            if (equippedItem != null && equippedItem.itemName == itemName)
                return true;
        }
        
        // Check passives
        for (int i = 0; i < 3; i++)
        {
            var equippedItem = equipmentDataProvider.GetEquippedPassive(i);
            if (equippedItem != null && equippedItem.itemName == itemName)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Unequips an item from all slots.
    /// </summary>
    private void UnequipItem(string itemName)
    {
        if (equipmentDataProvider == null)
            return;
        
        // Check and unequip from accessories
        for (int i = 0; i < 3; i++)
        {
            var equippedItem = equipmentDataProvider.GetEquippedAccessory(i);
            if (equippedItem != null && equippedItem.itemName == itemName)
            {
                equipmentDataProvider.UnequipAccessory(i);
            }
        }
        
        // Check and unequip from passives
        for (int i = 0; i < 3; i++)
        {
            var equippedItem = equipmentDataProvider.GetEquippedPassive(i);
            if (equippedItem != null && equippedItem.itemName == itemName)
            {
                equipmentDataProvider.UnequipPassive(i);
            }
        }
    }
    
    /// <summary>
    /// Updates the equipped indicator based on whether the current item is equipped.
    /// </summary>
    private void UpdateEquippedIndicator()
    {
        if (equippedIndicator == null || currentSelectedItem == null)
            return;
        
        bool isEquipped = IsItemEquipped(currentSelectedItem.itemName);
        equippedIndicator.gameObject.SetActive(isEquipped);
    }
    
    #endregion
    
    #region IScrollMenuAuthority Implementation
    
    private void ScrollDelegate(InputAction.CallbackContext context)
    {
        if (!isScrollMenuActive || itemScrollMenu == null)
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

