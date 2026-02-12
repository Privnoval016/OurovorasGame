using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single node in the skill tree.
/// Stores node data, UI position, connections, and unlockable content.
/// </summary>
[Serializable]
public class SkillTreeNode
{
    [Header("Node Identity")]
    [Tooltip("Unique identifier for this node")]
    public string nodeId;
    
    [Tooltip("Display name of the node")]
    public string nodeName;
    
    [Tooltip("Description shown when hovering over the node")]
    [TextArea(3, 6)]
    public string description;
    
    [Header("Node Visuals")]
    [Tooltip("Icon displayed on the node")]
    public Sprite icon;
    
    [Tooltip("Optional video demonstration of what this node unlocks")]
    public UnityEngine.Video.VideoClip demonstrationVideo;
    
    [Header("Cost & Requirements")]
    [Tooltip("Cost to unlock this node (skill points, currency, etc.)")]
    public int cost = 1;
    
    [Tooltip("Parent nodes that must be unlocked before this node becomes available")]
    public List<string> parentNodeIds = new List<string>();
    
    [Header("Unlockable Content")]
    [Tooltip("Attacks unlocked when this node is activated")]
    public Attack[] unlockedAttacks = Array.Empty<Attack>();
    
    [Tooltip("Which attack to show in the video demonstration")]
    public int displayedAttackIndex = 0;
    
    [Header("UI Layout (Set via Skill Tree Editor)")]
    [Tooltip("Position of this node in the skill tree UI (relative screen space 0-1)")]
    public Vector2 uiPosition = new Vector2(0.5f, 0.5f);
    
    /// <summary>
    /// Gets parent nodes by resolving IDs from the skill tree.
    /// </summary>
    public List<SkillTreeNode> GetParentNodes(PlayerSkillTree skillTree)
    {
        List<SkillTreeNode> parents = new List<SkillTreeNode>();
        
        foreach (string parentId in parentNodeIds)
        {
            SkillTreeNode parent = skillTree.GetNodeById(parentId);
            if (parent != null)
            {
                parents.Add(parent);
            }
        }
        
        return parents;
    }
    
    /// <summary>
    /// Checks if this node's requirements are met (all parents unlocked).
    /// </summary>
    public bool AreRequirementsMet(List<SkillTreeNode> unlockedNodes)
    {
        if (parentNodeIds.Count == 0)
            return true; // No requirements
        
        foreach (string parentId in parentNodeIds)
        {
            bool parentUnlocked = unlockedNodes.Exists(n => n.nodeId == parentId);
            if (!parentUnlocked)
                return false;
        }
        
        return true;
    }
}
