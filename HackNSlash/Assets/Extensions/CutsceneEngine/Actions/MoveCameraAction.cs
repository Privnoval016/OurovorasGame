using System;
using UnityEngine;

namespace Extensions.CutsceneEngine
{
    /**
     * <summary>
     * Moves the camera from its current position to a target position over the clip duration.
     * Supports Timeline scrubbing - dragging the playhead will update camera position in real-time.
     /// </summary>
     */
    [Serializable]
    public class MoveCameraAction : CutsceneActionBase
    {
        [Tooltip("Target position to move the camera to")]
        public Vector3 TargetPosition;
        
        [Tooltip("If true, uses a GameObject's position as the target")]
        public bool UseTargetObject = false;
        
        [Tooltip("Target object to move camera to (used when Use Target Object is true)")]
        public GameObject TargetObject;

        private Vector3 startPosition;
        private Transform cameraTransform;

        public override void OnEnter(ICutsceneActor actor, CutsceneContext context)
        {
            if (context?.Camera == null)
            {
                Debug.LogWarning("MoveCameraAction: No camera system available in context");
                return;
            }

            // Cache start position for interpolation
            // Note: This assumes the camera system exposes a way to get the camera transform
            // For now, we'll use a simple approach
            cameraTransform = Camera.main?.transform;
            if (cameraTransform != null)
            {
                startPosition = cameraTransform.position;
            }
        }

        public override void OnUpdate(ICutsceneActor actor, CutsceneContext context, float normalizedTime, float deltaTime)
        {
            if (cameraTransform == null) return;

            Vector3 target = UseTargetObject && TargetObject != null 
                ? TargetObject.transform.position 
                : TargetPosition;
            
            if (normalizedTime >= 1f)
            {
                cameraTransform.position = target;
                return;
            }

            // Lerp position based on normalized time (supports scrubbing)
            cameraTransform.position = Vector3.Lerp(startPosition, target, normalizedTime);
        }

        public override void OnExit(ICutsceneActor actor, CutsceneContext context)
        {
            if (cameraTransform == null) return;

            // Ensure camera reaches exact target position
            Vector3 target = UseTargetObject && TargetObject != null 
                ? TargetObject.transform.position 
                : TargetPosition;
            
            cameraTransform.position = target;
        }
    }
}

