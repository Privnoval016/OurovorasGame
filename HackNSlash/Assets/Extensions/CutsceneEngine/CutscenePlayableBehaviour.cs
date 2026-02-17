using UnityEngine.Playables;

namespace Extensions.CutsceneEngine
{
    /**
     * <summary>
     * The CutscenePlayableBehaviour class is a custom PlayableBehaviour used in Unity's Playable system to execute cutscene actions.
     * It contains references to a CutsceneAction, an optional explicit actor, and the CutsceneContext.
     * The ProcessFrame method is overridden to execute the specified cutscene action on the target actor when the playable is played.
     * This class allows for the integration of cutscene actions into Unity's timeline and playable system, enabling more complex and dynamic cutscenes.
     * </summary>
     */
    public class CutscenePlayableBehaviour : PlayableBehaviour
    {
        public CutsceneActionReference actionReference;
        public ICutsceneActor explicitActor;

        private bool executed;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (executed) return;

            var resolver = playable.GetGraph().GetResolver();
            if (resolver is not PlayableDirector director) return;

            var wrapper = director.GetComponent<CutsceneDirector>();
            if (wrapper == null) return;

            var context = wrapper.Context;

            ICutsceneActor actor = explicitActor ?? playerData as ICutsceneActor;
            if (actor == null) return;

            actionReference.Action.Execute(actor, context);

            executed = true;
        }

    }
}