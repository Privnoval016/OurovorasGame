
using System.Collections.Generic;
using Extensions.EventBus;
using UnityEngine;

/**
 * Broadcast strategy that triggers when the player has specific items in their inventory.
 */
public class ItemsInInventoryBroadcastStrategy : IQuestEventBroadcastStrategy
{
    [Header("Inventory Check Settings")]
    public PlayerController player;
    public List<ItemStack> requiredItems = new();
    
    private EventBinding<OnInventoryUpdatedEvent> inventoryUpdateBinding; // only performs check when inventory is updated

    protected override void OnInitialize()
    {
        base.OnInitialize();
        inventoryUpdateBinding = new EventBinding<OnInventoryUpdatedEvent>(OnInventoryUpdate);
        EventBus<OnInventoryUpdatedEvent>.Register(inventoryUpdateBinding);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventBus<OnInventoryUpdatedEvent>.Deregister(inventoryUpdateBinding);
    }

    private void OnInventoryUpdate()
    {
        InventoryInfo inventory = player.pi.inventoryInfo;
        
        foreach (var req in requiredItems)
        {
            InventoryStack<InventoryItem> stack = inventory.GetStackOfType(req.item);
            if (stack == null || stack.amount < req.requiredAmount)
            {
                return;
            }
        }
        
        Broadcast(Receiver);
    }
}