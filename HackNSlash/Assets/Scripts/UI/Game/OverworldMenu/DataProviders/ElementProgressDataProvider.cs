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
    private PlayerInventory playerInventory;
    private ElementLoadout elementLoadout;
    
    #region MonoBehaviour Callbacks
    
    private void Start()
    {
        try
        {
            var playerController = Services.Get<PlayerController>();
            
            if (playerController != null)
            {
                playerInventory = playerController.pi;
                
                if (playerInventory?.CurrentLoadout != null)
                    elementLoadout = playerController.pcc.CurrentElementLoadout;
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
        // TODO: Implement element progression system
        var progressData = new ElementProgressData
        {
            element = element,
            currentLevel = 1,
            progressToNextLevel = 0.3f,
            levelDescriptions = new string[10]
        };
        
        // Placeholder descriptions
        for (int i = 0; i < 10; i++)
        {
            progressData.levelDescriptions[i] = $"{element} Level {i + 1} - Unlocks new abilities and stat bonuses";
        }
        
        return progressData;
    }
    
    /// <summary>
    /// Gets all available elements.
    /// </summary>
    public ElementEffect[] GetAvailableElements()
    {
        return new ElementEffect[]
        {
            ElementEffect.Fire,
            ElementEffect.Ice,
            ElementEffect.Lightning,
            ElementEffect.Earth,
            ElementEffect.Wind
        };
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
        displayData[0] = ConvertToDisplayData(equippedAttacks.westAttack?.SwordAttack, element);
        displayData[1] = ConvertToDisplayData(equippedAttacks.northAttack?.SwordAttack, element);
        displayData[2] = ConvertToDisplayData(equippedAttacks.southAttack?.SwordAttack, element);
        
        return displayData;
    }
    
    /// <summary>
    /// Gets all available attacks for the specified element.
    /// </summary>
    public List<AttackDisplayData> GetAvailableAttacks(ElementEffect element)
    {
        var attacks = new List<AttackDisplayData>();
        
        // TODO: Get actual attacks from skill tree or attack data
        // For now, return placeholder data
        attacks.Add(new AttackDisplayData
        {
            attackName = $"{element} Strike",
            description = "A basic elemental attack",
            element = element,
            isUnlocked = true,
            isEquipped = false
        });
        
        attacks.Add(new AttackDisplayData
        {
            attackName = $"{element} Slash",
            description = "A powerful elemental slash",
            element = element,
            isUnlocked = true,
            isEquipped = false
        });
        
        attacks.Add(new AttackDisplayData
        {
            attackName = $"{element} Burst",
            description = "An explosive elemental attack",
            element = element,
            isUnlocked = true,
            isEquipped = false
        });
        
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
        
        // TODO: Get actual attack object and set it
        Debug.Log($"Assigned attack {availableAttacks[attackIndex].attackName} to {keyBind} button for {element}");
    }
    
    #endregion
    
    #region Helper Methods
    
    private AttackDisplayData ConvertToDisplayData(Attack attack, ElementEffect element)
    {
        if (attack == null)
            return AttackDisplayData.Empty();
        
        return new AttackDisplayData
        {
            attackName = attack.name,
            description = "Attack description", // TODO: Add description field to Attack class
            icon = null, // TODO: Add icon field to Attack class
            element = attack.element,
            isUnlocked = attack.isEnabled,
            isEquipped = true
        };
    }
    
    #endregion
}

