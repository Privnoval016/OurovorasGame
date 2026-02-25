using System.Collections.Generic;
using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tab 6: Missions/Quests tab for viewing active and completed quests.
/// Shows quest list on right, selected quest details on left, character model behind details.
/// </summary>
public class MissionsTab : TabSelection, IScrollMenuAuthority
{
    [Header("Data Provider")]
    [SerializeField] private MonoBehaviour questDataProviderObject;
    private IQuestDataProvider questDataProvider;
    
    [Header("UI Components - Quest Type Selection")]
    [SerializeField] private Button mainQuestsButton;
    [SerializeField] private Button sideQuestsButton;
    [SerializeField] private TextMeshProUGUI mainQuestsButtonText;
    [SerializeField] private TextMeshProUGUI sideQuestsButtonText;
    [SerializeField] private Color selectedTypeColor = Color.yellow;
    [SerializeField] private Color unselectedTypeColor = Color.white;
    
    [Header("UI Components - Quest List")]
    [SerializeField] private ScrollMenu questScrollMenu;
    [SerializeField] private GameObject scrollMenuContainer;
    
    [Header("UI Components - Quest Details")]
    [SerializeField] private GameObject questDetailsPanel;
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private Slider questProgressBar;
    [SerializeField] private TextMeshProUGUI questProgressText;
    [SerializeField] private GameObject completedIndicator;
    
    [Header("UI Components - Character Model")]
    [SerializeField] private RenderTextureDisplay characterModelDisplay;
    
    private bool showingMainQuests = true;
    private bool isScrollMenuActive = false;
    private int currentQuestIndex = 0;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        if (questDataProviderObject != null)
            questDataProvider = questDataProviderObject as IQuestDataProvider;
    }
    
    #endregion
    
    #region TabSelection Overrides
    
    /// <summary>
    /// Called when this tab is selected.
    /// </summary>
    public override void OnTabSelect()
    {
        base.OnTabSelect();
        
        questDataProvider ??= questDataProviderObject as IQuestDataProvider;
        
        // Activate character model (behind quest details)
        if (characterModelDisplay != null)
            characterModelDisplay.Activate();
        
        // Initialize buttons
        InitializeButtons();
        
        // Show main quests by default
        ShowMainQuests();
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
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeButtons()
    {
        if (mainQuestsButton != null)
            mainQuestsButton.onClick.AddListener(ShowMainQuests);
        
        if (sideQuestsButton != null)
            sideQuestsButton.onClick.AddListener(ShowSideQuests);
    }
    
    #endregion
    
    #region Quest Type Selection
    
    /// <summary>
    /// Shows main quests.
    /// </summary>
    public void ShowMainQuests()
    {
        showingMainQuests = true;
        UpdateQuestTypeVisuals();
        UpdateQuestDisplay();
    }
    
    /// <summary>
    /// Shows side quests.
    /// </summary>
    public void ShowSideQuests()
    {
        showingMainQuests = false;
        UpdateQuestTypeVisuals();
        UpdateQuestDisplay();
    }
    
    private void UpdateQuestTypeVisuals()
    {
        if (mainQuestsButtonText != null)
        {
            mainQuestsButtonText.color = showingMainQuests
                ? selectedTypeColor
                : unselectedTypeColor;
        }
        
        if (sideQuestsButtonText != null)
        {
            sideQuestsButtonText.color = !showingMainQuests
                ? selectedTypeColor
                : unselectedTypeColor;
        }
    }
    
    #endregion
    
    #region Quest Display
    
    private void UpdateQuestDisplay()
    {
        DeactivateScrollMenu();
        ActivateScrollMenu();
    }
    
    private void ActivateScrollMenu()
    {
        if (questScrollMenu == null || questDataProvider == null)
            return;
        
        List<QuestDisplayData> quests = showingMainQuests
            ? questDataProvider.GetMainQuests()
            : questDataProvider.GetSideQuests();
        
        if (quests == null || quests.Count == 0)
        {
            Debug.LogWarning($"MissionsTab: No {(showingMainQuests ? "main" : "side")} quests available");
            ClearQuestDetails();
            return;
        }
        
        // Convert quests to ItemUIInfo for scroll menu
        List<ItemUIInfo<MissionEntryData>> questItems = new();
        foreach (var quest in quests)
        {
            questItems.Add(new ItemUIInfo<MissionEntryData>
            {
                itemName = quest.questName,
                itemDescription = quest.progressText,
                icon = null, // Could add quest icons if available
                rarity = quest.isMainQuest ? Rarity.Epic : Rarity.Common
            });
        }
        
        if (scrollMenuContainer != null)
            scrollMenuContainer.SetActive(true);
        
        questScrollMenu.Activate(questItems, 0, this);
        isScrollMenuActive = true;
        currentQuestIndex = 0;
        
        // Display first quest details
        UpdateQuestDetails(quests[0]);
    }
    
    private void DeactivateScrollMenu()
    {
        if (questScrollMenu != null)
            questScrollMenu.Deactivate();
        
        isScrollMenuActive = false;
    }
    
    /// <summary>
    /// Updates the quest details panel with the selected quest.
    /// </summary>
    /// <param name="questIndex">The index of the quest to display.</param>
    public void OnQuestSelected(int questIndex)
    {
        if (questDataProvider == null)
            return;
        
        currentQuestIndex = questIndex;
        QuestDisplayData quest = questDataProvider.GetQuest(questIndex, showingMainQuests);
        
        if (quest != null)
            UpdateQuestDetails(quest);
    }
    
    private void UpdateQuestDetails(QuestDisplayData quest)
    {
        if (quest == null)
        {
            ClearQuestDetails();
            return;
        }
        
        if (questDetailsPanel != null)
            questDetailsPanel.SetActive(true);
        
        if (questNameText != null)
            questNameText.text = quest.questName;
        
        if (questDescriptionText != null)
            questDescriptionText.text = quest.description;
        
        if (questProgressBar != null)
            questProgressBar.value = quest.progress;
        
        if (questProgressText != null)
            questProgressText.text = quest.progressText;
        
        if (completedIndicator != null)
            completedIndicator.SetActive(quest.isCompleted);
    }
    
    private void ClearQuestDetails()
    {
        if (questDetailsPanel != null)
            questDetailsPanel.SetActive(false);
        
        if (questNameText != null)
            questNameText.text = "";
        
        if (questDescriptionText != null)
            questDescriptionText.text = "No quests available";
        
        if (questProgressBar != null)
            questProgressBar.value = 0f;
        
        if (questProgressText != null)
            questProgressText.text = "";
        
        if (completedIndicator != null)
            completedIndicator.SetActive(false);
    }
    
    #endregion
    
    #region IScrollMenuAuthority Implementation
    
    private void ScrollDelegate(InputAction.CallbackContext context)
    {
        if (!isScrollMenuActive || questScrollMenu == null)
            return;
        
        Vector2 scrollInput = context.ReadValue<Vector2>();
        questScrollMenu.OnScrollPerformed(scrollInput);
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
    /// Sets the quest data provider for this tab.
    /// </summary>
    /// <param name="provider">The quest data provider.</param>
    public void SetDataProvider(IQuestDataProvider provider)
    {
        questDataProvider = provider;
        
        if (gameObject.activeInHierarchy)
            UpdateQuestDisplay();
    }
    
    #endregion
}

