
using System.Collections.Generic;
using Extensions.EventBus;
using UnityEngine;

/**
 * Broadcast strategy that triggers when the player has specific items in their inventory.
 */
public class ItemsInInventoryConditionStrategy : IQuestConditionStrategy
{
    [Header("Inventory Check Settings")]
    private PlayerController player;
    public List<ItemStack> requiredItems = new();
    
    private bool hasItemsInInventory = false;
    
    private EventBinding<OnInventoryUpdatedEvent> inventoryUpdateBinding; // only performs check when inventory is updated

    protected override void OnInitialize()
    {
        base.OnInitialize();
        player = Services.Get<PlayerController>();
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
        
        InventoryInfo inventory = player.rps.inventoryInfo;
        
        foreach (var req in requiredItems)
        {
            InventoryStack stack = inventory.GetStackOfType(req.item);
            if (stack == null || stack.amount < req.requiredAmount)
            {
                Debug.Log($"Not enough of item: {req.item.itemName} because stack is {(stack == null ? "null" : "not enough")}");
                hasItemsInInventory = false;
                Broadcast(Broadcaster); // broadcast even if condition not met to turn off objectives that were previously completed
                return;
            }
        }
        
        hasItemsInInventory = true;
        Broadcast(Broadcaster);
    }

    public override string ToString()
    {
        return "Items In Inventory";
    }

    public override bool Evaluate() => hasItemsInInventory;

    public override bool StopIfTriggered() => false;
}