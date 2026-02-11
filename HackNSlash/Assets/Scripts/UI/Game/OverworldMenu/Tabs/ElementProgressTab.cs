using System.Collections.Generic;
using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Tab 3: Element progression tab showing element levels and attack assignments.
/// NAVIGATION LAYERS:
/// 1. Element Selection (left side)
/// 2. Attack Buttons OR Unlock Grid (center)
/// 3. Scroll Menu (for attack reassignment)
/// </summary>
public class ElementProgressTab : TabSelection, IScrollMenuAuthority
{
    [Header("Data Provider")]
    [SerializeField] private MonoBehaviour elementDataProviderObject;
    private IElementProgressDataProvider elementDataProvider;
    
    [Header("Element Selection (Left Side)")]
    [SerializeField] private ElementSlotUI[] elementSlots; // 5 element slots
    [SerializeField] private GameObject elementSelectionContainer;
    
    [Header("Progress Display (Top Center)")]
    [SerializeField] private TextMeshProUGUI elementNameText;
    [SerializeField] private UnlockSlotUI[] levelDisplays; // 10 level indicators in horizontal row
    [SerializeField] private GameObject unlockGridContainer; // The 3x3 grid that appears when navigating up
    [SerializeField] private Button unlockGridButton; // Single button that represents the unlock grid (navigable in Layer 2)
    
    [Header("Attack Assignment (Bottom Center)")]
    [SerializeField] private AttackButtonSlotUI[] attackButtons; // 3 buttons (X, Y, A)
    [SerializeField] private GameObject attackButtonsContainer;
    [SerializeField] private ScrollMenu attackScrollMenu;
    [SerializeField] private GameObject scrollMenuContainer;
    
    [Header("Description Display (Under Video)")]
    [SerializeField] private DescriptionDisplay descriptionDisplay;
    
    [Header("Video/Character Display (Right Side)")]
    [SerializeField] private RenderTextureDisplay characterModelDisplay;
    [SerializeField] private GameObject videoPlayerOverlay; // Shows over render texture when needed
    
    [Header("Navigation")]
    [SerializeField] private Selectable defaultButton; // First element slot
    
    private EventSystem eventSystem;
    private ElementEffect currentElement = ElementEffect.Fire;
    private int currentAttackButtonIndex = -1;
    
    // Navigation state
    private enum NavigationState
    {
        ElementSelection,  // Selecting which element
        AttackButtons,     // Navigating attack assignment buttons
        UnlockGrid,        // Navigating unlock grid
        ScrollMenu         // Reassigning attack
    }
    private NavigationState currentState = NavigationState.ElementSelection;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        eventSystem = EventSystem.current;
        
        if (elementDataProviderObject != null)
            elementDataProvider = elementDataProviderObject as IElementProgressDataProvider;
        
        // Hide containers initially
        if (scrollMenuContainer != null)
            scrollMenuContainer.SetActive(false);
        
        if (unlockGridContainer != null)
            unlockGridContainer.SetActive(true);
        
        // Subscribe to input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.onBack += OnBackInput;
        }
    }
    
    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.onBack -= OnBackInput;
        }
    }
    
    #endregion
    
    #region TabSelection Override
    
    public override void OnTabSelect()
    {
        base.OnTabSelect();
        
        if (elementDataProvider == null)
        {
            Debug.LogError("ElementProgressTab: No IElementProgressDataProvider assigned!");
            return;
        }
        
        // Activate character model
        if (characterModelDisplay != null)
            characterModelDisplay.Activate();
        
        // Initialize elements and UI
        InitializeElements();
        
        // Set default navigation to first element slot
        if (defaultButton != null)
        {
            eventSystem.SetSelectedGameObject(defaultButton.gameObject);
        }
        
        currentState = NavigationState.ElementSelection;
    }
    
    public override void OnTabDeselect()
    {
        base.OnTabDeselect();
        
        // Clean up
        if (scrollMenuContainer != null && scrollMenuContainer.activeSelf)
        {
            DeactivateScrollMenu();
        }
        
        if (characterModelDisplay != null)
            characterModelDisplay.Deactivate();
    }
    
    #endregion
    
    #region Input Callbacks
    
    private void OnBackInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        if (currentState == NavigationState.ScrollMenu)
        {
            // Exit scroll menu back to attack buttons
            DeactivateScrollMenu();
        }
        else if (currentState == NavigationState.UnlockGrid)
        {
            // Exit unlock grid back to Layer 2 (attack buttons + unlock grid button)
            ExitUnlockGrid();
        }
    }
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// Initializes element slots with data from provider.
    /// </summary>
    private void InitializeElements()
    {
        if (elementDataProvider == null || elementSlots == null || elementSlots.Length == 0)
            return;
        
        var availableElements = elementDataProvider.GetAvailableElements();
        
        for (int i = 0; i < elementSlots.Length && i < availableElements.Length; i++)
        {
            var element = availableElements[i];
            var slot = elementSlots[i];
            
            if (slot != null)
            {
                // Initialize slot with element data
                Sprite icon = null; // TODO: Get element icon from asset
                slot.Initialize(element, i, icon);
                
                // Subscribe to selection event via reflection or make onElementSelected public
                // For now, manually hook up in editor or make public
            }
        }
        
        if (availableElements.Length < elementSlots.Length)
        {
            // Hide unused slots
            for (int i = availableElements.Length; i < elementSlots.Length; i++)
            {
                if (elementSlots[i] != null)
                {
                    elementSlots[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {            
            // Ensure all slots are active
            for (int i = 0; i < elementSlots.Length; i++)
            {
                if (elementSlots[i] != null)                {
                    elementSlots[i].gameObject.SetActive(true);
                }
            }
        }
        
        // Load first element by default
        if (availableElements.Length > 0)
        {
            LoadElement(availableElements[0]);
        }
    }
    
    #endregion
    
    #region Element Selection
    
    public void OnElementSelected(int elementIndex)
    {
        if (elementDataProvider == null) return;
        
        var availableElements = elementDataProvider.GetAvailableElements();
        
        if (elementIndex < 0 || elementIndex >= availableElements.Length) return;
        
        OnElementSelected(availableElements[elementIndex]);
    }
    
    /// <summary>
    /// Called when user selects an element slot.
    /// Call this from ElementSlotUI's onElementSelected event.
    /// </summary>
    public void OnElementSelected(ElementEffect element)
    {
        LoadElement(element);
        
        // Transition to attack buttons
        currentState = NavigationState.AttackButtons;
        
        // Select first attack button
        if (attackButtons != null && attackButtons.Length > 0 && attackButtons[0] != null)
        {
            eventSystem.SetSelectedGameObject(attackButtons[0].gameObject);
        }
    }
    
    /// <summary>
    /// Loads element data and refreshes UI.
    /// </summary>
    private void LoadElement(ElementEffect element)
    {
        currentElement = element;
        
        // Update element name
        if (elementNameText != null)
        {
            elementNameText.text = element.ToString();
        }
        
        // Update progress display
        RefreshProgressDisplay();
        
        // Update attack buttons
        RefreshAttackButtons();
    }
    
    /// <summary>
    /// Refreshes the progress bar and level displays for current element.
    /// </summary>
    private void RefreshProgressDisplay()
    {
        if (elementDataProvider == null) return;
        
        var progressData = elementDataProvider.GetElementProgress(currentElement);
        
        // Update level displays (first 10 as horizontal indicators)
        if (levelDisplays != null)
        {
            for (int i = 0; i < levelDisplays.Length; i++)
            {
                if (levelDisplays[i] != null)
                {
                    bool unlocked = i < progressData.currentLevel;
                    string desc = i < progressData.levelDescriptions.Length ? progressData.levelDescriptions[i] : "";
                    
                    levelDisplays[i].Initialize(i, $"Level {i+1}", desc, i+1, unlocked, null);
                    
                    // Subscribe to hover event for description display
                    levelDisplays[i].onUnlockHovered.RemoveAllListeners();
                    int capturedIndex = i;
                    levelDisplays[i].onUnlockHovered.AddListener(_ => OnUnlockHovered(capturedIndex));
                }
            }
        }
        
        // Set up unlock grid button to enter grid navigation
        if (unlockGridButton != null)
        {
            unlockGridButton.onClick.RemoveAllListeners();
            unlockGridButton.onClick.AddListener(EnterUnlockGrid);
        }
    }
    
    /// <summary>
    /// Refreshes attack button assignments for current element.
    /// </summary>
    private void RefreshAttackButtons()
    {
        if (elementDataProvider == null || attackButtons == null) return;
        
        var assignedAttacks = elementDataProvider.GetAssignedAttacks(currentElement);
        
        for (int i = 0; i < attackButtons.Length && i < assignedAttacks.Length; i++)
        {
            if (attackButtons[i] != null)
            {
                // Initialize button with index and icon
                Sprite buttonIcon = null; // TODO: Get button icon (X/Y/A) from asset
                attackButtons[i].Initialize(i, buttonIcon);
                
                // Set assigned attack
                attackButtons[i].SetAttack(assignedAttacks[i]);
            }
        }
    }
    
    #endregion
    
    #region Attack Assignment
    
    /// <summary>
    /// Called when user hovers over an attack button (shows description).
    /// Call this from AttackButtonSlotUI's onButtonHovered event.
    /// </summary>
    public void OnAttackButtonHovered(int buttonIndex)
    {
        if (currentState != NavigationState.AttackButtons) return;
        
        if (buttonIndex < 0 || buttonIndex >= attackButtons.Length) return;
        
        var currentAttack = attackButtons[buttonIndex].GetCurrentAttack();
        
        if (descriptionDisplay != null)
        {
            if (currentAttack != null && !string.IsNullOrEmpty(currentAttack.attackName))
            {
                descriptionDisplay.ShowAttack(currentAttack);
            }
            else
            {
                descriptionDisplay.Hide();
            }
        }
    }
    
    /// <summary>
    /// Called when user presses A on an attack button to reassign.
    /// Call this from AttackButtonSlotUI's onButtonSelected event.
    /// </summary>
    public void OnAttackButtonSelected(int buttonIndex)
    {
        currentAttackButtonIndex = buttonIndex;
        ActivateScrollMenu();
    }
    
    /// <summary>
    /// Activates the scroll menu for attack selection.
    /// </summary>
    private void ActivateScrollMenu()
    {
        if (attackScrollMenu == null || scrollMenuContainer == null) return;
        
        var availableAttacks = elementDataProvider.GetAvailableAttacks(currentElement);
        
        if (availableAttacks == null || availableAttacks.Count == 0)
        {
            Debug.LogWarning("No available attacks for element!");
            return;
        }
        
        // Show scroll menu
        scrollMenuContainer.SetActive(true);
        
        // Populate with attacks
        attackScrollMenu.Activate(
            availableAttacks,
            0,
            attack => new ItemUIInfo
            {
                itemName = attack.attackName,
                itemDescription = attack.description,
                icon = attack.icon
            },
            this
        );
        
        currentState = NavigationState.ScrollMenu;
    }
    
    /// <summary>
    /// Deactivates the scroll menu.
    /// </summary>
    private void DeactivateScrollMenu()
    {
        if (attackScrollMenu == null || scrollMenuContainer == null) return;
        
        attackScrollMenu.Deactivate();
        scrollMenuContainer.SetActive(false);
        
        // Return to attack buttons
        currentState = NavigationState.AttackButtons;
        
        if (currentAttackButtonIndex >= 0 && currentAttackButtonIndex < attackButtons.Length)
        {
            eventSystem.SetSelectedGameObject(attackButtons[currentAttackButtonIndex].gameObject);
        }
    }
    
    #endregion
    
    #region Unlock Grid Navigation
    
    /// <summary>
    /// Called when user presses A on the unlock grid button.
    /// Enters the unlock grid for detailed navigation.
    /// </summary>
    private void EnterUnlockGrid()
    {
        if (unlockGridContainer == null) return;
        
        // Show the grid container
        unlockGridContainer.SetActive(true);
        
        // Change state to unlock grid
        currentState = NavigationState.UnlockGrid;
        
        // Select first unlocked item in grid
        if (levelDisplays != null && levelDisplays.Length > 0)
        {
            // Find first unlocked unlock slot
            for (int i = 0; i < levelDisplays.Length; i++)
            {
                if (levelDisplays[i] != null && levelDisplays[i].IsUnlocked())
                {
                    eventSystem.SetSelectedGameObject(levelDisplays[i].gameObject);
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Called when user presses B to exit unlock grid.
    /// Returns to Layer 2 (attack buttons + unlock grid button).
    /// </summary>
    private void ExitUnlockGrid()
    {
        if (unlockGridContainer != null)
        {
            unlockGridContainer.SetActive(false);
        }
        
        // Return to attack buttons layer
        currentState = NavigationState.AttackButtons;
        
        // Select the unlock grid button
        if (unlockGridButton != null)
        {
            eventSystem.SetSelectedGameObject(unlockGridButton.gameObject);
        }
    }
    
    /// <summary>
    /// Called when user hovers over an unlock slot in the grid.
    /// Shows unlock description below.
    /// </summary>
    public void OnUnlockHovered(int unlockIndex)
    {
        if (currentState != NavigationState.UnlockGrid) return;
        
        if (unlockIndex < 0 || unlockIndex >= levelDisplays.Length) return;
        
        var unlock = levelDisplays[unlockIndex];
        if (unlock != null && descriptionDisplay != null)
        {
            descriptionDisplay.ShowUnlock(unlock.GetUnlockName(), unlock.GetDescription());
        }
    }
    
    #endregion
    
    #region IScrollMenuAuthority Implementation
    
    private void ScrollDelegate(InputAction.CallbackContext context)
    {
        if (currentState != NavigationState.ScrollMenu || attackScrollMenu == null)
            return;
        
        Vector2 scrollInput = context.ReadValue<Vector2>();
        attackScrollMenu.OnScrollPerformed(scrollInput);
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
    /// Sets the element progress data provider for this tab.
    /// </summary>
    /// <param name="provider">The element progress data provider.</param>
    public void SetDataProvider(IElementProgressDataProvider provider)
    {
        elementDataProvider = provider;
        
        if (gameObject.activeInHierarchy)
        {
            // If tab is currently active, refresh display with new provider
            LoadElement(currentElement);
        }
    }

    #endregion
}