using System;
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
    
    [Header("Render Texture Controller")]
    [SerializeField] private SharedRenderTextureController renderTextureController;
    
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
        // Start with menu closed
        CloseMenu();
    }

    private void OnEnable()
    {
        tabGroup.OnTabSwitched += OnTabSwitched;
    }
    
    private void OnDisable()
    {
        tabGroup.OnTabSwitched -= OnTabSwitched;
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
        
        // CRITICAL: Ensure all tabs are deactivated first to prevent stale state
        if (characterStatsTab != null) characterStatsTab.gameObject.SetActive(false);
        if (equipmentTab != null) equipmentTab.gameObject.SetActive(false);
        if (elementProgressTab != null) elementProgressTab.gameObject.SetActive(false);
        if (skillTreeTab != null) skillTreeTab.gameObject.SetActive(false);
        if (inventoryTab != null) inventoryTab.gameObject.SetActive(false);
        if (missionsTab != null) missionsTab.gameObject.SetActive(false);
        if (compendiumTab != null) compendiumTab.gameObject.SetActive(false);
        if (settingsTab != null) settingsTab.gameObject.SetActive(false);
        
        renderTextureController.Activate();
        
        // CRITICAL: Reset to first tab when opening menu
        if (tabGroup != null)
        {
            tabGroup.SelectTabByIndex(0, force: true);
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
        
        renderTextureController.Deactivate();
        
        if (tabGroup != null)
            tabGroup.tabActive = false;
        
        // Clear EventSystem selection
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);
        
        Debug.Log("OverworldMenuUI: Menu closed and reset");
    }
    
    private void OnTabSwitched(int oldIndex, int newIndex)
    {
        renderTextureController.MoveToTab(newIndex, true);
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

