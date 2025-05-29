using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSkillTree", menuName = "Inventory/SkillTree", order = 1)]
public class PlayerSkillTree : ScriptableObject
{
    public string treeName;
    
    [Header("Skill Tree Nodes")]
    public SkillTreeNode[] skillTreeNodes = Array.Empty<SkillTreeNode>();

}



