
using System.Collections.Generic;
using Extensions.EventBus;
using UnityEngine;

/**
 * Broadcast strategy that triggers when the player has specific items in their inventory.
 */
public class ItemsInInventoryBroadcastStrategy : IQuestEventBroadcastStrategy
{
    [Header("Inventory Check Settings")]
    private PlayerController player;
    public List<ItemStack> requiredItems = new();
    
    private EventBinding<OnInventoryUpdatedEvent> inventoryUpdateBinding; // only performs check when inventory is updated

    protected override void OnInitialize()
    {
        base.OnInitialize();
        player = GameManager.Instance.pc;
        inventoryUpdateBinding = new EventBinding<OnInventoryUpdatedEvent>(OnInventoryUpdate);
        EventBus<OnInventoryUpdatedEvent>.Register(inventoryUpdateBinding);
        
        OnInventoryUpdate();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventBus<OnInventoryUpdatedEvent>.Deregister(inventoryUpdateBinding);
    }

    private void OnInventoryUpdate()
    {
        Debug.Log("OnInventoryUpdate");
        
        InventoryInfo inventory = player.pi.inventoryInfo;
        
        foreach (var req in requiredItems)
        {
            InventoryStack stack = inventory.GetStackOfType(req.item);
            if (stack == null || stack.amount < req.requiredAmount)
            {
                Debug.Log($"Not enough of item: {req.item.itemName} because stack is {(stack == null ? "null" : "not enough")}");
                return;
            }
        }
        
        Broadcast(Receiver);
    }

    public override string ToString()
    {
        return "Items In Inventory";
    }
}