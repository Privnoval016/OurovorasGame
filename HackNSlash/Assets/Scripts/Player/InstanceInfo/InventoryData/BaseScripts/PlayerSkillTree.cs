using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ScriptableObject that defines the entire skill tree structure.
/// Edit via the custom Skill Tree Editor window.
/// </summary>
[CreateAssetMenu(fileName = "PlayerSkillTree", menuName = "Inventory/SkillTree", order = 1)]
public class PlayerSkillTree : ScriptableObject
{
    [Header("Skill Tree Info")]
    public string treeName;
    
    [Header("Skill Tree Structure")]
    [Tooltip("The starting node where the player begins")]
    public string startNodeId;
    
    [Tooltip("All nodes in this skill tree")]
    public List<SkillTreeNode> nodes = new List<SkillTreeNode>();
    
    /// <summary>
    /// Gets a node by its unique ID.
    /// </summary>
    public SkillTreeNode GetNodeById(string nodeId)
    {
        return nodes.FirstOrDefault(n => n.nodeId == nodeId);
    }
    
    /// <summary>
    /// Gets the start node of the skill tree.
    /// </summary>
    public SkillTreeNode GetStartNode()
    {
        return GetNodeById(startNodeId);
    }
    
    /// <summary>
    /// Gets all child nodes of a given node.
    /// </summary>
    public List<SkillTreeNode> GetChildNodes(string nodeId)
    {
        List<SkillTreeNode> children = new List<SkillTreeNode>();
        
        foreach (var node in nodes)
        {
            if (node.parentNodeIds.Contains(nodeId))
            {
                children.Add(node);
            }
        }
        
        return children;
    }
    
    /// <summary>
    /// Gets the nearest node in a given direction from the current node.
    /// Only considers connected nodes (parents and children) for one-step navigation.
    /// </summary>
    public SkillTreeNode GetNearestNodeInDirection(SkillTreeNode currentNode, Vector2 direction)
    {
        if (currentNode == null || direction == Vector2.zero)
            return null;
        
        // Get all connected nodes (parents + children)
        List<SkillTreeNode> connectedNodes = new List<SkillTreeNode>();
        
        // Add parents
        connectedNodes.AddRange(currentNode.GetParentNodes(this));
        
        // Add children
        connectedNodes.AddRange(GetChildNodes(currentNode.nodeId));
        
        if (connectedNodes.Count == 0)
            return null;
        
        SkillTreeNode nearestNode = null;
        float shortestDistance = float.MaxValue;
        float bestAlignment = -1f;
        
        foreach (var node in connectedNodes)
        {
            Vector2 toNode = node.uiPosition - currentNode.uiPosition;
            float distance = toNode.magnitude;
            
            // Check if node is generally in the desired direction
            float alignment = Vector2.Dot(toNode.normalized, direction.normalized);
            
            // Must be at least somewhat aligned with the direction (> 0.3 means within ~70 degrees)
            if (alignment > 0.3f)
            {
                // Prefer better alignment, then shorter distance
                if (alignment > bestAlignment || (Mathf.Approximately(alignment, bestAlignment) && distance < shortestDistance))
                {
                    nearestNode = node;
                    shortestDistance = distance;
                    bestAlignment = alignment;
                }
            }
        }
        
        return nearestNode;
    }
}

/// <summary>
/// Runtime data for the player's progress in the skill tree.
/// Separates unlocked nodes (permanently available) from activated nodes (currently equipped).
/// </summary>
[Serializable]
public class PlayerSkillTreeData
{
    [Tooltip("Reference to the skill tree definition")]
    public PlayerSkillTree skillTree;
    
    [Tooltip("Nodes that have been permanently unlocked (paid the cost)")]
    public List<string> unlockedNodeIds = new List<string>();
    
    [Tooltip("Nodes that are currently activated/equipped")]
    public List<string> activatedNodeIds = new List<string>();
    
    [Tooltip("Available skill points to spend on unlocking nodes")]
    public int availableSkillPoints = 10; // Starting amount

    public Action<SkillTreeNode, bool> onTreeUpdated;
    
    /// <summary>
    /// Gets all unlocked nodes.
    /// </summary>
    public List<SkillTreeNode> GetUnlockedNodes()
    {
        if (skillTree == null) return new List<SkillTreeNode>();
        
        List<SkillTreeNode> nodes = new List<SkillTreeNode>();
        foreach (string nodeId in unlockedNodeIds)
        {
            var node = skillTree.GetNodeById(nodeId);
            if (node != null)
                nodes.Add(node);
        }
        return nodes;
    }
    
    /// <summary>
    /// Gets all activated nodes.
    /// </summary>
    public List<SkillTreeNode> GetActivatedNodes()
    {
        if (skillTree == null) return new List<SkillTreeNode>();
        
        List<SkillTreeNode> nodes = new List<SkillTreeNode>();
        foreach (string nodeId in activatedNodeIds)
        {
            var node = skillTree.GetNodeById(nodeId);
            if (node != null)
                nodes.Add(node);
        }
        return nodes;
    }
    
    public List<SkillTreeNode> GetAllDeactivatedNodes()
    {
        if (skillTree == null) return new List<SkillTreeNode>();
        
        List<SkillTreeNode> nodes = new List<SkillTreeNode>();
        foreach (string nodeId in skillTree.nodes.Select(n => n.nodeId))
        {
            if (!activatedNodeIds.Contains(nodeId))
            {
                var node = skillTree.GetNodeById(nodeId);
                if (node != null)
                    nodes.Add(node);
            }
        }
        return nodes;
    }
    
    /// <summary>
    /// Checks if a node is unlocked.
    /// </summary>
    public bool IsNodeUnlocked(string nodeId)
    {
        return unlockedNodeIds.Contains(nodeId);
    }
    
    /// <summary>
    /// Checks if a node is activated.
    /// </summary>
    public bool IsNodeActivated(string nodeId)
    {
        return activatedNodeIds.Contains(nodeId);
    }
    
    /// <summary>
    /// Attempts to unlock a node (spend skill points).
    /// </summary>
    public bool UnlockNode(SkillTreeNode node)
    {
        if (node == null || IsNodeUnlocked(node.nodeId))
            return false;
        
        // Check if we have enough skill points
        if (availableSkillPoints < node.cost)
            return false;
        
        // Check if requirements are met
        if (!node.AreRequirementsMet(GetUnlockedNodes()))
            return false;
        
        // Unlock the node
        availableSkillPoints -= node.cost;
        unlockedNodeIds.Add(node.nodeId);
        
        onTreeUpdated?.Invoke(node, false);
        
        return true;
    }
    
    /// <summary>
    /// Activates an unlocked node (no cost, just toggling).
    /// </summary>
    public bool ActivateNode(string nodeId)
    {
        if (!IsNodeUnlocked(nodeId) || IsNodeActivated(nodeId))
            return false;
        
        activatedNodeIds.Add(nodeId);
        
        var node = skillTree.GetNodeById(nodeId);
        
        onTreeUpdated?.Invoke(node, true);
        
        return true;
    }
    
    /// <summary>
    /// Deactivates a node (toggle off).
    /// </summary>
    public bool DeactivateNode(string nodeId)
    {
        if (!IsNodeActivated(nodeId))
            return false;
        
        activatedNodeIds.Remove(nodeId);
        
        var node = skillTree.GetNodeById(nodeId);
        
        onTreeUpdated?.Invoke(node, false);
        
        return true;
    }
    
    /// <summary>
    /// Gets all locked nodes.
    /// </summary>
    public List<SkillTreeNode> GetLockedNodes()
    {
        if (skillTree == null) return new List<SkillTreeNode>();
        
        List<SkillTreeNode> lockedNodes = new List<SkillTreeNode>();
        
        foreach (SkillTreeNode node in skillTree.nodes)
        {
            if (!IsNodeUnlocked(node.nodeId))
            {
                lockedNodes.Add(node);
            }
        }

        return lockedNodes;
    }
}



