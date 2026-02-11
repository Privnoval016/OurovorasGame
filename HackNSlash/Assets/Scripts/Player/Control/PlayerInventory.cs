using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.EventBus;
using Extensions.UI;
using Extensions.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

/**
 * PlayerInventory handles the player's equipment loadouts and inventory information. Anything related to the player's items should be managed here.
 */
public class PlayerInventory : MonoBehaviour
{
    [HideInInspector] public PlayerController pc;

    #region Loadout Info
    
    [Header("Loadout Info")]
    
    public InventoryItem testItem;

    #endregion
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        pc.rps.inventoryInfo?.Initialize();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Quote))
        {
            Debug.Log("L key pressed");
            AddItemToInventory(testItem, 1);
        }
        else if (Input.GetKeyDown(KeyCode.Semicolon))
        {
            Debug.Log("K key pressed");
            RemoveItemFromInventory(testItem, 1);
        }
    }

    #endregion
    
    #region Inventory Management
    
    public void SwitchLoadout(int loadoutIndex)
    {
        if (loadoutIndex < 0 || loadoutIndex >= pc.rps.loadouts.Length)
        {
            Debug.LogWarning("Invalid loadout index");
            return;
        }

        pc.rps.currentLoadoutIndex = loadoutIndex;
        EventBus<OnLoadoutChangedEvent>.Raise(new OnLoadoutChangedEvent
        {
            playerInventory = this
        });
    }
    
    public void AddItemToInventory(InventoryItem item, int amount = 1)
    {
        Debug.Log("Adding item to player inventory");
        pc.rps.inventoryInfo.AddItem(item, amount);
        EventBus<OnInventoryUpdatedEvent>.Raise(new OnInventoryUpdatedEvent
        {
            playerInventory = this,
            itemsChanged = new[] { new ItemStack(item, amount) }
        });
    }
    
    public void RemoveItemFromInventory(InventoryItem item, int amount = 1)
    {
        InventoryItem removedItem = pc.rps.inventoryInfo.RemoveItem(item, amount);
        
        if (removedItem != null)
        {
            EventBus<OnInventoryUpdatedEvent>.Raise(new OnInventoryUpdatedEvent
            {
                playerInventory = this,
                itemsChanged = new[] { new ItemStack(item, amount) }
            });
        }
    }
    
    #endregion
}

public struct OnLoadoutChangedEvent : IEvent
{
    public PlayerInventory playerInventory;
}

public struct OnInventoryUpdatedEvent : IEvent
{
    public PlayerInventory playerInventory;
    public ItemStack[] itemsChanged;
}