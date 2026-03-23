using Sirenix.OdinInspector;
using UnityEngine;

namespace Extensions.Dialogue.Integration
{
    /// <summary>
    /// Trigger point for starting dialogue from the scene.
    /// Attach to NPCs or objects that should initiate dialogue.
    /// </summary>
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [SerializeField, InlineEditor] private Data.DialogueGraph graph;
        [SerializeField] private bool triggerOnStart = false;
        [SerializeField] private string triggerTag = "Player";

        private bool _hasTriggered;
        private DialogueManager _dialogueManager;

        private void Start()
        {
            if (_dialogueManager == null)
            {
                _dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (triggerOnStart && graph != null)
            {
                StartDialogue();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (graph == null || _hasTriggered)
                return;

            if (other.CompareTag(triggerTag))
            {
                StartDialogue();
                _hasTriggered = true;
            }
        }

        [Button]
        public void StartDialogue()
        {
            if (graph == null)
            {
                Debug.LogError("[DialogueTrigger] No graph assigned");
                return;
            }

            if (_dialogueManager == null)
            {
                Debug.LogError("[DialogueTrigger] No DialogueManager found");
                return;
            }

            _dialogueManager.StartDialogue(graph);
        }

        public void ResetTrigger()
        {
            _hasTriggered = false;
        }
    }
}


