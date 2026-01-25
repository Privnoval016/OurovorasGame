using System;
using Extensions.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EquipmentMenuUI : TabSelection, IScrollMenuAuthority
{
    public Button defaultButton;
    public GameObject selectedButtonObject;
    
    public ScrollMenu equipmentScrollMenu;
    
    private EventSystem eventSystem;
    private InventoryInfo inventoryInfo;
    private EquipmentLoadout equipmentLoadout;
    
    private int currentCategoryIndex = -1;
    private int currentItemIndex = -1;
    
    private int currentItemStackIndexInUI = -1;
    
    #region MonoBehaviour Callbacks
    
    private void Start()
    {
        eventSystem = Services.Get<OverworldMenuUI>().eventSystem;
        
        inventoryInfo = Services.Get<OverworldMenuUI>().playerInventory.inventoryInfo;
        equipmentLoadout = Services.Get<OverworldMenuUI>().playerInventory.CurrentLoadout;
    }

    private void Update()
    {
        selectedButtonObject = eventSystem.currentSelectedGameObject;
    }
    
    #endregion
    
    
    #region TabSelection Methods

    public override void OnTabSelect()
    {
        gameObject.SetActive(true);
        
        eventSystem = Services.Get<OverworldMenuUI>().eventSystem;
        inventoryInfo = Services.Get<OverworldMenuUI>().playerInventory.inventoryInfo;
        equipmentLoadout = Services.Get<OverworldMenuUI>().playerInventory.CurrentLoadout;
        
        if (defaultButton != null)
            eventSystem.SetSelectedGameObject(defaultButton.gameObject);
    }
    
    public override void OnTabDeselect()
    {
        gameObject.SetActive(false);
        equipmentScrollMenu.Deactivate();
    }
    
    #endregion
    
    #region Scroll Menu Methods
    
    private void ActivateScrollMenu()
    {
        if (equipmentScrollMenu == null) return;
        
        InitializeScrollMenu();
    }
    
    private void DeactivateScrollMenu()
    {
        if (equipmentScrollMenu == null) return;
        
        equipmentScrollMenu.Deactivate();
    }
    
    #region Scroll Menu Initialization Methods
    
    private void InitializeScrollMenu()
    {
        switch (currentCategoryIndex)
        {
            case 0:
                InitializeAccessoryMenu();
                break;
            case 1:
                InitializePassiveMenu();
                break;
        }
    }
    
    private void InitializeAccessoryMenu()
    {
        var currentAccessory = equipmentLoadout.equippedAccessories[currentItemIndex];
        currentItemStackIndexInUI = inventoryInfo.GetIndexOfStack(currentAccessory);
        
        equipmentScrollMenu.Activate(inventoryInfo.GetStacksOfType<Accessory>(), 
            currentItemStackIndexInUI, stack => stack.GetItemUIInfo(), this);
    }

    private void InitializePassiveMenu()
    {

    }
    
    #endregion
    
    #region Scroll Input
    
    private void ScrollDelegate(InputAction.CallbackContext context)
    {
        Vector2 v = context.ReadValue<Vector2>();
        equipmentScrollMenu.OnScrollPerformed(v);
    }
    
    
    public void SubscribeToScroll(ScrollMenu scrollMenu)
    {
        InputManager.Instance.onScroll += ScrollDelegate;
    }
    
    public void UnsubscribeFromScroll(ScrollMenu scrollMenu)
    {
        InputManager.Instance.onScroll -= ScrollDelegate;
    }
    
    #endregion
    
    #endregion
    
    #region Button Callbacks
    
    public void OnAccessorySlotClicked(int index)
    {
        currentCategoryIndex = 0;
        currentItemIndex = index;
        print($"Equipment slot {index} clicked");
        
        ActivateScrollMenu();
    }
    
    public void OnAccessorySlotDeselected()
    {
        print($"Equipment slot {currentItemIndex} deselected");
        DeactivateScrollMenu();
    }
    
    public void OnPassiveSlotClicked(int index)
    {
        currentCategoryIndex = 1;
        currentItemIndex = index;
        print($"Passive slot {index} clicked");
    }
    
    public void OnPassiveSlotDeselected()
    {
        print($"Passive slot {currentItemIndex} deselected");
        DeactivateScrollMenu();
    }
    
    #endregion
}