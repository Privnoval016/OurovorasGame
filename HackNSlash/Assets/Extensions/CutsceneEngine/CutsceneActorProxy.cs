using UnityEngine;

namespace Extensions.CutsceneEngine
{
    /**
     * A simple text actor used as a default actor.
     */
    public class CutsceneActorProxy : MonoBehaviour, ICutsceneActor
    {
        private CutsceneActionAdapter adapter;
        
        private void Awake()
        {
            adapter = new CutsceneActionAdapter(this);
        }

        public Transform GetTransform() => transform;

        public CutsceneActionAdapter GetCutsceneAdapter() => adapter;

        public void OnCutsceneEnter() { }

        public void OnCutsceneExit() { }
    }

}