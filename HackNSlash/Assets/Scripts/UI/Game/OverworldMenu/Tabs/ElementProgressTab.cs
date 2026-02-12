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
    [SerializeField] private UnlockGridButtonVisual unlockGridButton; // Single button that represents the unlock grid (navigable in Layer 2)
    
    [Header("Attack Assignment (Bottom Center)")]
    [SerializeField] private AttackButtonSlotUI[] attackButtons; // 3 buttons (X, Y, A)
    [SerializeField] private GameObject attackButtonsContainer;
    [SerializeField] private ScrollMenu attackScrollMenu;
    [SerializeField] private GameObject scrollMenuContainer;
    [SerializeField] private SelectableContainer scrollMenuSelectable; // Needed to receive input in scroll menu
    
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
    
    // Cache the available attacks list when scroll menu is activated to maintain references
    private List<AttackDisplayData> cachedAvailableAttacks;
    
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
        
        // Set up unlock grid button onClick listener
        if (unlockGridButton != null)
        {
            unlockGridButton.onClick.AddListener(EnterUnlockGrid);
        }
        
        // Subscribe to input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.onBack += OnBackInput;
            InputManager.Instance.onSelect += OnSubmitInput;
        }
    }
    
    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.onBack -= OnBackInput;
            InputManager.Instance.onSelect -= OnSubmitInput;
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
    
    private void OnSubmitInput(InputAction.CallbackContext context)
    {
        // Only handle if scroll menu is active and button was pressed (not released)
        if (!context.performed || currentState != NavigationState.ScrollMenu)
            return;
        
        Debug.Log("ElementProgressTab: Submit (A button) pressed in scroll menu");
        
        // Invoke the confirmation event directly
        if (attackScrollMenu != null)
        {
            int selectedIndex = attackScrollMenu.GetSelectedIndex();
            attackScrollMenu.OnItemConfirmed?.Invoke(selectedIndex);
        }
    }
    
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
        else if (currentState == NavigationState.AttackButtons)
        {
            // Exit attack buttons back to element selection
            ReturnToElementSelection();
        }
    }
    
    /// <summary>
    /// Gets the currently selected attack from the scroll menu.
    /// </summary>
    private AttackDisplayData GetSelectedAttackFromScrollMenu()
    {
        if (attackScrollMenu == null || elementDataProvider == null) return null;
        
        int selectedIndex = attackScrollMenu.GetSelectedIndex();
        var availableAttacks = elementDataProvider.GetAvailableAttacks(currentElement);
        
        if (selectedIndex >= 0 && selectedIndex < availableAttacks.Count)
        {
            return availableAttacks[selectedIndex];
        }
        
        return null;
    }
    
    /// <summary>
    /// Assigns the selected attack to the current button slot.
    /// If the attack is already assigned to another button, swaps them.
    /// </summary>
    private void AssignAttackWithSwap(AttackDisplayData selectedAttack)
    {
        if (elementDataProvider == null || selectedAttack == null) return;
        
        // Use cached available attacks - these are the SAME references we used to populate the scroll menu
        if (cachedAvailableAttacks == null || cachedAvailableAttacks.Count == 0)
        {
            Debug.LogWarning("ElementProgressTab: Cached available attacks is null or empty!");
            return;
        }
        
        // IndexOf now works because we're using the cached list with same references
        int selectedAttackIndex = cachedAvailableAttacks.IndexOf(selectedAttack);
        
        if (selectedAttackIndex < 0)
        {
            Debug.LogWarning($"ElementProgressTab: Selected attack '{selectedAttack.attackName}' not found in cached attacks list!");
            return;
        }
        
        // CRITICAL: Check if this attack is already assigned to another button
        int existingButtonIndex = FindButtonWithAttack(selectedAttack);
        
        // If attack is already assigned to a different button, perform a swap
        if (existingButtonIndex >= 0 && existingButtonIndex != currentAttackButtonIndex)
        {
            Debug.Log($"ElementProgressTab: Attack '{selectedAttack.attackName}' is already assigned to button {existingButtonIndex}, swapping with button {currentAttackButtonIndex}");
            
            // Perform the swap
            bool swapped = SwapAttacks(currentAttackButtonIndex, existingButtonIndex);
            
            if (swapped)
            {
                UIAudio.PlayItemEquip();
                RefreshAttackButtons();
                Debug.Log($"ElementProgressTab: Successfully swapped attacks between buttons {currentAttackButtonIndex} and {existingButtonIndex}");
            }
            else
            {
                UIAudio.PlayError();
                Debug.LogWarning($"ElementProgressTab: Failed to swap attacks");
            }
            
            return;
        }
        
        // Otherwise, assign normally using the attack index
        bool assigned = elementDataProvider.AssignAttack(currentElement, currentAttackButtonIndex, selectedAttackIndex);
        
        if (assigned)
        {
            UIAudio.PlayItemEquip();
            RefreshAttackButtons();
            Debug.Log($"ElementProgressTab: Assigned attack '{selectedAttack.attackName}' to button {currentAttackButtonIndex}");
        }
        else
        {
            UIAudio.PlayError();
            Debug.LogWarning($"ElementProgressTab: Failed to assign attack '{selectedAttack.attackName}'");
        }
    }
    
    /// <summary>
    /// Finds which button has the specified attack assigned.
    /// Returns button index or -1 if not found.
    /// </summary>
    private int FindButtonWithAttack(AttackDisplayData attack)
    {
        if (attackButtons == null || attack == null) return -1;
        
        for (int i = 0; i < attackButtons.Length; i++)
        {
            if (attackButtons[i] != null)
            {
                AttackDisplayData buttonAttack = attackButtons[i].GetCurrentAttack();
                // Compare by name, not reference
                if (buttonAttack != null && buttonAttack.attackName == attack.attackName)
                {
                    return i;
                }
            }
        }
        
        return -1;
    }
    
    /// <summary>
    /// Swaps attacks between two button slots.
    /// </summary>
    private bool SwapAttacks(int buttonA, int buttonB)
    {
        if (elementDataProvider == null) return false;
        
        // Get both attacks
        AttackDisplayData attackA = null;
        AttackDisplayData attackB = null;
        
        if (buttonA >= 0 && buttonA < attackButtons.Length)
            attackA = attackButtons[buttonA].GetCurrentAttack();
        if (buttonB >= 0 && buttonB < attackButtons.Length)
            attackB = attackButtons[buttonB].GetCurrentAttack();
        
        // Get available attacks list to find indices
        var availableAttacks = elementDataProvider.GetAvailableAttacks(currentElement);
        
        // Convert attacks to indices (-1 if null/not found)
        int indexA = attackA != null ? availableAttacks.IndexOf(attackA) : -1;
        int indexB = attackB != null ? availableAttacks.IndexOf(attackB) : -1;
        
        // Perform swap via data provider using indices
        // Note: Passing -1 for index will unassign that slot
        if (elementDataProvider.AssignAttack(currentElement, buttonA, indexB) && 
            elementDataProvider.AssignAttack(currentElement, buttonB, indexA))
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Returns from attack buttons/unlock grid back to element selection.
    /// </summary>
    private void ReturnToElementSelection()
    {
        currentState = NavigationState.ElementSelection;
        
        // Select the element slot for current element
        if (elementSlots != null && elementDataProvider != null)
        {
            var availableElements = elementDataProvider.GetAvailableElements();
            for (int i = 0; i < availableElements.Length && i < elementSlots.Length; i++)
            {
                if (availableElements[i] == currentElement && elementSlots[i] != null)
                {
                    eventSystem.SetSelectedGameObject(elementSlots[i].gameObject);
                    return;
                }
            }
        }
        
        // Fallback to first element slot
        if (defaultButton != null)
        {
            eventSystem.SetSelectedGameObject(defaultButton.gameObject);
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
    
    public void OnElementHovered(int elementIndex)
    {
        if (elementDataProvider == null) return;
        
        var availableElements = elementDataProvider.GetAvailableElements();
        
        if (elementIndex < 0 || elementIndex >= availableElements.Length) return;
        
        OnElementHovered(availableElements[elementIndex]);
    }
    
    /// <summary>
    /// Called when user HOVERS over an element slot (updates UI without transitioning).
    /// </summary>
    public void OnElementHovered(ElementEffect element)
    {
        // Only update UI if in element selection state
        if (currentState == NavigationState.ElementSelection)
        {
            LoadElement(element);
        }
    }
    
    /// <summary>
    /// Called when user PRESSES A on an element slot (transitions to attack buttons).
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
        
        // CRITICAL: Cache the available attacks to maintain references
        cachedAvailableAttacks = availableAttacks;
        
        // CRITICAL: Disable all attack buttons and unlock grid button so they can't be navigated to
        SetLayerTwoInteractable(false);
        
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
        
        // Subscribe to item confirmation event
        if (attackScrollMenu != null)
        {
            attackScrollMenu.OnItemConfirmed += OnScrollMenuItemConfirmed;
        }
        
        // CRITICAL: Select the SelectableContainer so it can receive A/B button input
        if (scrollMenuSelectable != null)
        {
            scrollMenuSelectable.SelectThis();
        }
        else
        {
            // If no SelectableContainer, try to get it at runtime from the container
            if (scrollMenuContainer != null)
            {
                var selectable = scrollMenuContainer.GetComponent<SelectableContainer>();
                if (selectable != null)
                {
                    selectable.SelectThis();
                }
                else
                {
                    Debug.LogWarning("ElementProgressTab: No SelectableContainer found! Scroll menu won't receive input.");
                }
            }
        }
        
        currentState = NavigationState.ScrollMenu;
    }
    
    /// <summary>
    /// Called when user confirms selection in scroll menu (via OnItemConfirmed event).
    /// </summary>
    private void OnScrollMenuItemConfirmed(int itemIndex)
    {
        Debug.Log($"ElementProgressTab: OnScrollMenuItemConfirmed called with index {itemIndex}");
        
        if (cachedAvailableAttacks == null || cachedAvailableAttacks.Count == 0)
        {
            Debug.LogWarning("ElementProgressTab: Cached available attacks is null or empty!");
            return;
        }
        
        if (itemIndex >= 0 && itemIndex < cachedAvailableAttacks.Count)
        {
            AttackDisplayData selectedAttack = cachedAvailableAttacks[itemIndex];
            AssignAttackWithSwap(selectedAttack);
        }
        else
        {
            Debug.LogWarning($"ElementProgressTab: Invalid item index {itemIndex}");
        }
    }
    
    /// <summary>
    /// Deactivates the scroll menu.
    /// </summary>
    private void DeactivateScrollMenu()
    {
        if (attackScrollMenu == null || scrollMenuContainer == null) return;
        
        // Unsubscribe from event
        if (attackScrollMenu != null)
        {
            attackScrollMenu.OnItemConfirmed -= OnScrollMenuItemConfirmed;
        }
        
        attackScrollMenu.Deactivate();
        scrollMenuContainer.SetActive(false);
        
        // Clear the cached attacks
        cachedAvailableAttacks = null;
        
        // CRITICAL: Re-enable attack buttons and unlock grid button for navigation
        SetLayerTwoInteractable(true);
        
        // Return to attack buttons
        currentState = NavigationState.AttackButtons;
        
        if (currentAttackButtonIndex >= 0 && currentAttackButtonIndex < attackButtons.Length)
        {
            eventSystem.SetSelectedGameObject(attackButtons[currentAttackButtonIndex].gameObject);
        }
    }
    
    /// <summary>
    /// Sets the interactability of all Layer 2 elements (attack buttons + unlock grid button).
    /// Used to prevent navigation to them when scroll menu is active.
    /// </summary>
    private void SetLayerTwoInteractable(bool interactable)
    {
        // Disable/enable attack buttons
        if (attackButtons != null)
        {
            foreach (var button in attackButtons)
            {
                if (button != null)
                {
                    button.interactable = interactable;
                }
            }
        }
        
        // Disable/enable unlock grid button
        if (unlockGridButton != null)
        {
            unlockGridButton.interactable = interactable;
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
        Debug.Log("ElementProgressTab: EnterUnlockGrid called");
        
        if (unlockGridContainer == null)
        {
            Debug.LogWarning("ElementProgressTab: unlockGridContainer is null!");
            return;
        }
        
        // Show the grid container
        unlockGridContainer.SetActive(true);
        
        // Change state to unlock grid
        currentState = NavigationState.UnlockGrid;
        
        // Make sure all unlock slots are interactable
        if (levelDisplays != null)
        {
            for (int i = 0; i < levelDisplays.Length; i++)
            {
                if (levelDisplays[i] != null)
                {
                    levelDisplays[i].interactable = true;
                }
            }
        }
        
        // Select first unlocked item in grid
        if (levelDisplays != null && levelDisplays.Length > 0)
        {
            // Find first unlocked unlock slot
            for (int i = 0; i < levelDisplays.Length; i++)
            {
                if (levelDisplays[i] != null && levelDisplays[i].IsUnlocked())
                {
                    Debug.Log($"ElementProgressTab: Selecting unlock slot {i}");
                    if (eventSystem != null)
                    {
                        eventSystem.SetSelectedGameObject(levelDisplays[i].gameObject);
                    }
                    else
                    {
                        Debug.LogWarning("ElementProgressTab: EventSystem is null!");
                    }
                    break;
                }
            }
        }
        else
        {
            Debug.LogWarning("ElementProgressTab: No level displays found!");
        }
    }
    
    /// <summary>
    /// Called when user presses B to exit unlock grid.
    /// Returns to Layer 2 (attack buttons + unlock grid button).
    /// </summary>
    private void ExitUnlockGrid()
    {
        Debug.Log("ElementProgressTab: ExitUnlockGrid called");
        
        if (unlockGridContainer != null)
        {
            unlockGridContainer.SetActive(false);
        }
        
        // Return to attack buttons layer
        currentState = NavigationState.AttackButtons;
        
        // Select the unlock grid button
        if (unlockGridButton != null)
        {
            Debug.Log("ElementProgressTab: Selecting unlock grid button");
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(unlockGridButton.gameObject);
            }
        }
        else
        {
            Debug.LogWarning("ElementProgressTab: Cannot select unlock grid button - it's null!");
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