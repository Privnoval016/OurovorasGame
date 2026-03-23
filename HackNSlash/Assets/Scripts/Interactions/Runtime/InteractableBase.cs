using System;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;


/// <summary>
/// Base component for interactable objects in the scene.
/// Handles registration with interaction manager and trigger detection.
/// </summary>
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] protected InteractionTriggerType triggerType = InteractionTriggerType.Manual;
    [SerializeField] protected float interactionRange = 2f;
    [SerializeField] protected InteractionPromptStyle promptStyle = new();
    [SerializeField] protected InteractionChannelConfig channelConfig = new();

    protected Guid _interactableId = Guid.NewGuid();
    private bool _isInRange = false;
    private InteractionManager _manager;

    public virtual Guid InteractableId => _interactableId;
    public virtual InteractionTriggerType TriggerType => triggerType;
    public virtual bool IsAvailable => isActiveAndEnabled;

    protected virtual void OnEnable()
    {
        if (_manager == null)
        {
            _manager = InteractionManager.Instance;
        }

        _manager.RegisterInteractable(this);
    }

    protected virtual void OnDisable()
    {
        if (_manager != null)
        {
            _manager.UnregisterInteractable(_interactableId);
        }

        if (_isInRange)
        {
            _manager?.NotifyInteractableOutOfRange(this);
            _isInRange = false;
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (triggerType == InteractionTriggerType.Auto || triggerType == InteractionTriggerType.Manual)
        {
            if (other.CompareTag("Player"))
            {
                _isInRange = true;
                _manager.NotifyInteractableInRange(this);
            }
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && _isInRange)
        {
            _isInRange = false;
            _manager.NotifyInteractableOutOfRange(this);
        }
    }

    public abstract void Interact();

    public virtual void OnEnterRange()
    {
        // Override in derived class
    }

    public virtual void OnExitRange()
    {
        // Override in derived class
    }

    [Button]
    protected void TestInteraction()
    {
        Interact();
    }
}

/// <summary>
/// Simple interactable that triggers a UnityEvent.
/// Useful for quick setup or prototyping.
/// </summary>
public sealed class UnityEventInteractable : InteractableBase
{
    [SerializeField] private UnityEvent _onInteract = new();

    public override void Interact()
    {
        _onInteract.Invoke();
    }
}


