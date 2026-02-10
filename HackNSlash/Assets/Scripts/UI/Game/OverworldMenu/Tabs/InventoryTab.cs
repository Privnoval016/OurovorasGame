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
    
    [Header("UI Components - Character Model")]
    [SerializeField] private RenderTextureDisplay characterModelDisplay;
    
    private string currentCategory = "All";
    private bool isScrollMenuActive = false;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        if (inventoryDataProviderObject != null)
            inventoryDataProvider = inventoryDataProviderObject as IInventoryDataProvider;
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

