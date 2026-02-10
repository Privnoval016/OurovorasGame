using Extensions.UI;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Main menu UI controller for the overworld menu.
/// Manages all 8 tabs and coordinates data providers.
/// Follows MVVM architecture with complete separation from game logic.
/// </summary>
public class OverworldMenuUI : MonoBehaviour, IService
{
    [Header("Event System")]
    public EventSystem eventSystem;
    
    [Header("Tab System")]
    public TabGroup tabGroup;
    
    [Header("Data Providers")]
    [SerializeField] private PlayerDataProvider playerDataProvider;
    [SerializeField] private EquipmentDataProvider equipmentDataProvider;
    [SerializeField] private ElementProgressDataProvider elementProgressDataProvider;
    [SerializeField] private SkillTreeDataProvider skillTreeDataProvider;
    [SerializeField] private InventoryDataProvider inventoryDataProvider;
    [SerializeField] private QuestDataProvider questDataProvider;
    [SerializeField] private CompendiumDataProvider compendiumDataProvider;
    
    [Header("Tab References")]
    [SerializeField] private CharacterStatsTab characterStatsTab;
    [SerializeField] private EquipmentTab equipmentTab;
    [SerializeField] private ElementProgressTab elementProgressTab;
    [SerializeField] private SkillTreeTab skillTreeTab;
    [SerializeField] private InventoryTab inventoryTab;
    [SerializeField] private MissionsTab missionsTab;
    [SerializeField] private CompendiumTab compendiumTab;
    [SerializeField] private SettingsTab settingsTab;
    
    // References for external access (backwards compatibility)
    [HideInInspector] public PlayerInventory playerInventory;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        Services.Register<OverworldMenuUI>(this);
        
        // Get event system reference
        if (eventSystem == null)
            eventSystem = EventSystem.current;
        
        // Initialize data providers if not assigned
        InitializeDataProviders();
        
        // Connect data providers to tabs
        ConnectDataProvidersToTabs();
    }
    
    private void Start()
    {
        // Get player inventory reference for backwards compatibility
        var playerController = Services.Get<PlayerController>();
        if (playerController != null)
            playerInventory = playerController.pi;
        
        // Start with menu closed
        CloseMenu();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeDataProviders()
    {
        // Find or create data providers
        if (playerDataProvider == null)
            playerDataProvider = GetComponentInChildren<PlayerDataProvider>(true);
        
        if (equipmentDataProvider == null)
            equipmentDataProvider = GetComponentInChildren<EquipmentDataProvider>(true);
        
        if (elementProgressDataProvider == null)
            elementProgressDataProvider = GetComponentInChildren<ElementProgressDataProvider>(true);
        
        if (skillTreeDataProvider == null)
            skillTreeDataProvider = GetComponentInChildren<SkillTreeDataProvider>(true);
        
        if (inventoryDataProvider == null)
            inventoryDataProvider = GetComponentInChildren<InventoryDataProvider>(true);
        
        if (questDataProvider == null)
            questDataProvider = GetComponentInChildren<QuestDataProvider>(true);
        
        if (compendiumDataProvider == null)
            compendiumDataProvider = GetComponentInChildren<CompendiumDataProvider>(true);
    }
    
    private void ConnectDataProvidersToTabs()
    {
        // Connect each tab to its data provider
        if (characterStatsTab != null && playerDataProvider != null)
            characterStatsTab.SetDataProvider(playerDataProvider);
        
        if (equipmentTab != null && equipmentDataProvider != null)
            equipmentTab.SetDataProvider(equipmentDataProvider);
        
        if (elementProgressTab != null && elementProgressDataProvider != null)
            elementProgressTab.SetDataProvider(elementProgressDataProvider);
        
        if (skillTreeTab != null && skillTreeDataProvider != null)
            skillTreeTab.SetDataProvider(skillTreeDataProvider);
        
        if (inventoryTab != null && inventoryDataProvider != null)
            inventoryTab.SetDataProvider(inventoryDataProvider);
        
        if (missionsTab != null && questDataProvider != null)
            missionsTab.SetDataProvider(questDataProvider);
        
        if (compendiumTab != null && compendiumDataProvider != null)
            compendiumTab.SetDataProvider(compendiumDataProvider);
    }
    
    #endregion
    
    #region Menu Control
    
    /// <summary>
    /// Opens the menu and activates tab navigation.
    /// CRITICAL: Ensures EventSystem is ready and Menu input is enabled.
    /// Resets to first tab every time menu opens.
    /// </summary>
    public void OpenMenu()
    {
        gameObject.SetActive(true);
        
        // CRITICAL: Reset to first tab when opening menu
        if (tabGroup != null)
        {
            tabGroup.SelectTabByIndex(0);
            tabGroup.tabActive = true;
        }
        
        // CRITICAL: Ensure Menu action map is enabled
        if (InputManager.Instance != null)
        {
            InputManager.Instance.EnableStateInputs(GameState.Menu);
        }
        
        // CRITICAL: Ensure EventSystem has something selected for controller navigation
        // This happens after a delay to ensure everything is initialized
        if (eventSystem != null)
        {
            // Clear current selection first
            eventSystem.SetSelectedGameObject(null);
            
            // The TabSelection will auto-select first element when tab becomes active
            // But we can also force it here as a backup
            if (tabGroup != null && tabGroup.selectedTab != null && tabGroup.selectedTab.contentPanel != null)
            {
                // Give it a frame to initialize
                StartCoroutine(SelectFirstElementNextFrame());
            }
        }
        
        Debug.Log("OverworldMenuUI: Menu opened, reset to first tab, controller navigation should work");
    }
    
    /// <summary>
    /// Closes the menu and deactivates tab navigation.
    /// Ensures all UI state is reset for next opening.
    /// </summary>
    public void CloseMenu()
    {
        gameObject.SetActive(false);
        
        if (tabGroup != null)
            tabGroup.tabActive = false;
        
        // Clear EventSystem selection
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);
        
        Debug.Log("OverworldMenuUI: Menu closed and reset");
    }
    
    private System.Collections.IEnumerator SelectFirstElementNextFrame()
    {
        yield return null; // Wait one frame
        
        if (tabGroup != null && tabGroup.selectedTab != null && tabGroup.selectedTab.contentPanel != null)
        {
            UnityEngine.UI.Selectable firstSelectable = tabGroup.selectedTab.contentPanel.GetComponentInChildren<UnityEngine.UI.Selectable>();
            if (firstSelectable != null && firstSelectable.IsInteractable())
            {
                eventSystem.SetSelectedGameObject(firstSelectable.gameObject);
                Debug.Log($"OverworldMenuUI: Force-selected {firstSelectable.gameObject.name}");
            }
        }
    }
    
    #endregion
}

