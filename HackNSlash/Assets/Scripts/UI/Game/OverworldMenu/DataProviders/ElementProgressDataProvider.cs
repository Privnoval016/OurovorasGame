using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.UI;
using UnityEngine;

/// <summary>
/// Implementation of IElementProgressDataProvider that bridges the UI with PlayerInventory.
/// This is the backend connector for Tab 3 (Element Progress).
/// Locally managed by OverworldMenuUI - not a global service.
/// </summary>
public class ElementProgressDataProvider : MonoBehaviour, IElementProgressDataProvider
{
    private RuntimePlayerStatus runtimePlayerStatus;
    private ElementLoadout elementLoadout;
    
    private Dictionary<string, AttacksByWeapon> attackLookup = new Dictionary<string, AttacksByWeapon>();
    
    #region MonoBehaviour Callbacks
    
    private void Start()
    {
        try
        {
            var playerController = Services.Get<PlayerController>();
            
            if (playerController != null)
            {
                runtimePlayerStatus = playerController.rps;
                
                if (runtimePlayerStatus?.CurrentLoadout != null)
                    elementLoadout = playerController.rps.CurrentElementLoadout;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"ElementProgressDataProvider: Could not get PlayerController: {e.Message}");
        }
    }
    
    #endregion
    
    #region IElementProgressDataProvider Implementation
    
    /// <summary>
    /// Gets the progression data for a specific element.
    /// </summary>
    public ElementProgressData GetElementProgress(ElementEffect element)
    {
        if (runtimePlayerStatus == null)
        {
            Debug.LogWarning("ElementProgressDataProvider: RuntimePlayerStatus not available!");
            return new ElementProgressData { element = element, currentLevel = 0, progressToNextLevel = 0f };
        }
        
        var unlockInfo = runtimePlayerStatus.elementUnlocks.FirstOrDefault(e => e.elementUnlock.element == element);
        
        if (unlockInfo == null)
        {
            Debug.LogWarning($"ElementProgressDataProvider: No unlock info found for element {element}!");
            return new ElementProgressData { element = element, currentLevel = 0, progressToNextLevel = 0f };
        }
        
        return new ElementProgressData
        {
            element = element,
            currentLevel = unlockInfo.CurrentLevel,
            progressToNextLevel = unlockInfo.unlockLevelProgress - unlockInfo.CurrentLevel,
            levelDescriptions = unlockInfo.elementUnlock.progressionEntries.Select(e => e.description).ToArray()
        };
    }
    
    /// <summary>
    /// Gets all available elements.
    /// </summary>
    public ElementEffect[] GetAvailableElements()
    {
        return runtimePlayerStatus?.elementEffects ?? Array.Empty<ElementEffect>();
    }
    
    /// <summary>
    /// Gets the attacks assigned to the specified element.
    /// </summary>
    public AttackDisplayData[] GetAssignedAttacks(ElementEffect element)
    {
        if (elementLoadout == null)
        {
            Debug.LogWarning("ElementProgressDataProvider: ElementLoadout not available!");
            return new AttackDisplayData[3];
        }
        
        var equippedAttacks = elementLoadout.GetElementAttack(element);
        
        if (equippedAttacks == null)
        {
            return new AttackDisplayData[3];
        }
        
        var displayData = new AttackDisplayData[3];
        
        // Get attacks for each button (West=X, North=Y, South=A in Xbox controller layout)
        displayData[0] = ConvertToDisplayData(equippedAttacks.westAttack, element);
        displayData[1] = ConvertToDisplayData(equippedAttacks.northAttack, element);
        displayData[2] = ConvertToDisplayData(equippedAttacks.southAttack, element);
        
        return displayData;
    }
    
    /// <summary>
    /// Gets all available attacks for the specified element.
    /// </summary>
    public List<AttackDisplayData> GetAvailableAttacks(ElementEffect element)
    {
        if (runtimePlayerStatus == null)
        {
            Debug.LogWarning("ElementProgressDataProvider: RuntimePlayerStatus not available!");
            return new List<AttackDisplayData>();
        }
        
        var elementUnlocks = runtimePlayerStatus.elementUnlocks.FirstOrDefault(e => e.elementUnlock.element == element);
        
        if (elementUnlocks == null)
        {
            Debug.LogWarning($"ElementProgressDataProvider: No unlock info found for element {element}!");
            return new List<AttackDisplayData>();
        }

        var entries = elementUnlocks.GetUnlockedEntries();
        
        var attacks = new List<AttackDisplayData>();
        foreach (var entry in entries)
        {
            if (entry.isAttackUnlock && entry.unlockedAttack != null)
            {
                attacks.Add(ConvertToDisplayData(entry.unlockedAttack, element));
            }
        }
        
        return attacks;
    }
    
    /// <summary>
    /// Assigns an attack to a button for the specified element.
    /// </summary>
    public void AssignAttack(ElementEffect element, int buttonIndex, int attackIndex)
    {
        if (elementLoadout == null)
        {
            Debug.LogWarning("ElementProgressDataProvider: Cannot assign attack, ElementLoadout not available!");
            return;
        }
        
        var availableAttacks = GetAvailableAttacks(element);
        
        if (attackIndex < 0 || attackIndex >= availableAttacks.Count)
        {
            Debug.LogWarning($"ElementProgressDataProvider: Invalid attack index {attackIndex}!");
            return;
        }
        
        // Convert button index to KeyBind
        KeyBind keyBind = buttonIndex switch
        {
            0 => KeyBind.West,
            1 => KeyBind.North,
            2 => KeyBind.South,
            _ => KeyBind.West
        };
        
        Debug.Log($"Assigned attack {availableAttacks[attackIndex].attackName} to {keyBind} button for {element}");
        
        RefreshLookup();
        var attackToAssign = attackLookup.ContainsKey(availableAttacks[attackIndex].attackName) 
            ? attackLookup[availableAttacks[attackIndex].attackName] 
            : null;
        
        elementLoadout.SetElementAttack(element, attackToAssign, keyBind);
    }
    
    #endregion
    
    #region Helper Methods
    
    private AttackDisplayData ConvertToDisplayData(AttacksByWeapon attack, ElementEffect element)
    {
        if (attack == null)
            return AttackDisplayData.Empty();
        
        return new AttackDisplayData
        {
            attackName = attack.SwordAttack.name,
            description = "Attack description", // TODO: Add description field to Attack class
            icon = null, // TODO: Add icon field to Attack class
            element = attack.SwordAttack.element,
            isUnlocked = attack.SwordAttack.isEnabled,
            isEquipped = true
        };
    }
    
    private void RefreshLookup()
    {
        attackLookup.Clear();
        
        if (runtimePlayerStatus == null) return;
        
        foreach (var elementUnlock in runtimePlayerStatus.elementUnlocks)
        {
            var entries = elementUnlock.GetUnlockedEntries();
            foreach (var entry in entries)
            {
                if (entry.unlockedAttack != null && !attackLookup.ContainsKey(entry.unlockedAttack.SwordAttack.name))
                {
                    attackLookup.Add(entry.unlockedAttack.SwordAttack.name, entry.unlockedAttack);
                }
            }
        }
    }
    
    #endregion
}

