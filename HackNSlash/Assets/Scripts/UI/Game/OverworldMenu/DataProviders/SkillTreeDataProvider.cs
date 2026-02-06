using System.Collections.Generic;
using Extensions.Patterns;
using Extensions.UI;
using UnityEngine;

/// <summary>
/// Implementation of ISkillTreeDataProvider that bridges the UI with PlayerInventory's skill tree.
/// This is the backend connector for Tab 4 (Skill Tree).
/// </summary>
public class SkillTreeDataProvider : MonoBehaviour, ISkillTreeDataProvider, IService
{
    private PlayerCombatControl playerCombatControl;
    private PlayerSkillTree skillTree;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        Services.Register<SkillTreeDataProvider>(this);
    }
    
    private void Start()
    {
        var playerController = Services.Get<PlayerController>();
        
        if (playerController != null)
        {
            playerCombatControl = playerController.pcc;
            // TODO: Get skill tree from player inventory when implemented
            // skillTree = playerInventory.skillTree;
        }
    }
    
    #endregion
    
    #region ISkillTreeDataProvider Implementation
    
    /// <summary>
    /// Gets all skill nodes in the tree.
    /// </summary>
    public SkillNodeDisplayData[] GetAllNodes()
    {
        if (skillTree == null || skillTree.skillTreeNodes == null)
        {
            Debug.LogWarning("SkillTreeDataProvider: SkillTree not available!");
            return CreatePlaceholderNodes();
        }
        
        var displayNodes = new SkillNodeDisplayData[skillTree.skillTreeNodes.Length];
        
        for (int i = 0; i < skillTree.skillTreeNodes.Length; i++)
        {
            displayNodes[i] = ConvertToDisplayData(skillTree.skillTreeNodes[i], i);
        }
        
        return displayNodes;
    }
    
    /// <summary>
    /// Gets a specific skill node by index.
    /// </summary>
    public SkillNodeDisplayData GetNode(int nodeIndex)
    {
        if (skillTree == null || skillTree.skillTreeNodes == null)
        {
            Debug.LogWarning("SkillTreeDataProvider: SkillTree not available!");
            return CreatePlaceholderNode(nodeIndex);
        }
        
        if (nodeIndex < 0 || nodeIndex >= skillTree.skillTreeNodes.Length)
        {
            Debug.LogWarning($"SkillTreeDataProvider: Invalid node index {nodeIndex}!");
            return CreatePlaceholderNode(nodeIndex);
        }
        
        return ConvertToDisplayData(skillTree.skillTreeNodes[nodeIndex], nodeIndex);
    }
    
    /// <summary>
    /// Attempts to unlock a skill node.
    /// </summary>
    public bool UnlockNode(int nodeIndex)
    {
        if (skillTree == null || skillTree.skillTreeNodes == null)
        {
            Debug.LogWarning("SkillTreeDataProvider: SkillTree not available!");
            return false;
        }
        
        if (nodeIndex < 0 || nodeIndex >= skillTree.skillTreeNodes.Length)
        {
            Debug.LogWarning($"SkillTreeDataProvider: Invalid node index {nodeIndex}!");
            return false;
        }
        
        SkillTreeNode node = skillTree.skillTreeNodes[nodeIndex];
        
        // Check if already unlocked
        if (node.isUnlocked)
        {
            Debug.Log($"SkillTreeDataProvider: Node {node.nodeName} is already unlocked!");
            return false;
        }
        
        // Check prerequisites
        if (!ArePrerequisitesMet(node))
        {
            Debug.LogWarning($"SkillTreeDataProvider: Prerequisites not met for {node.nodeName}!");
            return false;
        }
        
        // TODO: Check if player has enough resources to unlock
        
        // Unlock the node
        node.isUnlocked = true;
        node.isActive = true;
        
        // Enable attacks associated with this node
        if (playerCombatControl != null)
            playerCombatControl.ActivateAttacksFromSkillTree();
        
        Debug.Log($"SkillTreeDataProvider: Unlocked skill node {node.nodeName}!");
        return true;
    }
    
    /// <summary>
    /// Gets the indices of nodes connected to the specified node.
    /// </summary>
    public int[] GetConnectedNodes(int nodeIndex)
    {
        if (skillTree == null || skillTree.skillTreeNodes == null)
            return new int[0];
        
        if (nodeIndex < 0 || nodeIndex >= skillTree.skillTreeNodes.Length)
            return new int[0];
        
        // Find nodes that list this node as a prerequisite
        var connected = new List<int>();
        
        for (int i = 0; i < skillTree.skillTreeNodes.Length; i++)
        {
            if (i == nodeIndex) continue;
            
            var node = skillTree.skillTreeNodes[i];
            if (node.prerequisites != null)
            {
                foreach (var prereq in node.prerequisites)
                {
                    if (prereq == skillTree.skillTreeNodes[nodeIndex])
                    {
                        connected.Add(i);
                        break;
                    }
                }
            }
        }
        
        return connected.ToArray();
    }
    
    #endregion
    
    #region Helper Methods
    
    private SkillNodeDisplayData ConvertToDisplayData(SkillTreeNode node, int index)
    {
        if (node == null)
            return CreatePlaceholderNode(index);
        
        var displayData = new SkillNodeDisplayData
        {
            nodeName = node.nodeName,
            description = node.description,
            icon = null, // TODO: Add icon to SkillTreeNode
            treePosition = CalculateNodePosition(index), // TODO: Add position data to SkillTreeNode
            isUnlocked = node.isUnlocked,
            isActive = node.isActive,
            canUnlock = ArePrerequisitesMet(node) && !node.isUnlocked,
            unlockCost = 1, // TODO: Add cost to SkillTreeNode
            unlockedAttacks = ConvertAttacksToDisplayData(node.unlockedAttacks),
            demonstrationVideo = null // TODO: Add video field
        };
        
        return displayData;
    }
    
    private AttackDisplayData[] ConvertAttacksToDisplayData(Attack[] attacks)
    {
        if (attacks == null || attacks.Length == 0)
            return new AttackDisplayData[0];
        
        var displayData = new AttackDisplayData[attacks.Length];
        
        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] != null)
            {
                displayData[i] = new AttackDisplayData
                {
                    attackName = attacks[i].name,
                    description = "Attack description", // TODO: Add description to Attack
                    icon = null, // TODO: Add icon to Attack
                    element = attacks[i].element,
                    isUnlocked = attacks[i].isEnabled,
                    isEquipped = false
                };
            }
            else
            {
                displayData[i] = AttackDisplayData.Empty();
            }
        }
        
        return displayData;
    }
    
    private bool ArePrerequisitesMet(SkillTreeNode node)
    {
        if (node.prerequisites == null || node.prerequisites.Length == 0)
            return true;
        
        foreach (var prereq in node.prerequisites)
        {
            if (prereq != null && !prereq.isUnlocked)
                return false;
        }
        
        return true;
    }
    
    private Vector2 CalculateNodePosition(int index)
    {
        // Simple grid layout for now
        int columns = 5;
        int x = index % columns;
        int y = index / columns;
        
        return new Vector2(x * 150f, y * 150f);
    }
    
    private SkillNodeDisplayData[] CreatePlaceholderNodes()
    {
        // Create some placeholder nodes for testing
        var nodes = new SkillNodeDisplayData[9];
        
        for (int i = 0; i < 9; i++)
        {
            nodes[i] = CreatePlaceholderNode(i);
        }
        
        return nodes;
    }
    
    private SkillNodeDisplayData CreatePlaceholderNode(int index)
    {
        return new SkillNodeDisplayData
        {
            nodeName = $"Skill Node {index + 1}",
            description = $"This is placeholder skill node {index + 1}. Unlock to gain new abilities.",
            treePosition = CalculateNodePosition(index),
            isUnlocked = index == 0, // First node unlocked by default
            isActive = index == 0,
            canUnlock = index <= 1,
            unlockCost = 1,
            unlockedAttacks = new AttackDisplayData[0]
        };
    }
    
    #endregion
}

