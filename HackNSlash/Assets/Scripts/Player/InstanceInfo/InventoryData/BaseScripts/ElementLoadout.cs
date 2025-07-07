using System;
using System.Collections.Generic;
using Extensions.Utils;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ElementLoadout", menuName = "Inventory/ElementLoadout", order = 2)]
public class ElementLoadout : ScriptableObject
{
    [Header("Cooldowns")] 
    public ElementCooldownInfo northCooldown = new ElementCooldownInfo(79f, 1.0f);
    public ElementCooldownInfo westCooldown = new ElementCooldownInfo(99f, 1.2f);
    public ElementCooldownInfo southCooldown = new ElementCooldownInfo(149f, 1.5f);
    
    [Header("Element Attacks")]
    public EquippedElementAttack fireElementAttack;
    public EquippedElementAttack iceElementAttack;
    public EquippedElementAttack lightningElementAttack;
    public EquippedElementAttack earthElementAttack;
    public EquippedElementAttack windElementAttack;
    
    [Header("Finishers")]
    public AttacksByWeapon finishers;
    
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
            case KeyBind.North:
                elementAttack.northAttack = attack;
                break;
            case KeyBind.West:
                elementAttack.westAttack = attack;
                break;
            case KeyBind.South:
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

        Attack[] attacks;
        switch (state)
        {
            case MovingStates.DualSword:
                attacks = element.GetSwordAttacks();
                break;
            case MovingStates.Katana:
                attacks = element.GetKatanaAttacks();
                break;
            default:
                attacks = element.GetSwordAttacks();
                break;
        }
        
        attacks = attacks.Add(finishers?.GetAttackByState(state));
        
        return attacks;
        
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

        ValidateFinisherKeyBinds();

        return valid;
    }
    
    private void ValidateFinisherKeyBinds()
    {
        if (finishers == null) return;
        
        if (finishers.SwordAttack != null)
        {
            finishers.SwordAttack.keyBinds = new KeyBind[] { KeyBind.East };
        }
        
        if (finishers.KatanaAttack != null)
        {
            finishers.KatanaAttack.keyBinds = new KeyBind[] { KeyBind.East };
        }
    }
    
    public bool AttackIsFinisher(Attack attack)
    {
        if (attack == null) return false;
        
        if (finishers == null) return false;
        
        if (finishers.SwordAttack == attack || finishers.KatanaAttack == attack)
        {
            return true;
        }
        
        return false;
    }
    
    #endregion
    
    #region Cooldown Methods
    
    public float GetMinCharge(KeyBind key)
    {
        return key switch
        {
            KeyBind.North => northCooldown.minCharge,
            KeyBind.West => westCooldown.minCharge,
            KeyBind.South => southCooldown.minCharge,
            _ => 0f
        };
    }
    
    public float GetDamageMultiplier(KeyBind key)
    {
        return key switch
        {
            KeyBind.North => northCooldown.damageMultiplier,
            KeyBind.West => westCooldown.damageMultiplier,
            KeyBind.South => southCooldown.damageMultiplier,
            _ => 1f
        };
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
    
    public Dictionary<KeyBind, AttacksByWeapon> keyBindAttackMap = new Dictionary<KeyBind, AttacksByWeapon>();

    public void ValidateKeyBinds()
    {
        keyBindAttackMap.Clear();
        
        if (northAttack != null)
        {
            keyBindAttackMap[KeyBind.North] = northAttack;
            
            if (northAttack.SwordAttack != null)
            {
                northAttack.SwordAttack.keyBinds = new KeyBind[] { KeyBind.North };
            }
        }
        
        if (westAttack != null)
        {
            keyBindAttackMap[KeyBind.West] = westAttack;
            
            if (westAttack.SwordAttack != null)
            {
                westAttack.SwordAttack.keyBinds = new KeyBind[] { KeyBind.West };
            }
        }
        
        if (southAttack != null)
        {
            keyBindAttackMap[KeyBind.South] = southAttack;
            
            if (southAttack.SwordAttack != null)
            {
                southAttack.SwordAttack.keyBinds = new KeyBind[] { KeyBind.South };
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
        
        return valid;
    }
    
    public bool AttackInLoadout(Attack attack)
    {
        if (northAttack != null && (northAttack.SwordAttack == attack || northAttack.KatanaAttack == attack)) return true;
        if (westAttack != null && (westAttack.SwordAttack == attack || westAttack.KatanaAttack == attack)) return true;
        if (southAttack != null && (southAttack.SwordAttack == attack || southAttack.KatanaAttack == attack)) return true;
        
        return false;
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

[Serializable]
public class ElementCooldownInfo
{
    [FormerlySerializedAs("cooldown")] public float minCharge;
    public float damageMultiplier;
    
    public ElementCooldownInfo(float minCharge, float damageMultiplier)
    {
        this.minCharge = minCharge;
        this.damageMultiplier = damageMultiplier;
    }
}