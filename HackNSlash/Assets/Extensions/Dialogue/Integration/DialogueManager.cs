using Extensions.Dialogue.Runtime;
using Extensions.Dialogue.Data;
using Extensions.Patterns;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Extensions.Dialogue.Integration
{
    /// <summary>
    /// Main dialogue manager singleton for interactive (manual) dialogue.
    /// Handles initialization and management of manual player-triggered dialogues.
    /// </summary>
    public sealed class DialogueManager : PersistentSingleton<DialogueManager>
    {
        [SerializeField] private DialogueAssetManager assetManager;

        private IDialoguePresenter _presenter;
        private IGameServices _gameServices;
        private DialogueEngine _engine;
        private DefaultCommandFactory _commandFactory;
        private DefaultTextFormatter _textFormatter;

        protected override void Awake()
        {
            base.Awake();

            if (_engine != null)
                return;

            if (_presenter == null)
            {
                _presenter = new Presentation.DebugDialoguePresenter();
            }

            if (_gameServices == null)
            {
                _gameServices = new DefaultGameServices();
            }

            _commandFactory = new DefaultCommandFactory();
            RegisterDefaultCommands();

            if (assetManager != null)
            {
                _textFormatter = new DefaultTextFormatter(assetManager.GetLocalizationTable());
            }
            else
            {
                _textFormatter = new DefaultTextFormatter();
            }

            _engine = gameObject.AddComponent<DialogueEngine>();
            _engine.Initialize(_gameServices, _presenter, _commandFactory, _textFormatter);
        }

        /// <summary>
        /// Start a dialogue with a given graph.
        /// </summary>
        [Button]
        public void StartDialogue(DialogueGraph graph)
        {
            if (graph == null)
            {
                Debug.LogError("[DialogueManager] Cannot start null graph");
                return;
            }

            _engine.StartDialogue(graph);
        }

        /// <summary>
        /// Start a dialogue by name from the asset manager.
        /// </summary>
        public void StartDialogueByName(string graphName)
        {
            if (assetManager == null)
            {
                Debug.LogError("[DialogueManager] No asset manager assigned");
                return;
            }

            var graph = assetManager.GetGraph(graphName);
            if (graph == null)
            {
                Debug.LogError($"[DialogueManager] Graph '{graphName}' not found");
                return;
            }

            StartDialogue(graph);
        }

        /// <summary>
        /// Advance to the next node.
        /// </summary>
        public void Advance()
        {
            _engine.AdvanceToNext();
        }

        /// <summary>
        /// Select a choice.
        /// </summary>
        public void SelectChoice(int index)
        {
            _engine.SelectChoice(index);
        }

        /// <summary>
        /// End the current dialogue.
        /// </summary>
        public void EndDialogue()
        {
            _engine.StopPlayback();
        }

        public IDialogueEngine.EngineState CurrentState
        {
            get
            {
                if (_engine == null)
                    return IDialogueEngine.EngineState.Idle;

                IDialogueEngine engine = _engine;
                return engine.CurrentState;
            }
        }

        public NodeId CurrentNode
        {
            get
            {
                if (_engine == null)
                    return NodeId.Invalid;

                IDialogueEngine engine = _engine;
                return engine.CurrentNode;
            }
        }

        private void RegisterDefaultCommands()
        {
            _commandFactory.Register<CommandData, Commands.EmptyCommand>(
                _ => new Commands.EmptyCommand()
            );
        }
    }

    /// <summary>
    /// Default implementation of IGameServices.
    /// Extend this to add game system integration (Inventory, Quests, etc).
    /// </summary>
    public sealed class DefaultGameServices : IGameServices
    {
    }
}







