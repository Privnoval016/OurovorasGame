using Extensions.UI;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Load game menu - shows autosave + 3 manual save slots.
/// </summary>
public class LoadMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private SaveSlotButton autosaveButton;
    [SerializeField] private SaveSlotButton slot1Button;
    [SerializeField] private SaveSlotButton slot2Button;
    [SerializeField] private SaveSlotButton slot3Button;
    
    [Header("Data Provider")]
    [SerializeField] private SaveDataProvider saveDataProvider;
    
    [Header("Default Selection")]
    [SerializeField] private SaveSlotButton defaultButton;
    
    private EventSystem eventSystem;
    
    private void Awake()
    {
        eventSystem = EventSystem.current;
        
        // Subscribe to slot selection
        if (autosaveButton != null)
            autosaveButton.onSlotSelected.AddListener(OnSlotSelected);
        
        if (slot1Button != null)
            slot1Button.onSlotSelected.AddListener(OnSlotSelected);
        
        if (slot2Button != null)
            slot2Button.onSlotSelected.AddListener(OnSlotSelected);
        
        if (slot3Button != null)
            slot3Button.onSlotSelected.AddListener(OnSlotSelected);
    }
    
    /// <summary>
    /// Shows the load menu and populates slots.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        
        // Refresh slot data
        RefreshSlots();
        
        // Select default slot
        if (defaultButton != null && eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(defaultButton.gameObject);
        }
    }
    
    /// <summary>
    /// Hides the load menu.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Refreshes all save slot displays.
    /// </summary>
    private void RefreshSlots()
    {
        if (saveDataProvider == null)
        {
            Debug.LogWarning("LoadMenu: SaveDataProvider is null!");
            return;
        }
        
        var allSlots = saveDataProvider.GetAllSaveSlots();
        
        // Autosave (index -1)
        var autosaveData = allSlots.Find(s => s.slotIndex == -1);
        if (autosaveButton != null && autosaveData != null)
        {
            autosaveButton.Initialize(
                autosaveData.slotIndex,
                autosaveData.saveName,
                autosaveData.timestamp,
                autosaveData.playerLevel.ToString(),
                autosaveData.isEmpty
            );
        }
        
        // Manual save slots (indices 0-2)
        for (int i = 0; i < 3; i++)
        {
            var slotData = allSlots.Find(s => s.slotIndex == i);
            SaveSlotButton button = i == 0 ? slot1Button : (i == 1 ? slot2Button : slot3Button);
            
            if (button != null && slotData != null)
            {
                button.Initialize(
                    slotData.slotIndex,
                    slotData.saveName,
                    slotData.timestamp,
                    slotData.playerLevel.ToString(),
                    slotData.isEmpty
                );
            }
        }
    }
    
    /// <summary>
    /// Called when a save slot is selected for loading.
    /// </summary>
    private void OnSlotSelected(int slotIndex)
    {
        if (saveDataProvider == null)
        {
            Debug.LogWarning("LoadMenu: Cannot load - SaveDataProvider is null!");
            UIAudio.PlayError();
            return;
        }
        
        // Don't allow loading empty slots
        if (saveDataProvider.IsSlotEmpty(slotIndex))
        {
            Debug.Log($"LoadMenu: Slot {slotIndex} is empty, cannot load");
            UIAudio.PlayError();
            return;
        }
        
        bool success = saveDataProvider.LoadFromSlot(slotIndex);
        
        if (success)
        {
            UIAudio.PlaySelect();
            Debug.Log($"LoadMenu: Loaded from slot {slotIndex}");
        }
        else
        {
            UIAudio.PlayError();
            Debug.LogWarning($"LoadMenu: Failed to load from slot {slotIndex}");
        }
    }
}

