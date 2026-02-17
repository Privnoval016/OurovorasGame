namespace Extensions.CutsceneEngine
{
    /**
     * <summary>
     * The PlayAnimationAction class represents a cutscene action that plays a specified animation on a cutscene actor.
     * It implements the ICutsceneAction interface, allowing it to be executed as part of a cutscene sequence.
     * The class contains a single field, AnimationId, which specifies the identifier of the animation to be played.
     * When the Execute method is called, it uses the provided CutsceneContext to access the animation system and play the specified animation on the given actor.
     * This action can be used to trigger specific animations on characters or objects during cutscenes, enhancing the visual storytelling and immersion of the scene.
     * </summary>
     */
    [System.Serializable]
    public class PlayAnimationAction : ICutsceneAction
    {
        public string AnimationId;

        public void Execute(ICutsceneActor actor, CutsceneContext context)
        {
            context.Animation.PlayAnimation(actor, AnimationId);
        }
    }
}