using Extensions.UI;
using UnityEngine;

/// <summary>
/// Tab 1: Character/Stats tab showing player information and equipped items.
/// Displays player stats on the left and a 3D character model in the center.
/// </summary>
public class CharacterStatsTab : TabSelection
{
    [Header("Data Provider")]
    [SerializeField] private MonoBehaviour playerDataProviderObject;
    private IPlayerDataProvider playerDataProvider;
    
    [Header("UI Components")]
    [SerializeField] private PlayerStatsDisplay statsDisplay;
    [SerializeField] private ItemSlotUI[] equippedItemSlots;
    [SerializeField] private RenderTextureDisplay characterModelDisplay;
    
    [Header("Refresh Settings")]
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float refreshInterval = 0.5f;
    
    private float lastRefreshTime;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        // Get the data provider interface from the assigned object
        if (playerDataProviderObject != null)
            playerDataProvider = playerDataProviderObject as IPlayerDataProvider;
    }
    
    private void Update()
    {
        if (!gameObject.activeInHierarchy || !autoRefresh)
            return;
        
        if (Time.time - lastRefreshTime >= refreshInterval)
        {
            RefreshDisplay();
            lastRefreshTime = Time.time;
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
        InitializeTab();
    }
    
    /// <summary>
    /// Called when this tab is deselected.
    /// </summary>
    public override void OnTabDeselect()
    {
        base.OnTabDeselect();
        
        ForceDeselect();
        
        if (characterModelDisplay != null)
            characterModelDisplay.Deactivate();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeTab()
    {
        playerDataProvider ??= playerDataProviderObject as IPlayerDataProvider;
        
        // Activate character model display
        if (characterModelDisplay != null)
            characterModelDisplay.Activate();
        
        // Initial data load
        RefreshDisplay();
        lastRefreshTime = Time.time;
    }
    
    #endregion
    
    #region Display Updates
    
    /// <summary>
    /// Refreshes all displays with current data from the provider.
    /// </summary>
    public void RefreshDisplay()
    {
        if (playerDataProvider == null)
            return;
        
        UpdateStatsDisplay();
        UpdateEquippedItems();
    }
    
    private void UpdateStatsDisplay()
    {
        if (statsDisplay == null)
            return;
        
        PlayerStatsDisplayData statsData = playerDataProvider.GetPlayerStats();
        statsDisplay.UpdateDisplay(statsData);
    }
    
    private void UpdateEquippedItems()
    {
        if (equippedItemSlots == null || equippedItemSlots.Length == 0)
            return;
        
        EquippedItemDisplayData[] equippedItems = playerDataProvider.GetEquippedItems();
        
        for (int i = 0; i < equippedItemSlots.Length && i < equippedItems.Length; i++)
        {
            if (equippedItemSlots[i] != null)
            {
                equippedItemSlots[i].Initialize(i);
                equippedItemSlots[i].SetItemData(equippedItems[i]);
            }
        }
    }
    
    private void ForceDeselect()
    {
        // Deselect all equipped item slots to prevent lingering selection
        if (equippedItemSlots != null)
        {
            foreach (var slot in equippedItemSlots)
            {
                if (slot != null)
                    slot.ForceDeselect();
            }
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Sets the data provider for this tab.
    /// </summary>
    /// <param name="provider">The player data provider.</param>
    public void SetDataProvider(IPlayerDataProvider provider)
    {
        playerDataProvider = provider;
        
        if (gameObject.activeInHierarchy)
            RefreshDisplay();
    }
    
    #endregion
}

