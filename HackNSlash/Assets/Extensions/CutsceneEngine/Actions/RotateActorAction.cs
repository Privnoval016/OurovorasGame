using UnityEngine;

namespace Extensions.CutsceneEngine
{
    /**
     * <summary>
     * The RotateActorAction class represents a cutscene action that rotates an actor by a specified Euler rotation over a given duration.
     * It implements the ICutsceneAction interface, allowing it to be executed as part of a cutscene sequence.
     * The Execute method uses the CutsceneContext's Motion system to apply the rotation to the actor smoothly over the specified duration.
     * This action can be used to create dynamic and visually engaging cutscenes by rotating actors in response to events or triggers within the cutscene.
     * </summary>
     */
    [System.Serializable]
    public class RotateActorAction : ICutsceneAction
    {
        public Vector3 EulerRotation;
        public float Duration = 1f;

        public void Execute(ICutsceneActor actor, CutsceneContext context)
        {
            context.Motion.Rotate(actor, Quaternion.Euler(EulerRotation), Duration);
        }
    }
}