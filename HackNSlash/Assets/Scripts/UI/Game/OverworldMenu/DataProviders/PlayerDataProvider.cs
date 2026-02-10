using System.Collections.Generic;
using System.Linq;
using Extensions.UI;
using UnityEngine;

/// <summary>
/// Implementation of IPlayerDataProvider that bridges the UI with PlayerController/PlayerStats.
/// This is the backend connector for Tab 1 (Character Stats).
/// Locally managed by OverworldMenuUI - not a global service.
/// </summary>
public class PlayerDataProvider : MonoBehaviour, IPlayerDataProvider
{
    private PlayerController playerController;
    private PlayerInventory playerInventory;
    private PlayerStats playerStats;
    
    #region MonoBehaviour Callbacks
    
    private void Start()
    {
        // Get player controller reference via Services (PlayerController is a valid global service)
        try
        {
            playerController = Services.Get<PlayerController>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"PlayerDataProvider: Could not get PlayerController service: {e.Message}");
        }
        
        if (playerController != null)
        {
            playerInventory = playerController.pi;
            playerStats = playerController.ps;
        }
    }
    
    #endregion
    
    #region IPlayerDataProvider Implementation
    
    /// <summary>
    /// Gets the current player stats for display.
    /// </summary>
    public PlayerStatsDisplayData GetPlayerStats()
    {
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerDataProvider: PlayerStats not available!");
            return PlayerStatsDisplayData.Default();
        }
        
        return new PlayerStatsDisplayData
        {
            level = 1, // TODO: Implement level system
            currentExperience = 0, // TODO: Implement experience system
            experienceToNextLevel = 100,
            maxHealth = playerStats.GetStat(InnateStat.MaxHealth),
            currentHealth = playerStats.CurrentHealth,
            maxCharge = playerStats.GetStat(InnateStat.MaxCharge),
            currentCharge = playerStats.CurrentElementCharge,
            strength = (int)playerStats.GetStat(InnateStat.Strength),
            defense = (int)playerStats.GetStat(InnateStat.Defense)
        };
    }
    
    /// <summary>
    /// Gets the equipped items for all slots.
    /// </summary>
    public Extensions.UI.EquippedItemDisplayData[] GetEquippedItems()
    {
        if (playerInventory == null || playerInventory.CurrentLoadout == null)
        {
            Debug.LogWarning("PlayerDataProvider: PlayerInventory or CurrentLoadout not available!");
            return new Extensions.UI.EquippedItemDisplayData[6]; // 3 accessories + 3 passives
        }
        
        var equipped = new List<Extensions.UI.EquippedItemDisplayData>();
        
        // Get equipped accessories
        for (int i = 0; i < 3; i++)
        {
            Accessory accessory = playerInventory.CurrentLoadout.equippedAccessories != null && 
                                  i < playerInventory.CurrentLoadout.equippedAccessories.Length
                ? playerInventory.CurrentLoadout.equippedAccessories[i]
                : null;
            
            if (accessory != null)
            {
                equipped.Add(new Extensions.UI.EquippedItemDisplayData
                {
                    icon = accessory.itemIcon,
                    itemName = accessory.itemName,
                    itemDescription = accessory.itemDescription,
                    rarity = accessory.itemRarity,
                    isEmpty = false
                });
            }
            else
            {
                equipped.Add(Extensions.UI.EquippedItemDisplayData.Empty());
            }
        }
        
        // Get equipped passives (TODO: Implement passive system)
        for (int i = 0; i < 3; i++)
        {
            equipped.Add(Extensions.UI.EquippedItemDisplayData.Empty());
        }
        
        return equipped.ToArray();
    }
    
    #endregion
}

