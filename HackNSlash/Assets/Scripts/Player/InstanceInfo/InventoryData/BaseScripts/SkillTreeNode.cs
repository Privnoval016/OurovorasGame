using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillTreeNode", menuName = "Inventory/SkillTreeNode", order = 2)]
public class SkillTreeNode : ScriptableObject
{
    [Header("Node Info")]
    public string nodeName;
    public string description;
    public bool isUnlocked = false;
    public bool isActive = false;
    public SkillTreeNode[] prerequisites = Array.Empty<SkillTreeNode>();
    
    [Header("Node Effects")]
    
    public Attack[] unlockedAttacks = Array.Empty<Attack>();
    public int displayedAttackIndex = 0;
}
