using System.Collections.Generic;
using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tab 2: Equipment selection tab for managing equipped items.
/// Shows equipment slots on the left, scrolling menu in center, and character model on right.
/// Manually checks for B button to close scroll menu (can't use ICancelHandler due to cleared EventSystem selection).
/// </summary>
public class EquipmentTab : TabSelection, IScrollMenuAuthority
{
    [Header("Data Provider")]
    [SerializeField] private MonoBehaviour equipmentDataProviderObject;
    private IEquipmentDataProvider equipmentDataProvider;
    
    [Header("UI Components - Slots")]
    [SerializeField] private ItemSlotUI[] accessorySlots;
    [SerializeField] private ItemSlotUI[] passiveSlots;
    [SerializeField] private GameObject slotsContainer;
    
    [Header("UI Components - Scroll Menu")]
    [SerializeField] private ScrollMenu equipmentScrollMenu;
    [SerializeField] private GameObject scrollMenuContainer;
    
    [Header("UI Components - Info Display")]
    [SerializeField] private TextMeshProUGUI currentItemNameText;
    [SerializeField] private TextMeshProUGUI currentItemDescriptionText;
    [SerializeField] private Image currentItemIcon;
    [SerializeField] private TextMeshProUGUI selectedItemNameText;
    [SerializeField] private TextMeshProUGUI selectedItemDescriptionText;
    [SerializeField] private Image selectedItemIcon;
    
    [Header("UI Components - Character Model")]
    [SerializeField] private RenderTextureDisplay characterModelDisplay;
    
    [Header("UI References")]
    [SerializeField] private Button defaultButton;
    
    private EventSystem eventSystem;
    private EquipmentSlotType currentSlotType = EquipmentSlotType.None;
    private int currentSlotIndex = -1;
    private bool isScrollMenuActive = false;
    
    #region Enums
    
    private enum EquipmentSlotType
    {
        None,
        Accessory,
        Passive
    }
    
    #endregion
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        // Get the data provider interface
        if (equipmentDataProviderObject != null)
            equipmentDataProvider = equipmentDataProviderObject as IEquipmentDataProvider;
        
        InitializeSlots();
    }
    
    private void Start()
    {
        eventSystem = EventSystem.current;
        
        if (scrollMenuContainer != null)
            scrollMenuContainer.SetActive(false);
        
        // Subscribe to back/cancel input for closing scroll menu
        if (InputManager.Instance != null)
        {
            InputManager.Instance.onBack += OnBackInput;
            // CRITICAL: Subscribe to Submit (A button) for item selection in scroll menu
            InputManager.Instance.onSelect += OnSubmitInput;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.onBack -= OnBackInput;
            InputManager.Instance.onSelect -= OnSubmitInput;
        }
    }
    
    private void OnSubmitInput(InputAction.CallbackContext context)
    {
        // Only handle if scroll menu is active and button was pressed (not released)
        if (!context.performed || !isScrollMenuActive)
            return;
        
        Debug.Log("EquipmentTab: Submit (A button) pressed in scroll menu");
        
        // Get current selected item and equip it
        if (equipmentScrollMenu != null)
        {
            int selectedIndex = equipmentScrollMenu.GetSelectedIndex();
            equipmentScrollMenu.OnItemConfirmed?.Invoke(selectedIndex);
        }
    }
    
    private void OnBackInput(InputAction.CallbackContext context)
    {
        // Only handle if scroll menu is active and button was pressed (not released)
        if (!context.performed || !isScrollMenuActive)
            return;
        
        DeactivateScrollMenu();
        UIAudio.PlayBack();
    }
    
    #endregion
    
    #region TabSelection Overrides
    
    /// <summary>
    /// Called when this tab is selected.
    /// </summary>
    public override void OnTabSelect()
    {
        base.OnTabSelect();
        
        equipmentDataProvider ??= equipmentDataProviderObject as IEquipmentDataProvider;
        
        // Activate character model
        if (characterModelDisplay != null)
            characterModelDisplay.Activate();
        
        // Refresh slot displays
        RefreshAllSlots();
        
        // Set default selection
        if (defaultButton != null && eventSystem != null)
            eventSystem.SetSelectedGameObject(defaultButton.gameObject);
    }
    
    /// <summary>
    /// Called when this tab is deselected.
    /// </summary>
    public override void OnTabDeselect()
    {
        base.OnTabDeselect();
        
        // Deactivate scroll menu if active
        if (isScrollMenuActive)
            DeactivateScrollMenu();
        
        // Force deselect all slots and clear highlights
        ClearAllSlotSelections();
        
        // Clear EventSystem selection to prevent highlights from sticking
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);
        
        // Deactivate character model
        if (characterModelDisplay != null)
            characterModelDisplay.Deactivate();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeSlots()
    {
        // Initialize accessory slots
        if (accessorySlots != null)
        {
            for (int i = 0; i < accessorySlots.Length; i++)
            {
                if (accessorySlots[i] != null)
                    accessorySlots[i].Initialize(i);
            }
        }
        
        // Initialize passive slots
        if (passiveSlots != null)
        {
            for (int i = 0; i < passiveSlots.Length; i++)
            {
                if (passiveSlots[i] != null)
                    passiveSlots[i].Initialize(i);
            }
        }
    }
    
    #endregion
    
    #region Display Updates
    
    private void RefreshAllSlots()
    {
        RefreshAccessorySlots();
        RefreshPassiveSlots();
    }
    
    private void RefreshAccessorySlots()
    {
        if (equipmentDataProvider == null || accessorySlots == null)
            return;
        
        for (int i = 0; i < accessorySlots.Length; i++)
        {
            if (accessorySlots[i] != null)
            {
                EquippedItemDisplayData data = equipmentDataProvider.GetEquippedAccessory(i);
                accessorySlots[i].SetItemData(data);
            }
        }
    }
    
    private void RefreshPassiveSlots()
    {
        if (equipmentDataProvider == null || passiveSlots == null)
            return;
        
        for (int i = 0; i < passiveSlots.Length; i++)
        {
            if (passiveSlots[i] != null)
            {
                EquippedItemDisplayData data = equipmentDataProvider.GetEquippedPassive(i);
                passiveSlots[i].SetItemData(data);
            }
        }
    }
    
    #endregion
    
    #region Scroll Menu Management
    
    private void ActivateScrollMenu()
    {
        if (equipmentScrollMenu == null || equipmentDataProvider == null)
            return;
        
        // CRITICAL: Disable slot navigation so controller can't navigate back to slots
        DisableSlotNavigation();
        
        // Clear EventSystem selection so slots don't stay highlighted
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);
        
        if (scrollMenuContainer != null)
            scrollMenuContainer.SetActive(true);
        
        List<ItemUIInfo> items = null;
        int currentItemIndex = 0;
        
        // Get items based on slot type
        switch (currentSlotType)
        {
            case EquipmentSlotType.Accessory:
                items = equipmentDataProvider.GetAccessories();
                // Try to find currently equipped item in list
                var equippedAccessory = equipmentDataProvider.GetEquippedAccessory(currentSlotIndex);
                if (items != null && !equippedAccessory.isEmpty)
                {
                    currentItemIndex = items.FindIndex(item => item.itemName == equippedAccessory.itemName);
                    if (currentItemIndex < 0) currentItemIndex = 0;
                }
                break;
            
            case EquipmentSlotType.Passive:
                items = equipmentDataProvider.GetPassives();
                var equippedPassive = equipmentDataProvider.GetEquippedPassive(currentSlotIndex);
                if (items != null && !equippedPassive.isEmpty)
                {
                    currentItemIndex = items.FindIndex(item => item.itemName == equippedPassive.itemName);
                    if (currentItemIndex < 0) currentItemIndex = 0;
                }
                break;
        }
        
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning($"EquipmentMenuUI: No items available for {currentSlotType}");
            return;
        }
        
        // Activate scroll menu with items
        equipmentScrollMenu.Activate(items, currentItemIndex, item => item, this);
        isScrollMenuActive = true;
        
        // Subscribe to item confirmation (A button press)
        equipmentScrollMenu.OnItemConfirmed += OnItemConfirmed;
        
        // Subscribe to scroll input to update selected item display
        if (InputManager.Instance != null)
            InputManager.Instance.onScroll += OnScrollUpdateDisplay;
        
        // CRITICAL: Select the scroll menu container so it can receive Cancel (B button) input
        // This makes the scroll menu receive input events
        if (scrollMenuContainer != null && eventSystem != null)
        {
            // Check if container has SelectableContainer component
            var selectableContainer = scrollMenuContainer.GetComponent<SelectableContainer>();
            if (selectableContainer != null)
            {
                selectableContainer.SelectThis();
            }
            else
            {
                // Fallback: Just set it as selected even without SelectableContainer
                eventSystem.SetSelectedGameObject(scrollMenuContainer);
            }
        }
        
        // Update current item display (what's currently equipped)
        UpdateCurrentItemDisplay();
        
        // Update selected item display (what's being looked at in scroll menu)
        UpdateSelectedItemDisplay();
    }
    
    private void DeactivateScrollMenu()
    {
        if (equipmentScrollMenu != null)
        {
            // Unsubscribe from item confirmation
            equipmentScrollMenu.OnItemConfirmed -= OnItemConfirmed;
            equipmentScrollMenu.Deactivate();
        }
        
        // Unsubscribe from scroll display updates
        if (InputManager.Instance != null)
            InputManager.Instance.onScroll -= OnScrollUpdateDisplay;
        
        if (scrollMenuContainer != null)
            scrollMenuContainer.SetActive(false);
        
        isScrollMenuActive = false;
        
        // CRITICAL: Re-enable slot navigation so controller can navigate slots again
        EnableSlotNavigation();
        
        // Return focus to the slot that opened the scroll menu
        if (eventSystem != null && currentSlotIndex >= 0)
        {
            ItemSlotUI[] slots = currentSlotType == EquipmentSlotType.Accessory ? accessorySlots : passiveSlots;
            if (slots != null && currentSlotIndex < slots.Length && slots[currentSlotIndex] != null)
            {
                var button = slots[currentSlotIndex].GetComponent<Button>();
                if (button != null)
                    eventSystem.SetSelectedGameObject(button.gameObject);
            }
        }
        
        currentSlotType = EquipmentSlotType.None;
        currentSlotIndex = -1;
    }
    
    /// <summary>
    /// Called when user presses A button to confirm item selection in scroll menu.
    /// Equips the item but keeps scroll menu open for continued browsing.
    /// </summary>
    private void OnItemConfirmed(int itemIndex)
    {
        if (equipmentDataProvider == null)
            return;
        
        Debug.Log($"EquipmentTab: Item confirmed at index {itemIndex}");
        
        // Get the selected item
        ItemUIInfo selectedItem = equipmentScrollMenu.GetSelectedItem();
        if (selectedItem == null)
        {
            Debug.LogWarning("EquipmentTab: Selected item is null!");
            return;
        }
        
        // Actually equip the item via data provider
        bool equipped = false;
        switch (currentSlotType)
        {
            case EquipmentSlotType.Accessory:
                Debug.Log($"EquipmentTab: Equipping accessory '{selectedItem.itemName}' to slot {currentSlotIndex}");
                equipped = equipmentDataProvider.EquipAccessory(currentSlotIndex, selectedItem);
                break;
            
            case EquipmentSlotType.Passive:
                Debug.Log($"EquipmentTab: Equipping passive '{selectedItem.itemName}' to slot {currentSlotIndex}");
                equipped = equipmentDataProvider.EquipPassive(currentSlotIndex, selectedItem);
                break;
        }
        
        if (equipped)
        {
            UIAudio.PlayItemEquip();
            
            // Refresh the slot display to show newly equipped item
            RefreshAllSlots();
            
            // Update the "currently equipped" display in the scroll menu
            UpdateCurrentItemDisplay();
        }
        else
        {
            UIAudio.PlayError();
            Debug.LogWarning($"EquipmentTab: Failed to equip item '{selectedItem.itemName}'");
        }
        
        // DON'T close scroll menu - let user continue browsing and equipping different items
        // They can press B to close when done
    }
    
    /// <summary>
    /// Called when scroll input is received - updates the selected item display.
    /// </summary>
    private void OnScrollUpdateDisplay(InputAction.CallbackContext context)
    {
        if (!isScrollMenuActive || equipmentScrollMenu == null)
            return;
        
        // Wait a frame for scroll menu to update its selection
        StartCoroutine(UpdateSelectedItemDisplayNextFrame());
    }
    
    private System.Collections.IEnumerator UpdateSelectedItemDisplayNextFrame()
    {
        yield return null;
        UpdateSelectedItemDisplay();
    }
    
    private void UpdateSelectedItemDisplay()
    {
        if (equipmentScrollMenu == null)
            return;
        
        ItemUIInfo selectedItem = equipmentScrollMenu.GetSelectedItem();
        if (selectedItem == null)
            return;
        
        if (selectedItemNameText != null)
            selectedItemNameText.text = selectedItem.itemName;
        if (selectedItemDescriptionText != null)
            selectedItemDescriptionText.text = selectedItem.itemDescription;
        if (selectedItemIcon != null && selectedItem.icon != null)
        {
            selectedItemIcon.sprite = selectedItem.icon;
            selectedItemIcon.enabled = true;
        }
    }
    
    private void UpdateCurrentItemDisplay()
    {
        if (equipmentDataProvider == null)
            return;
        
        EquippedItemDisplayData currentData = null;
        
        switch (currentSlotType)
        {
            case EquipmentSlotType.Accessory:
                currentData = equipmentDataProvider.GetEquippedAccessory(currentSlotIndex);
                break;
            case EquipmentSlotType.Passive:
                currentData = equipmentDataProvider.GetEquippedPassive(currentSlotIndex);
                break;
        }
        
        if (currentData != null)
        {
            if (currentItemNameText != null)
                currentItemNameText.text = currentData.itemName;
            if (currentItemDescriptionText != null)
                currentItemDescriptionText.text = currentData.itemDescription;
            if (currentItemIcon != null && currentData.icon != null)
            {
                currentItemIcon.sprite = currentData.icon;
                currentItemIcon.enabled = true;
            }
        }
    }
    
    #endregion
    
    #region IScrollMenuAuthority Implementation
    
    private void ScrollDelegate(InputAction.CallbackContext context)
    {
        if (!isScrollMenuActive || equipmentScrollMenu == null)
            return;
        
        Vector2 scrollInput = context.ReadValue<Vector2>();
        Debug.Log($"ScrollDelegate: Received scroll input: {scrollInput}, isScrollMenuActive: {isScrollMenuActive}");
        equipmentScrollMenu.OnScrollPerformed(scrollInput);
    }
    
    /// <summary>
    /// Subscribes to scroll input when the scroll menu is activated.
    /// </summary>
    public void SubscribeToScroll(ScrollMenu scrollMenu)
    {
        if (InputManager.Instance != null)
            InputManager.Instance.onScroll += ScrollDelegate;
    }
    
    /// <summary>
    /// Unsubscribes from scroll input when the scroll menu is deactivated.
    /// </summary>
    public void UnsubscribeFromScroll(ScrollMenu scrollMenu)
    {
        if (InputManager.Instance != null)
            InputManager.Instance.onScroll -= ScrollDelegate;
    }
    

    #endregion
    
    #region Slot Callbacks
    
    /// <summary>
    /// Called when an accessory slot is selected (A button pressed).
    /// Opens scroll menu to choose equipment.
    /// </summary>
    /// <param name="slotIndex">The slot index.</param>
    public void OnAccessorySlotSelected(int slotIndex)
    {
        currentSlotType = EquipmentSlotType.Accessory;
        currentSlotIndex = slotIndex;
        ActivateScrollMenu();
    }
    
    /// <summary>
    /// Called when a passive slot is selected (A button pressed).
    /// Opens scroll menu to choose passive ability.
    /// </summary>
    /// <param name="slotIndex">The slot index.</param>
    public void OnPassiveSlotSelected(int slotIndex)
    {
        currentSlotType = EquipmentSlotType.Passive;
        currentSlotIndex = slotIndex;
        ActivateScrollMenu();
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Sets the equipment data provider for this tab.
    /// </summary>
    /// <param name="provider">The equipment data provider.</param>
    public void SetDataProvider(IEquipmentDataProvider provider)
    {
        equipmentDataProvider = provider;
        
        if (gameObject.activeInHierarchy)
            RefreshAllSlots();
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Clears all slot selections and visual highlights.
    /// </summary>
    private void ClearAllSlotSelections()
    {
        // Force deselect all accessory slots
        if (accessorySlots != null)
        {
            foreach (var slot in accessorySlots)
            {
                if (slot != null)
                    slot.ForceDeselect();
            }
        }
        
        // Force deselect all passive slots
        if (passiveSlots != null)
        {
            foreach (var slot in passiveSlots)
            {
                if (slot != null)
                    slot.ForceDeselect();
            }
        }
    }
    
    /// <summary>
    /// Disables all slot buttons to prevent navigation while scroll menu is active.
    /// </summary>
    private void DisableSlotNavigation()
    {
        SetSlotsInteractable(false);
    }
    
    /// <summary>
    /// Re-enables all slot buttons after scroll menu is closed.
    /// </summary>
    private void EnableSlotNavigation()
    {
        SetSlotsInteractable(true);
    }
    
    /// <summary>
    /// Sets interactability of all slot buttons.
    /// </summary>
    private void SetSlotsInteractable(bool interactable)
    {
        // Set accessory slots
        if (accessorySlots != null)
        {
            foreach (var slot in accessorySlots)
            {
                if (slot != null)
                {
                    var button = slot.GetComponent<Button>();
                    if (button != null)
                        button.interactable = interactable;
                }
            }
        }
        
        // Set passive slots
        if (passiveSlots != null)
        {
            foreach (var slot in passiveSlots)
            {
                if (slot != null)
                {
                    var button = slot.GetComponent<Button>();
                    if (button != null)
                        button.interactable = interactable;
                }
            }
        }
    }
    
    #endregion
}

