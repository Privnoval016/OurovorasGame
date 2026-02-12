using System.Collections.Generic;
using Extensions.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Tab 4: Skill Tree visualization and navigation.
/// Allows the player to navigate the skill tree, unlock nodes, and activate/deactivate abilities.
/// </summary>
public class SkillTreeTab : TabSelection
{
    [Header("Data Provider")]
    [SerializeField] private SkillTreeDataProvider skillTreeDataProvider;
    
    [Header("UI References - Tree View")]
    [SerializeField] private RectTransform treeContainer; // Container that moves to keep focused node centered
    [SerializeField] private RectTransform nodeContainer; // Parent for all node UI elements
    [SerializeField] private RectTransform maskObject; // Mask for the tree view area
    [SerializeField] private SkillTreeNodeUI nodeUIPrefab; // Prefab for node visuals
    
    [Header("UI References - Info Display")]
    [SerializeField] private Image nodeIcon;
    [SerializeField] private TextMeshProUGUI nodeNameText;
    [SerializeField] private TextMeshProUGUI nodeDescriptionText;
    [SerializeField] private TextMeshProUGUI nodeCostText;
    [SerializeField] private TextMeshProUGUI skillPointsText;
    [SerializeField] private GameObject infoPanel; // Bottom panel with node info
    
    [Header("UI References - Video Player")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject videoPlayerOverlay;
    
    [Header("UI References - Background")]
    [SerializeField] private RenderTextureDisplay characterModelDisplay;
    
    [Header("Animation Settings")]
    [SerializeField] private float focusAnimationDuration = 0.3f;
    [SerializeField] private Ease focusAnimationEase = Ease.OutCubic;
    [SerializeField] private float nodeScaleNormal = 1f;
    [SerializeField] private float nodeScaleFocused = 1.2f;
    
    private Dictionary<string, SkillTreeNodeUI> nodeUIElements = new Dictionary<string, SkillTreeNodeUI>();
    private SkillNodeDisplayData currentFocusedNode;
    private bool isHoldingToUnlock = false;
    private bool isHoldingToDeactivate = false;
    private float holdTimer = 0f;
    private const float HOLD_DURATION = 0.5f; // How long to hold to unlock/deactivate
    
    #region Initialization
    
    private void Awake()
    {
        if (skillTreeDataProvider == null)
        {
            Debug.LogWarning("SkillTreeTab: skillTreeDataProvider is not assigned!");
        }
        
        // Subscribe to input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.onNavigate += OnNavigateInput;
            InputManager.Instance.onSelect += OnSelectInput;
            InputManager.Instance.onBack += OnBackInput;
        }
    }
    
    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.onNavigate -= OnNavigateInput;
            InputManager.Instance.onSelect -= OnSelectInput;
            InputManager.Instance.onBack -= OnBackInput;
        }
    }
    
    public override void OnTabSelect()
    {
        base.OnTabSelect();
        
        BuildSkillTree();
        FocusOnStartNode();
        UpdateSkillPointsDisplay();
    }
    
    public new void OnTabDeselect()
    {
        // Reset hold states
        isHoldingToUnlock = false;
        isHoldingToDeactivate = false;
        holdTimer = 0f;
    }
    
    #endregion
    
    #region Skill Tree Building
    
    /// <summary>
    /// Builds the skill tree UI from the data provider.
    /// </summary>
    private void BuildSkillTree()
    {
        if (skillTreeDataProvider == null)
        {
            Debug.LogWarning("SkillTreeTab: Cannot build skill tree - no data provider!");
            return;
        }
        
        // Clear existing nodes
        foreach (var kvp in nodeUIElements)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        nodeUIElements.Clear();
        
        // Get all nodes from data provider
        var allNodes = skillTreeDataProvider.GetAllNodes();
        
        if (allNodes == null || allNodes.Count == 0)
        {
            Debug.LogWarning("SkillTreeTab: No nodes found in skill tree!");
            return;
        }
        
        // Create UI element for each node
        foreach (var nodeData in allNodes)
        {
            CreateNodeUI(nodeData);
        }
        
        // Draw connections between nodes
        DrawConnections();
    }
    
    /// <summary>
    /// Creates a UI element for a skill tree node.
    /// </summary>
    private void CreateNodeUI(SkillNodeDisplayData nodeData)
    {
        if (nodeUIPrefab == null || nodeContainer == null)
            return;
        
        SkillTreeNodeUI nodeUI = Instantiate(nodeUIPrefab, nodeContainer);
        
        // Set position based on uiPosition (0-1 relative space)
        RectTransform rectTransform = nodeUI.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Convert 0-1 space to anchored position
            Vector2 containerSize = nodeContainer.rect.size;
            Vector2 anchoredPos = new Vector2(
                nodeData.uiPosition.x * containerSize.x - containerSize.x / 2,
                nodeData.uiPosition.y * containerSize.y - containerSize.y / 2
            );
            
            rectTransform.anchoredPosition = anchoredPos;
        }
        
        nodeUI.transform.SetParent(maskObject, false); // Ensure it's under the mask for proper clipping
        
        // Initialize the node UI
        nodeUI.Initialize(nodeData);
        
        // Store reference
        nodeUIElements[nodeData.nodeId] = nodeUI;
    }
    
    /// <summary>
    /// Draws connection lines between parent and child nodes.
    /// </summary>
    private void DrawConnections()
    {
        // TODO: Implement connection line drawing
        // This would create Line Renderers or UI Lines between connected nodes
    }
    
    #endregion
    
    #region Navigation
    
    /// <summary>
    /// Focuses on the start node when entering the tab.
    /// </summary>
    private void FocusOnStartNode()
    {
        if (skillTreeDataProvider == null)
            return;
        
        var startNode = skillTreeDataProvider.GetStartNode();
        if (startNode != null)
        {
            FocusOnNode(startNode, false); // No animation on first focus
        }
    }
    
    /// <summary>
    /// Focuses the camera/view on a specific node (animated).
    /// </summary>
    private void FocusOnNode(SkillNodeDisplayData nodeData, bool animate = true)
    {
        if (nodeData == null || treeContainer == null)
            return;
        
        currentFocusedNode = nodeData;
        
        // Get the node UI element
        if (!nodeUIElements.TryGetValue(nodeData.nodeId, out SkillTreeNodeUI nodeUI))
            return;
        
        // Calculate the position to center this node
        Vector2 nodeWorldPos = nodeUI.GetComponent<RectTransform>().anchoredPosition;
        Vector2 targetPosition = -nodeWorldPos; // Negate to center
        
        // Animate the tree container to center the node
        if (animate)
        {
            Tween.Custom(treeContainer.anchoredPosition, targetPosition, focusAnimationDuration,
                onValueChange: pos => treeContainer.anchoredPosition = pos,
                ease: focusAnimationEase,
                useUnscaledTime: true);
            
            // Scale animation for focused node
            foreach (var kvp in nodeUIElements)
            {
                float targetScale = kvp.Key == nodeData.nodeId ? nodeScaleFocused : nodeScaleNormal;
                Tween.Scale(kvp.Value.transform, targetScale, focusAnimationDuration, 
                    ease: focusAnimationEase, useUnscaledTime: true);
            }
        }
        else
        {
            treeContainer.anchoredPosition = targetPosition;
            
            foreach (var kvp in nodeUIElements)
            {
                float targetScale = kvp.Key == nodeData.nodeId ? nodeScaleFocused : nodeScaleNormal;
                kvp.Value.transform.localScale = Vector3.one * targetScale;
            }
        }
        
        // Update info display
        UpdateNodeInfoDisplay(nodeData);
        
        // Update video player
        UpdateVideoPlayer(nodeData);
    }
    
    private void OnNavigateInput(InputAction.CallbackContext context)
    {
        if (!context.performed || currentFocusedNode == null)
            return;
        
        Vector2 navInput = context.ReadValue<Vector2>();
        
        if (navInput.magnitude < 0.1f)
            return;
        
        // Get nearest node in the navigation direction
        var nearestNode = skillTreeDataProvider.GetNearestNodeInDirection(currentFocusedNode.nodeId, navInput);
        
        if (nearestNode != null)
        {
            FocusOnNode(nearestNode, true);
            UIAudio.PlayHover();
        }
    }
    
    #endregion
    
    #region Node Interaction
    
    private void OnSelectInput(InputAction.CallbackContext context)
    {
        if (currentFocusedNode == null || skillTreeDataProvider == null)
            return;
        
        if (context.started)
        {
            // Start holding to unlock or activate
            if (!currentFocusedNode.isUnlocked && currentFocusedNode.canUnlock)
            {
                isHoldingToUnlock = true;
                isHoldingToDeactivate = false;
                holdTimer = 0f;
                UpdateNodeHoldProgress(currentFocusedNode.nodeId, 0f, false);
            }
            else if (currentFocusedNode.isUnlocked && !currentFocusedNode.isActivated)
            {
                // Can activate already unlocked node
                isHoldingToUnlock = true;
                isHoldingToDeactivate = false;
                holdTimer = 0f;
                UpdateNodeHoldProgress(currentFocusedNode.nodeId, 0f, false);
            }
        }
        else if (context.canceled)
        {
            // Released before full hold
            isHoldingToUnlock = false;
            holdTimer = 0f;
            UpdateNodeHoldProgress(currentFocusedNode.nodeId, 0f, false);
        }
    }
    
    private void OnBackInput(InputAction.CallbackContext context)
    {
        if (currentFocusedNode == null || skillTreeDataProvider == null)
            return;
        
        if (context.started)
        {
            // Start holding to deactivate
            if (currentFocusedNode.isActivated)
            {
                isHoldingToDeactivate = true;
                isHoldingToUnlock = false;
                holdTimer = 0f;
                UpdateNodeHoldProgress(currentFocusedNode.nodeId, 0f, false);
            }
        }
        else if (context.canceled)
        {
            // Released before full hold
            isHoldingToDeactivate = false;
            holdTimer = 0f;
            UpdateNodeHoldProgress(currentFocusedNode.nodeId, 0f, false);
        }
    }
    
    private void Update()
    {
        HandleHoldInput();
    }
    
    private void HandleHoldInput()
    {
        if (currentFocusedNode == null)
        {
            // Safety: Clear hold states if no focused node
            if (isHoldingToUnlock || isHoldingToDeactivate)
            {
                isHoldingToUnlock = false;
                isHoldingToDeactivate = false;
                holdTimer = 0f;
            }
            return;
        }
        
        if (isHoldingToUnlock || isHoldingToDeactivate)
        {
            holdTimer += Time.unscaledDeltaTime;
            
            // Update progress visual on the focused node
            float progress = Mathf.Clamp01(holdTimer / HOLD_DURATION);
            UpdateNodeHoldProgress(currentFocusedNode.nodeId, progress, false);
            
            if (holdTimer >= HOLD_DURATION)
            {
                if (isHoldingToUnlock)
                {
                    AttemptUnlockNode();
                }
                else if (isHoldingToDeactivate)
                {
                    AttemptDeactivateNode();
                }
                
                // Reset hold state
                isHoldingToUnlock = false;
                isHoldingToDeactivate = false;
                holdTimer = 0f;
                UpdateNodeHoldProgress(currentFocusedNode.nodeId, 0f, true);
            }
        }
    }
    
    /// <summary>
    /// Updates the hold progress ring on a specific node.
    /// </summary>
    private void UpdateNodeHoldProgress(string nodeId, float progress, bool finished)
    {
        if (nodeUIElements.TryGetValue(nodeId, out SkillTreeNodeUI nodeUI))
        {
            nodeUI.SetHoldProgress(progress, finished);
        }
    }
    
    private void AttemptUnlockNode()
    {
        if (currentFocusedNode == null || skillTreeDataProvider == null)
        {
            Debug.LogWarning("SkillTreeTab: Cannot unlock node - null reference");
            return;
        }
        
        bool success = false;
        
        // If already unlocked, try to activate instead
        if (currentFocusedNode.isUnlocked)
        {
            success = skillTreeDataProvider.ActivateNode(currentFocusedNode.nodeId);
        }
        else
        {
            success = skillTreeDataProvider.UnlockNode(currentFocusedNode.nodeId);
        }
        
        if (success)
        {
            UIAudio.PlayItemEquip();
            
            // Refresh the node display
            RefreshCurrentNode();
        }
        else
        {
            UIAudio.PlayError();
        }
    }
    
    private void AttemptDeactivateNode()
    {
        if (currentFocusedNode == null || skillTreeDataProvider == null)
        {
            Debug.LogWarning("SkillTreeTab: Cannot deactivate node - null reference");
            return;
        }
        
        bool success = skillTreeDataProvider.DeactivateNode(currentFocusedNode.nodeId);
        
        if (success)
        {
            UIAudio.PlayBack();
            
            // Refresh the node display
            RefreshCurrentNode();
        }
        else
        {
            UIAudio.PlayError();
        }
    }
    
    /// <summary>
    /// Refreshes the currently focused node's data and UI.
    /// </summary>
    private void RefreshCurrentNode()
    {
        if (currentFocusedNode == null || skillTreeDataProvider == null)
            return;
        
        var updatedNode = skillTreeDataProvider.GetNode(currentFocusedNode.nodeId);
        if (updatedNode != null)
        {
            currentFocusedNode = updatedNode;
            
            // Update the node UI
            if (nodeUIElements.TryGetValue(currentFocusedNode.nodeId, out SkillTreeNodeUI nodeUI))
            {
                nodeUI.UpdateState(updatedNode);
            }
            
            UpdateNodeInfoDisplay(updatedNode);
            UpdateSkillPointsDisplay();
        }
    }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateNodeInfoDisplay(SkillNodeDisplayData nodeData)
    {
        if (nodeData == null)
            return;
        
        if (nodeIcon != null)
        {
            nodeIcon.sprite = nodeData.icon;
            nodeIcon.enabled = nodeData.icon != null;
        }
        
        if (nodeNameText != null)
            nodeNameText.text = nodeData.nodeName;
        
        if (nodeDescriptionText != null)
            nodeDescriptionText.text = nodeData.description;
        
        if (nodeCostText != null)
        {
            if (nodeData.isUnlocked)
            {
                nodeCostText.text = nodeData.isActivated ? "ACTIVATED" : "UNLOCKED";
            }
            else
            {
                nodeCostText.text = $"Cost: {nodeData.cost} SP";
            }
        }
    }
    
    private void UpdateVideoPlayer(SkillNodeDisplayData nodeData)
    {
        if (videoPlayer == null || videoPlayerOverlay == null)
            return;
        
        if (nodeData.demonstrationVideo != null)
        {
            videoPlayer.clip = nodeData.demonstrationVideo;
            videoPlayer.Play();
            videoPlayerOverlay.SetActive(true);
        }
        else
        {
            videoPlayer.Stop();
            videoPlayerOverlay.SetActive(false);
        }
    }
    
    private void UpdateSkillPointsDisplay()
    {
        if (skillPointsText != null && skillTreeDataProvider != null)
        {
            int availablePoints = skillTreeDataProvider.GetAvailableSkillPoints();
            skillPointsText.text = $"Skill Points: {availablePoints}";
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Sets the skill tree data provider for this tab.
    /// </summary>
    public void SetDataProvider(SkillTreeDataProvider provider)
    {
        skillTreeDataProvider = provider;
    }
    
    #endregion
}

