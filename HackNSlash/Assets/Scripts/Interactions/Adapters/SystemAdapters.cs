using UnityEngine;
using Extensions.Dialogue.Runtime;


/// <summary>
/// Adapter for dialogue system interactions.
/// Allows NPCs with dialogue to be interactable.
/// </summary>
public sealed class DialogueInteractable : InteractableBase
{
    [SerializeField] private Extensions.Dialogue.Data.DialogueGraph dialogueGraph;
    [SerializeField] private bool resumeFromLastNode = false;

    public override void Interact()
    {
        if (dialogueGraph == null)
        {
            Debug.LogError("[DialogueInteractable] No dialogue graph assigned");
            return;
        }

        var dialogueManager = Extensions.Dialogue.Integration.DialogueManager.Instance;
        if (dialogueManager == null)
        {
            Debug.LogError("[DialogueInteractable] No dialogue manager found in scene");
            return;
        }

        dialogueManager.StartDialogue(dialogueGraph);
    }
}

/// <summary>
/// Adapter for inventory interactions (pickups, collectibles).
/// </summary>
public sealed class InventoryInteractable : InteractableBase
{
    [SerializeField] private int itemId = 0;
    [SerializeField] private int quantity = 1;
    [SerializeField] private bool destroyAfterPickup = true;

    public override void Interact()
    {
        // This will be implemented when inventory system is created
        // For now, just log
        Debug.Log($"[InventoryInteractable] Picked up item {itemId} x{quantity}");

        if (destroyAfterPickup)
        {
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Adapter for custom action interactions.
/// Call delegate when interacted.
/// </summary>
public sealed class ActionInteractable : InteractableBase
{
    public System.Action OnInteractCallback;

    public override void Interact()
    {
        OnInteractCallback?.Invoke();
    }
}

/// <summary>
/// Adapter for quest interactions.
/// Trigger quest state changes on interaction.
/// </summary>
public sealed class QuestInteractable : InteractableBase
{
    [SerializeField] private int questId = 0;
    [SerializeField] private bool completeOnInteract = false;

    public override void Interact()
    {
        // This will be implemented when quest system is made generic
        // For now, just log
        Debug.Log($"[QuestInteractable] Interacted with quest {questId}");

        if (completeOnInteract)
        {
            // Quest completion logic
        }
    }
}


