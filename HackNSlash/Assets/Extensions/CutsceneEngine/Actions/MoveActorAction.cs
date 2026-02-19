using UnityEngine;

namespace Extensions.CutsceneEngine
{
    /**
     * <summary>
     * Moves an actor from their current position to a target position over the clip duration.
     * Supports Timeline scrubbing - dragging the playhead will update actor position in real-time.
     * </summary>
     */
    [System.Serializable]
    public class MoveActorAction : CutsceneActionBase
    {
        public Vector3 Target;
        public bool PlayWalkAnimation = true;

        [System.NonSerialized]
        private Vector3 startPosition;
        [System.NonSerialized]
        private bool hasInitialized;
        [System.NonSerialized]
        private bool animationStarted;

        public override void OnEnter(ICutsceneActor actor, CutsceneContext context)
        {
            // Cache start position for interpolation
            startPosition = actor.GetTransform().position;
            hasInitialized = true;
            animationStarted = false;

            // Start walk animation if requested
            if (PlayWalkAnimation && context?.Animation != null)
            {
                context.Animation.PlayAnimation(actor, "Walk");
                animationStarted = true;
            }
        }

        public override void OnUpdate(ICutsceneActor actor, CutsceneContext context, float normalizedTime, float deltaTime)
        {
            // Ensure we have a start position (in case OnEnter wasn't called due to scrubbing)
            if (!hasInitialized)
            {
                startPosition = actor.GetTransform().position;
                hasInitialized = true;
            }
            
            // If normalizedTime is 1 or greater, ensure we set the final position and exit early
            if (normalizedTime >= 1f)
            {
                actor.GetTransform().position = Target;
                return;
            }
            
            // Lerp position based on normalized time (supports scrubbing)
            Vector3 targetPos = Vector3.Lerp(startPosition, Target, normalizedTime);
            actor.GetTransform().position = targetPos;
        }

        public override void OnExit(ICutsceneActor actor, CutsceneContext context)
        {
            // Ensure actor reaches exact target position
            actor.GetTransform().position = Target;

            // Stop walk animation if we started it
            if (animationStarted && context?.Animation != null)
            {
                context.Animation.PlayAnimation(actor, "Idle"); // TODO: Make this configurable
            }
            
            // Reset for next time
            hasInitialized = false;
        }
    }
}

