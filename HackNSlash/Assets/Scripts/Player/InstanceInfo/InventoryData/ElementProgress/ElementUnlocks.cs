using System.Collections.Generic;
using Extensions.UI;
using UnityEngine;

[CreateAssetMenu(fileName = "New Element Progress Data", menuName = "Player/Element Progress Data")]
public class ElementUnlocks : ScriptableObject
{
    public ElementEffect element;
    
    public List<ElementProgressEntry> progressionEntries = new();

    public void InitializeStatChanges(EvaluatedStats stats, int level)
    {
        StatModifierFactory modifierFactory = new StatModifierFactory();
        
        foreach (ElementProgressEntry entry in progressionEntries)
        {
            if (level >= entry.levelRequirement && entry.statChange != null)
            {
                var modifier = modifierFactory.Create(entry.statChange);
                stats.StatMediator.AddModifier(modifier);
            }
        }
    }
    
}

[System.Serializable]
public class ElementProgressEntry
{
    public AttacksByWeapon unlockedAttack;
    public InnateStatChange statChange;
    
    public int levelRequirement;
    
    [TextArea]
    public string description;
}
