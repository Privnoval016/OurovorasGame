using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentLoadout", menuName = "Inventory/EquipmentLoadout", order = 1)]
public class EquipmentLoadout : ScriptableObject
{
    public string loadoutName;
    
    [Header("Element Info")]

    public EquippedElementAttack[] equippedElementAttacks = new EquippedElementAttack[5];
    public Dictionary<ElementEffect, EquippedElementAttack> elementAttackMap = new Dictionary<ElementEffect, EquippedElementAttack>();
    
    // public Reaction[] activeReactions;
    
    //[Header("Equipped Items")]



    #region Element Methods
    
    public void SetElementAttack(ElementEffect elementEffect, AttacksByWeapon attack)
    {
        foreach (var equippedAttack in equippedElementAttacks)
        {
            if (equippedAttack.elementEffect == elementEffect)
            {
                equippedAttack.attack = attack;
                break;
            }
        }
        
        ValidateElementAttacks();
    }
    
    public EquippedElementAttack RemoveElementAttack(ElementEffect elementEffect)
    {
        EquippedElementAttack removedAttack = null;
        
        if (elementAttackMap.TryGetValue(elementEffect, out removedAttack))
        {
            elementAttackMap.Remove(elementEffect);
            
            for (int i = 0; i < equippedElementAttacks.Length; i++)
            {
                if (equippedElementAttacks[i].elementEffect == elementEffect)
                {
                    equippedElementAttacks[i] = null;
                    break;
                }
            }
        }
        
        return removedAttack;
    }
    
    public Attack GetElementAttack(ElementEffect elementEffect, MovingStates state)
    {
        var element = elementAttackMap.GetValueOrDefault(elementEffect, null);

        if (element == null)
        {
            Debug.LogWarning($"No attack found for element {elementEffect}");
            return null;
        }
        
        switch (state)
        {
            case MovingStates.DualSword:
                return element.attack.SwordAttack;
            case MovingStates.Katana:
                return element.attack.KatanaAttack;
            default:
                Debug.LogWarning($"No attack found for element {elementEffect} in state {state}");
                return null;
        }
        
    }

    private void ValidateElementAttacks()
    {
        elementAttackMap.Clear();
        foreach (var attack in equippedElementAttacks)
        {
            if (attack != null && attack.elementEffect != ElementEffect.None)
            {
                elementAttackMap[attack.elementEffect] = attack;
            }
        }
    }
    
    #endregion

    private void OnValidate()
    {
        if (equippedElementAttacks == null)
        {
            equippedElementAttacks = new EquippedElementAttack[5];
        }
        
        if (equippedElementAttacks.Length > 5)
        {
            Array.Resize(ref equippedElementAttacks, 5);
        }
        
        ValidateElementAttacks();
    }
}


[Serializable]
public class EquippedElementAttack
{
    public ElementEffect elementEffect;
    public AttacksByWeapon attack;
}

[Serializable]
public class AttacksByWeapon
{
    public Attack SwordAttack;
    public Attack KatanaAttack;
}
