using System.Collections.Generic;
using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tab 7: Compendium tab for viewing discovered lore, enemies, items, and other entries.
/// Shows category selection on left, scrolling entry list in center.
/// </summary>
public class CompendiumTab : TabSelection, IScrollMenuAuthority
{
    [Header("Data Provider")]
    [SerializeField] private MonoBehaviour compendiumDataProviderObject;
    private ICompendiumDataProvider compendiumDataProvider;
    
    [Header("UI Components - Category Selection")]
    [SerializeField] private Button[] categoryButtons;
    [SerializeField] private TextMeshProUGUI[] categoryButtonTexts;
    [SerializeField] private Color selectedCategoryColor = Color.yellow;
    [SerializeField] private Color unselectedCategoryColor = Color.white;
    
    [Header("UI Components - Entry Display")]
    [SerializeField] private ScrollMenu entryScrollMenu;
    [SerializeField] private GameObject scrollMenuContainer;
    [SerializeField] private GameObject entryDetailsPanel;
    [SerializeField] private TextMeshProUGUI entryNameText;
    [SerializeField] private TextMeshProUGUI entryDescriptionText;
    [SerializeField] private Image entryIcon;
    [SerializeField] private GameObject undiscoveredIndicator;
    
    private string currentCategory = "All";
    private bool isScrollMenuActive = false;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        if (compendiumDataProviderObject != null)
            compendiumDataProvider = compendiumDataProviderObject as ICompendiumDataProvider;
    }
    
    #endregion
    
    #region TabSelection Overrides
    
    /// <summary>
    /// Called when this tab is selected.
    /// </summary>
    public override void OnTabSelect()
    {
        base.OnTabSelect();
        
        if (compendiumDataProvider == null)
        {
            Debug.LogError("CompendiumTab: No ICompendiumDataProvider assigned!");
            return;
        }
        
        // Initialize category buttons
        InitializeCategoryButtons();
        
        // Show all entries by default
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
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeCategoryButtons()
    {
        if (compendiumDataProvider == null)
            return;
        
        string[] categories = compendiumDataProvider.GetCategories();
        
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
    /// Selects a category and updates the entry display.
    /// </summary>
    /// <param name="category">The category to display.</param>
    public void SelectCategory(string category)
    {
        currentCategory = category;
        UpdateCategoryButtonVisuals();
        UpdateEntryDisplay();
    }
    
    private void UpdateCategoryButtonVisuals()
    {
        if (compendiumDataProvider == null)
            return;
        
        string[] categories = compendiumDataProvider.GetCategories();
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
    
    #region Entry Display
    
    private void UpdateEntryDisplay()
    {
        DeactivateScrollMenu();
        ActivateScrollMenu();
    }
    
    private void ActivateScrollMenu()
    {
        if (entryScrollMenu == null || compendiumDataProvider == null)
            return;
        
        List<CompendiumEntryData> entries;
        
        if (currentCategory == "All")
            entries = compendiumDataProvider.GetAllEntries();
        else
            entries = compendiumDataProvider.GetEntriesByCategory(currentCategory);
        
        if (entries == null || entries.Count == 0)
        {
            Debug.LogWarning($"CompendiumTab: No entries in category '{currentCategory}'");
            ClearEntryDetails();
            return;
        }
        
        // Convert entries to ItemUIInfo for scroll menu
        List<ItemUIInfo> entryItems = new List<ItemUIInfo>();
        foreach (var entry in entries)
        {
            entryItems.Add(new ItemUIInfo
            {
                itemName = entry.isDiscovered ? entry.entryName : "???",
                itemDescription = entry.isDiscovered ? entry.category : "Undiscovered",
                icon = entry.isDiscovered ? entry.icon : null,
                itemRarity = Rarity.Common
            });
        }
        
        if (scrollMenuContainer != null)
            scrollMenuContainer.SetActive(true);
        
        entryScrollMenu.Activate(entryItems, 0, item => item, this);
        isScrollMenuActive = true;
        
        // Display first entry details
        UpdateEntryDetails(entries[0]);
    }
    
    private void DeactivateScrollMenu()
    {
        if (entryScrollMenu != null)
            entryScrollMenu.Deactivate();
        
        isScrollMenuActive = false;
    }
    
    /// <summary>
    /// Called when an entry is selected in the scroll menu.
    /// </summary>
    /// <param name="entryIndex">The index of the selected entry.</param>
    public void OnEntrySelected(int entryIndex)
    {
        if (compendiumDataProvider == null)
            return;
        
        List<CompendiumEntryData> entries;
        
        if (currentCategory == "All")
            entries = compendiumDataProvider.GetAllEntries();
        else
            entries = compendiumDataProvider.GetEntriesByCategory(currentCategory);
        
        if (entries != null && entryIndex >= 0 && entryIndex < entries.Count)
            UpdateEntryDetails(entries[entryIndex]);
    }
    
    private void UpdateEntryDetails(CompendiumEntryData entry)
    {
        if (entry == null)
        {
            ClearEntryDetails();
            return;
        }
        
        if (entryDetailsPanel != null)
            entryDetailsPanel.SetActive(true);
        
        // Show discovered info or hide it
        bool discovered = entry.isDiscovered;
        
        if (entryNameText != null)
            entryNameText.text = discovered ? entry.entryName : "???";
        
        if (entryDescriptionText != null)
            entryDescriptionText.text = discovered ? entry.description : "This entry has not been discovered yet.";
        
        if (entryIcon != null)
        {
            entryIcon.sprite = discovered ? entry.icon : null;
            entryIcon.enabled = discovered && entry.icon != null;
        }
        
        if (undiscoveredIndicator != null)
            undiscoveredIndicator.SetActive(!discovered);
    }
    
    private void ClearEntryDetails()
    {
        if (entryDetailsPanel != null)
            entryDetailsPanel.SetActive(false);
        
        if (entryNameText != null)
            entryNameText.text = "";
        
        if (entryDescriptionText != null)
            entryDescriptionText.text = "No entries available";
        
        if (entryIcon != null)
            entryIcon.enabled = false;
        
        if (undiscoveredIndicator != null)
            undiscoveredIndicator.SetActive(false);
    }
    
    #endregion
    
    #region IScrollMenuAuthority Implementation
    
    private void ScrollDelegate(InputAction.CallbackContext context)
    {
        if (!isScrollMenuActive || entryScrollMenu == null)
            return;
        
        Vector2 scrollInput = context.ReadValue<Vector2>();
        entryScrollMenu.OnScrollPerformed(scrollInput);
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
    /// Sets the compendium data provider for this tab.
    /// </summary>
    /// <param name="provider">The compendium data provider.</param>
    public void SetDataProvider(ICompendiumDataProvider provider)
    {
        compendiumDataProvider = provider;
        
        if (gameObject.activeInHierarchy)
        {
            InitializeCategoryButtons();
            UpdateEntryDisplay();
        }
    }
    
    #endregion
}

