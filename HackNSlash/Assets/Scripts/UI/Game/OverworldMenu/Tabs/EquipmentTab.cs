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
    }
    
    #endregion
    
    #region TabSelection Overrides
    
    /// <summary>
    /// Called when this tab is selected.
    /// </summary>
    public override void OnTabSelect()
    {
        base.OnTabSelect();
        
        if (equipmentDataProvider == null)
        {
            Debug.LogError("EquipmentMenuUI: No IEquipmentDataProvider assigned!");
            return;
        }
        
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
        
        // Update current item display
        UpdateCurrentItemDisplay();
    }
    
    private void DeactivateScrollMenu()
    {
        if (equipmentScrollMenu != null)
            equipmentScrollMenu.Deactivate();
        
        if (scrollMenuContainer != null)
            scrollMenuContainer.SetActive(false);
        
        isScrollMenuActive = false;
        currentSlotType = EquipmentSlotType.None;
        currentSlotIndex = -1;
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
    /// Called when an accessory slot is selected.
    /// </summary>
    /// <param name="slotIndex">The slot index.</param>
    public void OnAccessorySlotSelected(int slotIndex)
    {
        currentSlotType = EquipmentSlotType.Accessory;
        currentSlotIndex = slotIndex;
        ActivateScrollMenu();
    }
    
    /// <summary>
    /// Called when an accessory slot is deselected.
    /// </summary>
    public void OnAccessorySlotDeselected()
    {
        if (currentSlotType == EquipmentSlotType.Accessory)
            DeactivateScrollMenu();
    }
    
    /// <summary>
    /// Called when a passive slot is selected.
    /// </summary>
    /// <param name="slotIndex">The slot index.</param>
    public void OnPassiveSlotSelected(int slotIndex)
    {
        currentSlotType = EquipmentSlotType.Passive;
        currentSlotIndex = slotIndex;
        ActivateScrollMenu();
    }
    
    /// <summary>
    /// Called when a passive slot is deselected.
    /// </summary>
    public void OnPassiveSlotDeselected()
    {
        if (currentSlotType == EquipmentSlotType.Passive)
            DeactivateScrollMenu();
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
}

