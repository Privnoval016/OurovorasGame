using System.Collections.Generic;
using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tab 3: Element progression tab showing element levels and attack assignments.
/// Displays element selection on left, progress bars and attack assignment in center, character model on right.
/// </summary>
public class ElementProgressTab : TabSelection, IScrollMenuAuthority
{
    [Header("Data Provider")]
    [SerializeField] private MonoBehaviour elementDataProviderObject;
    private IElementProgressDataProvider elementDataProvider;
    
    [Header("UI Components - Element Selection")]
    [SerializeField] private Button[] elementButtons;
    [SerializeField] private Image[] elementButtonIcons;
    [SerializeField] private Color selectedElementColor = Color.yellow;
    [SerializeField] private Color unselectedElementColor = Color.white;
    
    [Header("UI Components - Progress Display")]
    [SerializeField] private TextMeshProUGUI elementNameText;
    [SerializeField] private ProgressLevelDisplay[] levelDisplays; // 10 level indicators
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    
    [Header("UI Components - Attack Assignment")]
    [SerializeField] private AttackAssignmentButton[] attackButtons; // 3 buttons (X, Y, A)
    [SerializeField] private ScrollMenu attackScrollMenu;
    [SerializeField] private GameObject scrollMenuContainer;
    [SerializeField] private VideoDisplay attackVideoDisplay;
    
    [Header("UI Components - Character Model")]
    [SerializeField] private RenderTextureDisplay characterModelDisplay;
    
    private ElementEffect currentElement = ElementEffect.Fire;
    private int currentAttackButtonIndex = -1;
    private bool isScrollMenuActive = false;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        if (elementDataProviderObject != null)
            elementDataProvider = elementDataProviderObject as IElementProgressDataProvider;
    }
    
    #endregion
    
    #region TabSelection Overrides
    
    /// <summary>
    /// Called when this tab is selected.
    /// </summary>
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
        
        // Initialize element buttons
        InitializeElementButtons();
        
        // Select first element by default
        SelectElement(0);
    }
    
    /// <summary>
    /// Called when this tab is deselected.
    /// </summary>
    public override void OnTabDeselect()
    {
        base.OnTabDeselect();
        
        if (isScrollMenuActive)
            DeactivateScrollMenu();
        
        if (characterModelDisplay != null)
            characterModelDisplay.Deactivate();
        
        if (attackVideoDisplay != null)
            attackVideoDisplay.Deactivate();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeElementButtons()
    {
        elementDataProvider ??= elementDataProviderObject as IElementProgressDataProvider;
        
        ElementEffect[] elements = elementDataProvider.GetAvailableElements();
        
        for (int i = 0; i < elementButtons.Length && i < elements.Length; i++)
        {
            if (elementButtons[i] != null)
            {
                int index = i;
                elementButtons[i].onClick.AddListener(() => SelectElement(index));
            }
        }
    }
    
    #endregion
    
    #region Element Selection
    
    /// <summary>
    /// Selects an element and updates all displays.
    /// </summary>
    /// <param name="elementIndex">The index of the element to select.</param>
    public void SelectElement(int elementIndex)
    {
        if (elementDataProvider == null)
            return;
        
        ElementEffect[] elements = elementDataProvider.GetAvailableElements();
        
        if (elementIndex < 0 || elementIndex >= elements.Length)
        {
            Debug.LogWarning($"ElementProgressTab: Invalid element index {elementIndex}");
            return;
        }
        
        currentElement = elements[elementIndex];
        
        UpdateElementButtonVisuals();
        UpdateProgressDisplay();
        UpdateAttackAssignments();
    }
    
    private void UpdateElementButtonVisuals()
    {
        if (elementDataProvider == null)
            return;
        
        ElementEffect[] elements = elementDataProvider.GetAvailableElements();
        
        for (int i = 0; i < elementButtonIcons.Length && i < elements.Length; i++)
        {
            if (elementButtonIcons[i] != null)
            {
                elementButtonIcons[i].color = elements[i] == currentElement
                    ? selectedElementColor
                    : unselectedElementColor;
            }
        }
    }
    
    #endregion
    
    #region Progress Display
    
    private void UpdateProgressDisplay()
    {
        if (elementDataProvider == null)
            return;
        
        ElementProgressData progressData = elementDataProvider.GetElementProgress(currentElement);
        
        if (elementNameText != null)
            elementNameText.text = currentElement.ToString();
        
        // Update level displays
        if (levelDisplays != null)
        {
            for (int i = 0; i < levelDisplays.Length; i++)
            {
                if (levelDisplays[i] != null)
                {
                    bool isUnlocked = i < progressData.currentLevel;
                    string description = progressData.GetLevelDescription(i);
                    levelDisplays[i].SetLevel(i + 1, isUnlocked, description);
                }
            }
        }
        
        // Update progress bar
        if (progressBar != null)
            progressBar.value = progressData.progressToNextLevel;
        
        if (progressText != null)
        {
            int nextLevel = progressData.currentLevel + 1;
            progressText.text = $"Level {progressData.currentLevel} → {nextLevel}";
        }
    }
    
    #endregion
    
    #region Attack Assignment
    
    private void UpdateAttackAssignments()
    {
        if (elementDataProvider == null || attackButtons == null)
            return;
        
        AttackDisplayData[] assignedAttacks = elementDataProvider.GetAssignedAttacks(currentElement);
        
        for (int i = 0; i < attackButtons.Length && i < assignedAttacks.Length; i++)
        {
            if (attackButtons[i] != null)
            {
                attackButtons[i].SetAttack(assignedAttacks[i]);
            }
        }
    }
    
    /// <summary>
    /// Called when an attack button is selected to change its assignment.
    /// </summary>
    /// <param name="buttonIndex">The button index (0=X, 1=Y, 2=A).</param>
    public void OnAttackButtonSelected(int buttonIndex)
    {
        currentAttackButtonIndex = buttonIndex;
        ActivateScrollMenu();
    }
    
    private void ActivateScrollMenu()
    {
        if (attackScrollMenu == null || elementDataProvider == null)
            return;
        
        List<AttackDisplayData> availableAttacks = elementDataProvider.GetAvailableAttacks(currentElement);
        
        if (availableAttacks == null || availableAttacks.Count == 0)
        {
            Debug.LogWarning($"ElementProgressTab: No attacks available for {currentElement}");
            return;
        }
        
        // Convert to ItemUIInfo for scroll menu
        List<ItemUIInfo> attackItems = new List<ItemUIInfo>();
        foreach (var attack in availableAttacks)
        {
            attackItems.Add(new ItemUIInfo
            {
                itemName = attack.attackName,
                itemDescription = attack.description,
                icon = attack.icon,
                itemRarity = Rarity.Common // Placeholder
            });
        }
        
        if (scrollMenuContainer != null)
            scrollMenuContainer.SetActive(true);
        
        attackScrollMenu.Activate(attackItems, 0, item => item, this);
        isScrollMenuActive = true;
    }
    
    private void DeactivateScrollMenu()
    {
        if (attackScrollMenu != null)
            attackScrollMenu.Deactivate();
        
        if (scrollMenuContainer != null)
            scrollMenuContainer.SetActive(false);
        
        if (attackVideoDisplay != null)
            attackVideoDisplay.Stop();
        
        isScrollMenuActive = false;
        currentAttackButtonIndex = -1;
    }
    
    #endregion
    
    #region IScrollMenuAuthority Implementation
    
    private void ScrollDelegate(InputAction.CallbackContext context)
    {
        if (!isScrollMenuActive || attackScrollMenu == null)
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
            InitializeElementButtons();
            SelectElement(0);
        }
    }
    
    #endregion
}