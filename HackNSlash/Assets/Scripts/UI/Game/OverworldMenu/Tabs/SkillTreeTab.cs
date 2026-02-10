using System.Collections.Generic;
using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Tab 4: Skill Tree tab for viewing and unlocking skills/abilities.
/// Shows navigable skill tree with nodes, info panel at bottom, character model behind.
/// </summary>
public class SkillTreeTab : TabSelection
{
    [Header("Data Provider")]
    [SerializeField] private MonoBehaviour skillTreeDataProviderObject;
    private ISkillTreeDataProvider skillTreeDataProvider;
    
    [Header("UI Components - Skill Tree Display")]
    [SerializeField] private RectTransform skillTreeContainer;
    [SerializeField] private GameObject skillNodePrefab;
    [SerializeField] private float navigationSpeed = 500f;
    
    [Header("UI Components - Skill Info")]
    [SerializeField] private GameObject skillInfoPanel;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillDescriptionText;
    [SerializeField] private Image skillIcon;
    [SerializeField] private VideoDisplay skillVideoDisplay;
    [SerializeField] private Button unlockButton;
    [SerializeField] private TextMeshProUGUI unlockCostText;
    [SerializeField] private GameObject lockedIndicator;
    
    [Header("UI Components - Character Model")]
    [SerializeField] private RenderTextureDisplay characterModelDisplay;
    
    private List<SkillNodeUI> skillNodes = new List<SkillNodeUI>();
    private int currentSelectedNodeIndex = 0;
    private SkillNodeDisplayData[] allNodesData;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        if (skillTreeDataProviderObject != null)
            skillTreeDataProvider = skillTreeDataProviderObject as ISkillTreeDataProvider;
    }
    
    private void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;
        
        HandleNavigation();
    }
    
    #endregion
    
    #region TabSelection Overrides
    
    /// <summary>
    /// Called when this tab is selected.
    /// </summary>
    public override void OnTabSelect()
    {
        base.OnTabSelect();
        
        skillTreeDataProvider ??= skillTreeDataProviderObject as ISkillTreeDataProvider;
        
        // Activate character model (behind skill tree)
        if (characterModelDisplay != null)
            characterModelDisplay.Activate();
        
        // Build skill tree
        BuildSkillTree();
        
        // Select first node
        if (skillNodes.Count > 0)
            SelectNode(0);
    }
    
    /// <summary>
    /// Called when this tab is deselected.
    /// </summary>
    public override void OnTabDeselect()
    {
        base.OnTabDeselect();
        
        if (characterModelDisplay != null)
            characterModelDisplay.Deactivate();
        
        if (skillVideoDisplay != null)
            skillVideoDisplay.Deactivate();
    }
    
    #endregion
    
    #region Skill Tree Building
    
    private void BuildSkillTree()
    {
        // Clear existing nodes
        ClearSkillTree();
        
        // Get all node data
        allNodesData = skillTreeDataProvider.GetAllNodes();
        
        if (allNodesData == null || allNodesData.Length == 0)
        {
            Debug.LogWarning("SkillTreeTab: No skill nodes available!");
            return;
        }
        
        // Create UI nodes
        for (int i = 0; i < allNodesData.Length; i++)
        {
            CreateSkillNode(allNodesData[i], i);
        }
        
        // Draw connections between nodes
        DrawNodeConnections();
    }
    
    private void CreateSkillNode(SkillNodeDisplayData nodeData, int index)
    {
        if (skillNodePrefab == null || skillTreeContainer == null)
            return;
        
        GameObject nodeObj = Instantiate(skillNodePrefab, skillTreeContainer);
        SkillNodeUI nodeUI = nodeObj.GetComponent<SkillNodeUI>();
        
        if (nodeUI != null)
        {
            nodeUI.Initialize(nodeData, index);
            nodeUI.onNodeClicked += OnNodeClicked;
            skillNodes.Add(nodeUI);
            
            // Position the node
            RectTransform nodeRect = nodeObj.GetComponent<RectTransform>();
            if (nodeRect != null)
            {
                nodeRect.anchoredPosition = nodeData.treePosition;
            }
        }
    }
    
    private void DrawNodeConnections()
    {
        // TODO: Draw lines between connected nodes
        // This would require a line drawing system or UI line renderers
    }
    
    private void ClearSkillTree()
    {
        foreach (var node in skillNodes)
        {
            if (node != null)
            {
                node.onNodeClicked -= OnNodeClicked;
                Destroy(node.gameObject);
            }
        }
        
        skillNodes.Clear();
    }
    
    #endregion
    
    #region Navigation
    
    private void HandleNavigation()
    {
        // Navigate with WASD or arrow keys
        Vector2 input = Vector2.zero;
        
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            input.y = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            input.y = -1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            input.x = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            input.x = 1f;
        
        if (input != Vector2.zero)
        {
            // Move the skill tree container to keep selected node centered
            Vector2 movement = input * navigationSpeed * Time.deltaTime;
            skillTreeContainer.anchoredPosition += movement;
        }
    }
    
    private void OnNodeClicked(int nodeIndex)
    {
        SelectNode(nodeIndex);
    }
    
    private void SelectNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= skillNodes.Count)
            return;
        
        currentSelectedNodeIndex = nodeIndex;
        
        // Update visual selection
        for (int i = 0; i < skillNodes.Count; i++)
        {
            if (skillNodes[i] != null)
                skillNodes[i].SetSelected(i == nodeIndex);
        }
        
        // Center the selected node
        CenterNode(nodeIndex);
        
        // Update info panel
        UpdateSkillInfo(allNodesData[nodeIndex]);
    }
    
    private void CenterNode(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= skillNodes.Count || skillNodes[nodeIndex] == null)
            return;
        
        RectTransform nodeRect = skillNodes[nodeIndex].GetComponent<RectTransform>();
        if (nodeRect != null)
        {
            // Calculate position to center this node
            Vector2 targetPosition = -nodeRect.anchoredPosition;
            skillTreeContainer.anchoredPosition = targetPosition;
        }
    }
    
    #endregion
    
    #region Skill Info Display
    
    private void UpdateSkillInfo(SkillNodeDisplayData nodeData)
    {
        if (skillInfoPanel != null)
            skillInfoPanel.SetActive(true);
        
        if (skillNameText != null)
            skillNameText.text = nodeData.nodeName;
        
        if (skillDescriptionText != null)
            skillDescriptionText.text = nodeData.description;
        
        if (skillIcon != null)
        {
            skillIcon.sprite = nodeData.icon;
            skillIcon.enabled = nodeData.icon != null;
        }
        
        // Update unlock button
        if (unlockButton != null)
        {
            unlockButton.gameObject.SetActive(!nodeData.isUnlocked);
            unlockButton.interactable = nodeData.canUnlock;
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(() => OnUnlockButtonClicked(currentSelectedNodeIndex));
        }
        
        if (unlockCostText != null)
            unlockCostText.text = $"Cost: {nodeData.unlockCost}";
        
        if (lockedIndicator != null)
            lockedIndicator.SetActive(!nodeData.isUnlocked);
        
        // Show video if available
        if (skillVideoDisplay != null && nodeData.demonstrationVideo != null)
        {
            skillVideoDisplay.SetVideo(nodeData.demonstrationVideo, true);
            skillVideoDisplay.Activate();
        }
        else if (skillVideoDisplay != null)
        {
            skillVideoDisplay.Deactivate();
        }
    }
    
    private void OnUnlockButtonClicked(int nodeIndex)
    {
        if (skillTreeDataProvider == null)
            return;
        
        bool success = skillTreeDataProvider.UnlockNode(nodeIndex);
        
        if (success)
        {
            // Refresh the skill tree
            allNodesData = skillTreeDataProvider.GetAllNodes();
            
            // Update the unlocked node's visual
            if (nodeIndex >= 0 && nodeIndex < skillNodes.Count && skillNodes[nodeIndex] != null)
            {
                skillNodes[nodeIndex].Initialize(allNodesData[nodeIndex], nodeIndex);
            }
            
            // Update info panel
            UpdateSkillInfo(allNodesData[nodeIndex]);
            
            Debug.Log($"Successfully unlocked skill node {nodeIndex}!");
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Sets the skill tree data provider for this tab.
    /// </summary>
    /// <param name="provider">The skill tree data provider.</param>
    public void SetDataProvider(ISkillTreeDataProvider provider)
    {
        skillTreeDataProvider = provider;
        
        if (gameObject.activeInHierarchy)
            BuildSkillTree();
    }
    
    #endregion
}

/// <summary>
/// UI component for a single skill node in the skill tree.
/// Controller-only navigation - no mouse support.
/// </summary>
public class SkillNodeUI : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [Header("UI References")]
    [SerializeField] private Image nodeIcon;
    [SerializeField] private Image nodeBackground;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject selectedIndicator;
    
    [Header("Colors")]
    [SerializeField] private Color unlockedColor = Color.green;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color canUnlockColor = Color.yellow;
    
    private SkillNodeDisplayData nodeData;
    private int nodeIndex;
    
    public System.Action<int> onNodeClicked;
    public System.Action<int> onNodeSelected;
    
    /// <summary>
    /// Initializes the node with data.
    /// </summary>
    public void Initialize(SkillNodeDisplayData data, int index)
    {
        nodeData = data;
        nodeIndex = index;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Sets whether this node is selected.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);
    }
    
    private void UpdateVisuals()
    {
        if (nodeIcon != null && nodeData.icon != null)
        {
            nodeIcon.sprite = nodeData.icon;
            nodeIcon.enabled = true;
        }
        else if (nodeIcon != null)
        {
            nodeIcon.enabled = false;
        }
        
        Color bgColor = lockedColor;
        if (nodeData.isUnlocked)
            bgColor = unlockedColor;
        else if (nodeData.canUnlock)
            bgColor = canUnlockColor;
        
        if (nodeBackground != null)
            nodeBackground.color = bgColor;
        
        if (lockedOverlay != null)
            lockedOverlay.SetActive(!nodeData.isUnlocked);
    }
    
    public void OnSelect(UnityEngine.EventSystems.BaseEventData eventData)
    {
        SetSelected(true);
        onNodeSelected?.Invoke(nodeIndex);
        UIAudio.PlayHover();
    }
    
    public void OnDeselect(UnityEngine.EventSystems.BaseEventData eventData)
    {
        SetSelected(false);
    }
    
    public void OnSubmit(UnityEngine.EventSystems.BaseEventData eventData)
    {
        onNodeClicked?.Invoke(nodeIndex);
        UIAudio.PlaySelect();
    }
}

