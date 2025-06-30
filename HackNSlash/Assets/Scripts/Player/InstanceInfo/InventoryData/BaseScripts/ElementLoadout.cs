using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementLoadout", menuName = "Inventory/ElementLoadout", order = 2)]
public class ElementLoadout : ScriptableObject
{
    public EquippedElementAttack fireElementAttack;
    public EquippedElementAttack iceElementAttack;
    public EquippedElementAttack lightningElementAttack;
    public EquippedElementAttack earthElementAttack;
    public EquippedElementAttack windElementAttack;
    
    private Dictionary<ElementEffect, EquippedElementAttack> elementAttackMap = new Dictionary<ElementEffect, EquippedElementAttack>();
    
    #region Element Methods
    
    public bool SetElementAttack(ElementEffect elementEffect, AttacksByWeapon attack, KeyBind k)
    {
        if (!elementAttackMap.TryGetValue(elementEffect, out var elementAttack))
        {
            Debug.LogWarning($"No attack found for element {elementEffect}");
            return false;
        }

        switch (k)
        {
            case KeyBind.HeavyAttack:
                elementAttack.northAttack = attack;
                break;
            case KeyBind.LightAttack:
                elementAttack.westAttack = attack;
                break;
            case KeyBind.Jump:
                elementAttack.southAttack = attack;
                break;
            default:
                Debug.LogWarning($"Invalid keybind {k} for element {elementEffect}");
                return false;
        }

        elementAttack.ValidateKeyBinds();
        return ValidateElementAttacks();
    }
    
    public AttacksByWeapon RemoveElementAttack(ElementEffect elementEffect, KeyBind k)
    {
        
        if (!elementAttackMap.TryGetValue(elementEffect, out var elementAttack))
        {
            Debug.LogWarning($"No attack found for element {elementEffect}");
            return null;
        }

        if (elementAttack.keyBindAttackMap.TryGetValue(k, out var attack))
        {
            elementAttack.keyBindAttackMap.Remove(k);
            ValidateElementAttacks();
            return attack;
        }
        
        Debug.LogWarning($"No attack found for element {elementEffect} with keybind {k}");
        return null;
    }
    
    public Attack[] GetElementAttacks(ElementEffect elementEffect, MovingStates state)
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
                return element.GetSwordAttacks();
            case MovingStates.Katana:
                return element.GetKatanaAttacks();
            default:
                Debug.LogWarning($"No attack found for element {elementEffect} in state {state}");
                return null;
        }
    }
    
    public EquippedElementAttack GetElementAttack(ElementEffect elementEffect)
    {
        if (elementAttackMap.TryGetValue(elementEffect, out var elementAttack))
        {
            return elementAttack;
        }
        
        Debug.LogWarning($"No attack found for element {elementEffect}");
        return null;
    }
    
    
    public bool ValidateElementAttacks()
    {
        bool valid = true;
        
        fireElementAttack.elementEffect = ElementEffect.Fire;
        iceElementAttack.elementEffect = ElementEffect.Ice;
        lightningElementAttack.elementEffect = ElementEffect.Lightning;
        earthElementAttack.elementEffect = ElementEffect.Earth;
        windElementAttack.elementEffect = ElementEffect.Wind;
        
        elementAttackMap.Clear();
        elementAttackMap.Add(ElementEffect.Fire, fireElementAttack);
        elementAttackMap.Add(ElementEffect.Ice, iceElementAttack);
        elementAttackMap.Add(ElementEffect.Lightning, lightningElementAttack);
        elementAttackMap.Add(ElementEffect.Earth, earthElementAttack);
        elementAttackMap.Add(ElementEffect.Wind, windElementAttack);
        
        foreach (var elementAttack in elementAttackMap)
        {
            if (elementAttack.Value == null)
            {
                Debug.LogWarning($"Element attack for {elementAttack.Key} is null.");
                valid = false;
                continue;
            }
            
            valid &= elementAttack.Value.CheckAttackElement(elementAttack.Key);
            
            elementAttack.Value.ValidateKeyBinds();
        }

        return valid;
    }
    
    #endregion

    private void OnValidate()
    {
        ValidateElementAttacks();
    }
}

[Serializable]
public class EquippedElementAttack
{
    public ElementEffect elementEffect;
    
    public AttacksByWeapon northAttack;
    public AttacksByWeapon westAttack;
    public AttacksByWeapon southAttack;
    public AttacksByWeapon eastAttack;
    
    public Dictionary<KeyBind, AttacksByWeapon> keyBindAttackMap = new Dictionary<KeyBind, AttacksByWeapon>();

    public void ValidateKeyBinds()
    {
        keyBindAttackMap.Clear();
        
        if (northAttack != null)
        {
            keyBindAttackMap[KeyBind.HeavyAttack] = northAttack;
            
            if (northAttack.SwordAttack != null)
            {
                northAttack.SwordAttack.keyBinds = new KeyBind[] { KeyBind.HeavyAttack };
            }
        }
        
        if (westAttack != null)
        {
            keyBindAttackMap[KeyBind.LightAttack] = westAttack;
            
            if (westAttack.SwordAttack != null)
            {
                westAttack.SwordAttack.keyBinds = new KeyBind[] { KeyBind.LightAttack };
            }
        }
        
        if (southAttack != null)
        {
            keyBindAttackMap[KeyBind.Jump] = southAttack;
            
            if (southAttack.SwordAttack != null)
            {
                southAttack.SwordAttack.keyBinds = new KeyBind[] { KeyBind.Jump };
            }
        }
        
        if (eastAttack != null)
        {
            keyBindAttackMap[KeyBind.Dodge] = eastAttack;
            
            if (eastAttack.SwordAttack != null)
            {
                eastAttack.SwordAttack.keyBinds = new KeyBind[] { KeyBind.Dodge };
            }
        }
    }

    public Attack[] GetSwordAttacks()
    {
        List<Attack> swordAttacks = new List<Attack>();
        
        if (northAttack != null && northAttack.SwordAttack != null)
        {
            swordAttacks.Add(northAttack.SwordAttack);
        }
        
        if (westAttack != null && westAttack.SwordAttack != null)
        {
            swordAttacks.Add(westAttack.SwordAttack);
        }
        
        if (southAttack != null && southAttack.SwordAttack != null)
        {
            swordAttacks.Add(southAttack.SwordAttack);
        }
        
        if (eastAttack != null && eastAttack.SwordAttack != null)
        {
            swordAttacks.Add(eastAttack.SwordAttack);
        }
        
        return swordAttacks.ToArray();
    }
    
    public Attack[] GetKatanaAttacks()
    {
        List<Attack> katanaAttacks = new List<Attack>();
        
        if (northAttack != null && northAttack.KatanaAttack != null)
        {
            katanaAttacks.Add(northAttack.KatanaAttack);
        }
        
        if (westAttack != null && westAttack.KatanaAttack != null)
        {
            katanaAttacks.Add(westAttack.KatanaAttack);
        }
        
        if (southAttack != null && southAttack.KatanaAttack != null)
        {
            katanaAttacks.Add(southAttack.KatanaAttack);
        }
        
        if (eastAttack != null && eastAttack.KatanaAttack != null)
        {
            katanaAttacks.Add(eastAttack.KatanaAttack);
        }
        
        return katanaAttacks.ToArray();
    }

    public bool CheckAttackElement(ElementEffect element)
    {
        bool valid = true;
        
        if (northAttack != null && northAttack.SwordAttack != null && northAttack.SwordAttack.element != element && northAttack.SwordAttack.element != ElementEffect.MatchCurrent)
        {
            Debug.LogWarning($"North attack element mismatch: {northAttack.SwordAttack.element} != {element}");
            northAttack.SwordAttack = null;
            valid = false;
        }
        
        if (westAttack != null && westAttack.SwordAttack != null && westAttack.SwordAttack.element != element && westAttack.SwordAttack.element != ElementEffect.MatchCurrent)
        {
            Debug.LogWarning($"West attack element mismatch: {westAttack.SwordAttack.element} != {element}");
            westAttack.SwordAttack = null;
            valid = false;
        }
        
        if (southAttack != null && southAttack.SwordAttack != null && southAttack.SwordAttack.element != element && southAttack.SwordAttack.element != ElementEffect.MatchCurrent)
        {
            Debug.LogWarning($"South attack element mismatch: {southAttack.SwordAttack.element} != {element}");
            southAttack.SwordAttack = null;
            valid = false;
        }
        
        if (eastAttack != null && eastAttack.SwordAttack != null && eastAttack.SwordAttack.element != element && eastAttack.SwordAttack.element != ElementEffect.MatchCurrent)
        {
            Debug.LogWarning($"East attack element mismatch: {eastAttack.SwordAttack.element} != {element}");
            eastAttack.SwordAttack = null;
            valid = false;
        }
        
        if (northAttack != null && northAttack.KatanaAttack != null && northAttack.KatanaAttack.element != element && northAttack.KatanaAttack.element != ElementEffect.MatchCurrent)
        {
            Debug.LogWarning($"North attack element mismatch: {northAttack.KatanaAttack.element} != {element}");
            northAttack.KatanaAttack = null;
            valid = false;
        }
        
        if (westAttack != null && westAttack.KatanaAttack != null && westAttack.KatanaAttack.element != element && westAttack.KatanaAttack.element != ElementEffect.MatchCurrent)
        {
            Debug.LogWarning($"West attack element mismatch: {westAttack.KatanaAttack.element} != {element}");
            westAttack.KatanaAttack = null;
            valid = false;
        }
        
        if (southAttack != null && southAttack.KatanaAttack != null && southAttack.KatanaAttack.element != element && southAttack.KatanaAttack.element != ElementEffect.MatchCurrent)
        {
            Debug.LogWarning($"South attack element mismatch: {southAttack.KatanaAttack.element} != {element}");
            southAttack.KatanaAttack = null;
            valid = false;
        }
        
        if (eastAttack != null && eastAttack.KatanaAttack != null && eastAttack.KatanaAttack.element != element && eastAttack.KatanaAttack.element != ElementEffect.MatchCurrent)
        {
            Debug.LogWarning($"East attack element mismatch: {eastAttack.KatanaAttack.element} != {element}");
            eastAttack.KatanaAttack = null;
            valid = false;
        }
        
        return valid;
    }
    
}

[Serializable]
public class AttacksByWeapon
{
    public Attack SwordAttack;
    public Attack KatanaAttack;
    
    public Attack GetAttackByState(MovingStates state)
    {
        switch (state)
        {
            case MovingStates.DualSword:
                return SwordAttack;
            case MovingStates.Katana:
                return KatanaAttack;
            case MovingStates.NonCombat:
                return SwordAttack;
            default:
                return null;
        }
    }
}