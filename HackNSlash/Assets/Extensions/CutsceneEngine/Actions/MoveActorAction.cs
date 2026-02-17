using UnityEngine;

namespace Extensions.CutsceneEngine
{
    /**
     * <summary>
     * The MoveActorAction class represents a cutscene action that moves an actor to a specified position over a given duration.
     * This class implements the ICutsceneAction interface, allowing it to be executed as part of a cutscene sequence.
     * When executed, it uses the MotionSystem from the CutsceneContext to move the actor to the target position smoothly over the specified duration.
     * This action can be used to create dynamic and engaging cutscenes where actors need to move to specific locations as part of the narrative.
     * </summary>
     */
    [System.Serializable]
    public class MoveActorAction : ICutsceneAction
    {
        public Vector3 Target;
        public float Duration = 1f;
        public bool PlayWalkAnimation = true;

        public void Execute(ICutsceneActor actor, CutsceneContext context)
        {
            if (PlayWalkAnimation)
                context.Animation.PlayAnimation(actor, "Walk"); // TODO: This should be more flexible

            context.Motion.Move(actor, Target, Duration);
        }
    }
}