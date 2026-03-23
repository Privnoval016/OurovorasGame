using System;
using System.Collections.Generic;
using UnityEngine;
using Extensions.Patterns;


/// <summary>
/// Singleton manager for all interactions in the scene.
/// Handles registration, input, and coordination between interactables and UI.
/// </summary>
public sealed class InteractionManager : PersistentSingleton<InteractionManager>, IInteractionManager
{
    [SerializeField] private IInteractionInputHandler _inputHandler;
    [SerializeField] private IInteractionPresenter _presenter;

    private Dictionary<Guid, IInteractable> _interactables = new();
    private IInteractable _focusedInteractable;
    private IInteractable _currentInRangeInteractable;
    private HashSet<Guid> _autoTriggeredInteractables = new();
    private float _channelProgress = 0f;
    private bool _isChanneling = false;

    protected override void Awake()
    {
        base.Awake();

        if (_inputHandler == null)
        {
            _inputHandler = gameObject.AddComponent<KeyboardInteractionInput>();
        }

        if (_presenter == null)
        {
            _presenter = gameObject.AddComponent<DebugInteractionPresenter>();
        }

        _inputHandler.Subscribe(OnInteractPressed);
    }

    private void OnDestroy()
    {
        if (_inputHandler != null)
        {
            _inputHandler.Unsubscribe(OnInteractPressed);
        }
    }

    public void RegisterInteractable(IInteractable interactable)
    {
        if (interactable == null)
        {
            Debug.LogError("[InteractionManager] Cannot register null interactable");
            return;
        }

        _interactables[interactable.InteractableId] = interactable;
        Debug.Log($"[InteractionManager] Registered interactable {interactable.InteractableId}");
    }

    public void UnregisterInteractable(Guid interactableId)
    {
        if (_interactables.Remove(interactableId))
        {
            if (_focusedInteractable?.InteractableId == interactableId)
            {
                SetFocusedInteractable(null);
            }

            _autoTriggeredInteractables.Remove(interactableId);
            Debug.Log($"[InteractionManager] Unregistered interactable {interactableId}");
        }
    }

    public void SetFocusedInteractable(IInteractable interactable)
    {
        if (_focusedInteractable?.InteractableId == interactable?.InteractableId)
            return;

        if (_focusedInteractable != null)
        {
            _presenter.HidePrompt();
        }

        _focusedInteractable = interactable;

        if (_focusedInteractable != null && _focusedInteractable.TriggerType == InteractionTriggerType.Manual)
        {
            _presenter.ShowPrompt($"Press E to interact", _focusedInteractable);
            _inputHandler.EnableInput();
        }
        else
        {
            _inputHandler.DisableInput();
        }
    }

    public void PerformInteraction()
    {
        if (_focusedInteractable == null)
        {
            Debug.LogWarning("[InteractionManager] No focused interactable to interact with");
            return;
        }

        if (!_focusedInteractable.IsAvailable)
        {
            _presenter.ShowBlocked("Interaction not available");
            return;
        }

        PerformInteractionInternal(_focusedInteractable);
    }

    private void PerformInteractionInternal(IInteractable interactable)
    {
        if (interactable.TriggerType == InteractionTriggerType.Manual)
        {
            _presenter.HidePrompt();
        }

        interactable.Interact();

        var evt = new InteractionPerformedEvent
        {
            InteractableId = interactable.InteractableId,
            Interactable = interactable
        };
        Extensions.EventBus.EventBus<InteractionPerformedEvent>.Raise(evt);
    }

    public IInteractable GetFocusedInteractable() => _focusedInteractable;

    public void NotifyInteractableInRange(IInteractable interactable)
    {
        if (_currentInRangeInteractable?.InteractableId == interactable?.InteractableId)
            return;

        _currentInRangeInteractable = interactable;

        if (interactable != null)
        {
            interactable.OnEnterRange();

            if (interactable.TriggerType == InteractionTriggerType.Auto && interactable.IsAvailable)
            {
                if (_autoTriggeredInteractables.Add(interactable.InteractableId))
                {
                    PerformInteractionInternal(interactable);
                }
            }
            else if (interactable.TriggerType == InteractionTriggerType.Manual)
            {
                SetFocusedInteractable(interactable);
            }
        }
    }

    public void NotifyInteractableOutOfRange(IInteractable interactable)
    {
        if (_currentInRangeInteractable?.InteractableId == interactable?.InteractableId)
        {
            _currentInRangeInteractable = null;
        }

        if (_focusedInteractable?.InteractableId == interactable?.InteractableId)
        {
            SetFocusedInteractable(null);
        }

        interactable.OnExitRange();
        _autoTriggeredInteractables.Remove(interactable.InteractableId);
    }

    private void OnInteractPressed()
    {
        if (_focusedInteractable != null && _focusedInteractable.IsAvailable)
        {
            PerformInteraction();
        }
    }

    private void Update()
    {
        if (_isChanneling)
        {
            _channelProgress += Time.deltaTime;
            _presenter.ShowProgress(_channelProgress);
        }
    }
}

/// <summary>
/// Keyboard input handler for interactions.
/// Allows remapping input without changing interaction logic.
/// </summary>
public sealed class KeyboardInteractionInput : MonoBehaviour, IInteractionInputHandler
{
    [SerializeField] private KeyCode _interactKey = KeyCode.E;
    private System.Action _onInteractPressed;
    private bool _inputEnabled = false;

    public void EnableInput() => _inputEnabled = true;
    public void DisableInput() => _inputEnabled = false;

    public void Subscribe(System.Action onInteractPressed)
    {
        _onInteractPressed += onInteractPressed;
    }

    public void Unsubscribe(System.Action onInteractPressed)
    {
        _onInteractPressed -= onInteractPressed;
    }

    private void Update()
    {
        if (!_inputEnabled)
            return;

        if (Input.GetKeyDown(_interactKey))
        {
            _onInteractPressed?.Invoke();
        }
    }
}

/// <summary>
/// Debug presenter that outputs to console.
/// Use as reference for custom UI implementations.
/// </summary>
public sealed class DebugInteractionPresenter : MonoBehaviour, IInteractionPresenter
{
    public void ShowPrompt(string promptText, IInteractable interactable)
    {
        Debug.Log($"[Interaction] {promptText} on {interactable.InteractableId}");
    }

    public void HidePrompt()
    {
        Debug.Log("[Interaction] Prompt hidden");
    }

    public void ShowProgress(float progress)
    {
        Debug.Log($"[Interaction] Progress: {progress:P}");
    }

    public void HideProgress()
    {
        Debug.Log("[Interaction] Progress hidden");
    }

    public void ShowBlocked(string reason)
    {
        Debug.Log($"[Interaction] Blocked: {reason}");
    }
}


