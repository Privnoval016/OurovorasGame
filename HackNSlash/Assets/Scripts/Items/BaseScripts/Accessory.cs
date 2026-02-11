using System.Collections.Generic;
using Extensions.Patterns;
using UnityEngine;

[CreateAssetMenu(fileName = "New Accessory", menuName = "Inventory/Items/Accessory")]
public class Accessory : InventoryItem
{
    [Header("Accessory Details")]
    
    public InnateStatChange[] statChanges; // Array of stat changes this accessory provides
    
    [SerializeReference] public IEquipmentEffect[] equipmentEffects; // Array of effects this accessory provides

    public IEnumerable<IDamageRule> ContributeRules()
    {
        foreach (var effect in equipmentEffects)
        {
            yield return effect.GetRule();
        }
    }
    
    public void InitializeEffects(EvaluatedStats stats)
    {
        var modifierFactory = new StatModifierFactory();
        
        foreach (var statChange in statChanges)
        {
            var modifier = modifierFactory.Create(statChange);
            stats.StatMediator.AddModifier(modifier);
        }
    }
}

public interface IEquipmentEffect
{
    IDamageRule GetRule();
}
