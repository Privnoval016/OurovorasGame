using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Extensions.Patterns;


/// <summary>
/// Handles interaction detection and triggering for the player.
/// Sits on the player and detects nearby interactables.
/// </summary>
public sealed class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private LayerMask detectionLayer = -1;
    [SerializeField] private bool debugVisualize = false;

    private InteractionManager _manager;
    private Collider[] _detectionBuffer = new Collider[10];

    private void Start()
    {
        _manager = InteractionManager.Instance;
    }

    private void Update()
    {
        DetectNearbyInteractables();
    }

    private void DetectNearbyInteractables()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectionRadius,
            _detectionBuffer,
            detectionLayer
        );

        for (int i = 0; i < count; i++)
        {
            var collider = _detectionBuffer[i];
            if (collider != null && collider.TryGetComponent<IInteractable>(out var interactable))
            {
                if (interactable.TriggerType == InteractionTriggerType.Manual)
                {
                    _manager.SetFocusedInteractable(interactable);
                    return;
                }
            }
        }

        _manager.SetFocusedInteractable(null);
    }

    [Button]
    private void EnableDebugVisualization()
    {
        debugVisualize = !debugVisualize;
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugVisualize)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

