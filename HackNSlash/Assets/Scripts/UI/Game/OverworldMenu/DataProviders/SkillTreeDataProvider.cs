using System.Collections.Generic;
using UnityEngine;
using Extensions.UI;

/// <summary>
/// Data provider for the Skill Tree UI.
/// Interfaces with PlayerSkillTreeData to provide skill tree information.
/// </summary>
public class SkillTreeDataProvider : MonoBehaviour, ISkillTreeDataProvider
{
    [Header("Data Source")]
    [SerializeField] private PlayerController playerController;
    
    private RuntimePlayerStatus runtimePlayerStatus;
    private PlayerSkillTreeData skillTreeData;
    
    private void Awake()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
        
        if (playerController != null)
        {
            runtimePlayerStatus = playerController.rps;
            
            if (runtimePlayerStatus != null)
            {
                skillTreeData = runtimePlayerStatus.skillTreeData;
            }
        }
        
        if (skillTreeData == null)
        {
            Debug.LogWarning("SkillTreeDataProvider: Could not find PlayerSkillTreeData!");
        }
    }
    
    /// <summary>
    /// Gets all nodes in the skill tree.
    /// </summary>
    public List<SkillNodeDisplayData> GetAllNodes()
    {
        if (skillTreeData == null || skillTreeData.skillTree == null)
            return new List<SkillNodeDisplayData>();
        
        List<SkillNodeDisplayData> displayNodes = new List<SkillNodeDisplayData>();
        
        foreach (var node in skillTreeData.skillTree.nodes)
        {
            displayNodes.Add(ConvertToDisplayData(node));
        }
        
        return displayNodes;
    }
    
    /// <summary>
    /// Gets a specific node by ID.
    /// </summary>
    public SkillNodeDisplayData GetNode(string nodeId)
    {
        if (skillTreeData == null || skillTreeData.skillTree == null)
            return null;
        
        var node = skillTreeData.skillTree.GetNodeById(nodeId);
        return node != null ? ConvertToDisplayData(node) : null;
    }
    
    /// <summary>
    /// Gets the start node.
    /// </summary>
    public SkillNodeDisplayData GetStartNode()
    {
        if (skillTreeData == null || skillTreeData.skillTree == null)
            return null;
        
        var startNode = skillTreeData.skillTree.GetStartNode();
        return startNode != null ? ConvertToDisplayData(startNode) : null;
    }
    
    /// <summary>
    /// Gets the nearest node in a given direction.
    /// </summary>
    public SkillNodeDisplayData GetNearestNodeInDirection(string currentNodeId, Vector2 direction)
    {
        if (skillTreeData == null || skillTreeData.skillTree == null)
            return null;
        
        var currentNode = skillTreeData.skillTree.GetNodeById(currentNodeId);
        if (currentNode == null)
            return null;
        
        var nearestNode = skillTreeData.skillTree.GetNearestNodeInDirection(currentNode, direction);
        return nearestNode != null ? ConvertToDisplayData(nearestNode) : null;
    }
    
    /// <summary>
    /// Checks if a node is unlocked.
    /// </summary>
    public bool IsNodeUnlocked(string nodeId)
    {
        if (skillTreeData == null)
            return false;
        
        return skillTreeData.IsNodeUnlocked(nodeId);
    }
    
    /// <summary>
    /// Checks if a node is activated.
    /// </summary>
    public bool IsNodeActivated(string nodeId)
    {
        if (skillTreeData == null)
            return false;
        
        return skillTreeData.IsNodeActivated(nodeId);
    }
    
    /// <summary>
    /// Attempts to unlock a node.
    /// </summary>
    public bool UnlockNode(string nodeId)
    {
        if (skillTreeData == null || skillTreeData.skillTree == null)
            return false;
        
        var node = skillTreeData.skillTree.GetNodeById(nodeId);
        if (node == null)
            return false;
        
        return skillTreeData.UnlockNode(node);
    }
    
    /// <summary>
    /// Activates a node (toggle on).
    /// </summary>
    public bool ActivateNode(string nodeId)
    {
        if (skillTreeData == null)
            return false;
        
        return skillTreeData.ActivateNode(nodeId);
    }
    
    /// <summary>
    /// Deactivates a node (toggle off).
    /// </summary>
    public bool DeactivateNode(string nodeId)
    {
        if (skillTreeData == null)
            return false;
        
        return skillTreeData.DeactivateNode(nodeId);
    }
    
    /// <summary>
    /// Gets available skill points.
    /// </summary>
    public int GetAvailableSkillPoints()
    {
        if (skillTreeData == null)
            return 0;
        
        return skillTreeData.availableSkillPoints;
    }
    
    /// <summary>
    /// Converts a SkillTreeNode to display data.
    /// </summary>
    private SkillNodeDisplayData ConvertToDisplayData(SkillTreeNode node)
    {
        return new SkillNodeDisplayData
        {
            nodeId = node.nodeId,
            nodeName = node.nodeName,
            description = node.description,
            icon = node.icon,
            demonstrationVideo = node.demonstrationVideo,
            cost = node.cost,
            uiPosition = node.uiPosition,
            isUnlocked = skillTreeData != null && skillTreeData.IsNodeUnlocked(node.nodeId),
            isActivated = skillTreeData != null && skillTreeData.IsNodeActivated(node.nodeId),
            canUnlock = CanUnlockNode(node)
        };
    }
    
    /// <summary>
    /// Checks if a node can be unlocked (requirements met, enough points).
    /// </summary>
    private bool CanUnlockNode(SkillTreeNode node)
    {
        if (skillTreeData == null)
            return false;
        
        // Already unlocked
        if (skillTreeData.IsNodeUnlocked(node.nodeId))
            return false;
        
        // Not enough points
        if (skillTreeData.availableSkillPoints < node.cost)
            return false;
        
        // Check requirements
        return node.AreRequirementsMet(skillTreeData.GetUnlockedNodes());
    }
}

