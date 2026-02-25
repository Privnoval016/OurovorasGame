using Extensions.UI;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Save game menu - shows 3 save slots for manual saves.
/// </summary>
public class SaveMenu : MonoBehaviour
{
    [Header("UI References")]
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
        if (slot1Button != null)
            slot1Button.onSlotSelected.AddListener(OnSlotSelected);
        
        if (slot2Button != null)
            slot2Button.onSlotSelected.AddListener(OnSlotSelected);
        
        if (slot3Button != null)
            slot3Button.onSlotSelected.AddListener(OnSlotSelected);
    }
    
    /// <summary>
    /// Shows the save menu and populates slots.
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
    /// Hides the save menu.
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
            Debug.LogWarning("SaveMenu: SaveDataProvider is null!");
            return;
        }
        
        var allSlots = saveDataProvider.GetAllSaveSlots();
        
        // Manual save slots are indices 0-2
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
    /// Called when a save slot is selected.
    /// </summary>
    private void OnSlotSelected(int slotIndex)
    {
        if (saveDataProvider == null)
        {
            Debug.LogWarning("SaveMenu: Cannot save - SaveDataProvider is null!");
            UIAudio.PlayError();
            return;
        }
        
        bool success = saveDataProvider.SaveToSlot(slotIndex);
        
        if (success)
        {
            UIAudio.PlaySelect();
            Debug.Log($"SaveMenu: Saved to slot {slotIndex}");
            
            // Refresh to show new save data
            RefreshSlots();
        }
        else
        {
            UIAudio.PlayError();
            Debug.LogWarning($"SaveMenu: Failed to save to slot {slotIndex}");
        }
    }
}

