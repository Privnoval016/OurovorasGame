using UnityEngine;


/// <summary>
/// Defines how an interaction is triggered by the player.
/// </summary>
public enum InteractionTriggerType
{
    /// <summary>
    /// Requires player to click/press a button to interact.
    /// </summary>
    Manual,

    /// <summary>
    /// Automatically triggers when player enters trigger area.
    /// </summary>
    Auto,

    /// <summary>
    /// Triggers based on custom conditions (proximity, line of sight, etc).
    /// </summary>
    Custom
}

/// <summary>
/// Interface for handling player input during interactions.
/// Allows dynamic input subscription without tight coupling.
/// </summary>
public interface IInteractionInputHandler
{
    /// <summary>
    /// Called when input should start listening for interaction.
    /// </summary>
    void EnableInput();

    /// <summary>
    /// Called when input should stop listening.
    /// </summary>
    void DisableInput();

    /// <summary>
    /// Subscribe to receive interaction input events.
    /// </summary>
    void Subscribe(System.Action onInteractPressed);

    /// <summary>
    /// Unsubscribe from interaction input events.
    /// </summary>
    void Unsubscribe(System.Action onInteractPressed);
}

/// <summary>
/// Base interface for all interactable objects.
/// Keeps interactions modular and decoupled from specific systems.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Unique identifier for this interactable.
    /// </summary>
    System.Guid InteractableId { get; }

    /// <summary>
    /// How this interaction is triggered.
    /// </summary>
    InteractionTriggerType TriggerType { get; }

    /// <summary>
    /// Whether this interaction is currently available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Execute the interaction.
    /// </summary>
    void Interact();

    /// <summary>
    /// Called when player enters interaction range (for proximity hints).
    /// </summary>
    void OnEnterRange();

    /// <summary>
    /// Called when player leaves interaction range.
    /// </summary>
    void OnExitRange();
}

/// <summary>
/// Manages all active interactions in the scene.
/// Handles input, triggers, and communication with interactables.
/// </summary>
public interface IInteractionManager
{
    /// <summary>
    /// Register an interactable with the system.
    /// </summary>
    void RegisterInteractable(IInteractable interactable);

    /// <summary>
    /// Unregister an interactable from the system.
    /// </summary>
    void UnregisterInteractable(System.Guid interactableId);

    /// <summary>
    /// Set the currently focused interactable (for manual interactions).
    /// </summary>
    void SetFocusedInteractable(IInteractable interactable);

    /// <summary>
    /// Perform interaction on focused interactable.
    /// </summary>
    void PerformInteraction();

    /// <summary>
    /// Get the currently focused interactable.
    /// </summary>
    IInteractable GetFocusedInteractable();
}

/// <summary>
/// Interface for interaction UI presentation.
/// Keeps UI logic separate from interaction logic.
/// </summary>
public interface IInteractionPresenter
{
    /// <summary>
    /// Show interaction prompt (e.g., "Press E to interact").
    /// </summary>
    void ShowPrompt(string promptText, IInteractable interactable);

    /// <summary>
    /// Hide the interaction prompt.
    /// </summary>
    void HidePrompt();

    /// <summary>
    /// Show ongoing interaction feedback (e.g., progress bar for channel).
    /// </summary>
    void ShowProgress(float progress);

    /// <summary>
    /// Hide interaction progress.
    /// </summary>
    void HideProgress();

    /// <summary>
    /// Show interaction blocked message.
    /// </summary>
    void ShowBlocked(string reason);
}

/// <summary>
/// Configuration for interaction prompt display.
/// Keeps UI styling self-contained.
/// </summary>
[System.Serializable]
public sealed class InteractionPromptStyle
{
    [SerializeField] public string PromptFormat = "Press {key} to {action}";
    [SerializeField] public Color PromptColor = UnityEngine.Color.white;
    [SerializeField] public float DisplayDistance = 0.5f;
    [SerializeField] public bool ShowAboveObject = true;
    [SerializeField] public float FadeDistance = 20f;
}

/// <summary>
/// Configuration for interaction channeling/casting.
/// </summary>
[System.Serializable]
public sealed class InteractionChannelConfig
{
    [SerializeField] public bool UseChanneling = false;
    [SerializeField] public float ChannelDuration = 1f;
    [SerializeField] public bool CancelOnMove = true;
    [SerializeField] public bool ShowProgressBar = true;
}

/// <summary>
/// Event for interaction system state changes.
/// </summary>
public struct InteractionStateChangedEvent : Extensions.EventBus.IEvent
{
    public IInteractable Interactable;
    public bool IsInRange;
}

/// <summary>
/// Event for interaction completion.
/// </summary>
public struct InteractionPerformedEvent : Extensions.EventBus.IEvent
{
    public System.Guid InteractableId;
    public IInteractable Interactable;
}


