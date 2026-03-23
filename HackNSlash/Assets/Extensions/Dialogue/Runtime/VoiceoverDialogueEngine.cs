using UnityEngine;
using Sirenix.OdinInspector;
using Extensions.EventBus;

namespace Extensions.Dialogue.Runtime
{
    /// <summary>
    /// Voiceover dialogue engine for auto-playing dialogue in areas.
    /// Supports external graph traversal and smooth dialogue transitions.
    /// Shares common interface with DialogueEngine for consistency.
    /// </summary>
    public sealed class VoiceoverDialogueEngine : MonoBehaviour, IDialogueEngine
    {
        [SerializeField] private DialogueState state = new();
        private Data.DialogueGraph _currentGraph;
        private Data.NodeId _currentNodeId = Data.NodeId.Invalid;
        private IGameServices _gameServices;
        private IDialoguePresenter _presenter;
        private ICommandFactory _commandFactory;
        private DialogueScheduler _scheduler = new();
        private float _elapsedTime;
        private bool _isPlaying;

        IDialogueEngine.EngineState IDialogueEngine.CurrentState => _isPlaying ? IDialogueEngine.EngineState.Playing : IDialogueEngine.EngineState.Idle;
        public Data.NodeId CurrentNode => _currentNodeId;
        public DialogueState State => state;

        public void Initialize(IGameServices gameServices, IDialoguePresenter presenter, ICommandFactory commandFactory)
        {
            _gameServices = gameServices;
            _presenter = presenter;
            _commandFactory = commandFactory;
        }

        void IDialogueEngine.PlayFromNode(Data.DialogueGraph graph, Data.NodeId startNodeId) => PlayFromNode(graph, startNodeId);

        void IDialogueEngine.Play(Data.DialogueGraph graph) => Play(graph);

        void IDialogueEngine.AdvanceToNext() => AdvanceToNext();

        void IDialogueEngine.JumpToNode(Data.NodeId nodeId) => JumpToNode(nodeId);

        void IDialogueEngine.Pause() => Pause();

        void IDialogueEngine.Resume() => Resume();

        void IDialogueEngine.StopPlayback() => StopPlayback();

        public void PlayFromNode(Data.DialogueGraph graph, Data.NodeId startNodeId)
        {
            if (graph == null)
            {
                Debug.LogError("[VoiceoverDialogueEngine] Cannot play null graph");
                return;
            }

            if (_isPlaying)
            {
                StopPlayback();
            }

            _currentGraph = graph;
            _currentNodeId = startNodeId;
            _elapsedTime = 0f;
            _scheduler.Clear();
            _isPlaying = true;

            EventBus<DialogueStartedEvent>.Raise(new DialogueStartedEvent { StartNode = _currentNodeId });
            ProcessCurrentNode();
        }

        public void Play(Data.DialogueGraph graph)
        {
            PlayFromNode(graph, graph.StartNode);
        }

        public void AdvanceToNext()
        {
            if (!_isPlaying)
            {
                Debug.LogWarning("[VoiceoverDialogueEngine] Not currently playing");
                return;
            }

            var currentNode = _currentGraph.GetNode(_currentNodeId);
            if (currentNode?.NextNodes.Length > 0)
            {
                var previousNodeId = _currentNodeId;
                _currentNodeId = currentNode.NextNodes[0];

                EventBus<DialogueAdvancedEvent>.Raise(new DialogueAdvancedEvent
                {
                    PreviousNode = previousNodeId,
                    NextNode = _currentNodeId
                });

                ProcessCurrentNode();
            }
            else
            {
                StopPlayback();
            }
        }

        public void JumpToNode(Data.NodeId nodeId)
        {
            if (!_isPlaying)
            {
                Debug.LogWarning("[VoiceoverDialogueEngine] Not currently playing");
                return;
            }

            var previousNodeId = _currentNodeId;
            _currentNodeId = nodeId;

            EventBus<DialogueAdvancedEvent>.Raise(new DialogueAdvancedEvent
            {
                PreviousNode = previousNodeId,
                NextNode = _currentNodeId
            });

            ProcessCurrentNode();
        }

        public void Pause()
        {
            if (_isPlaying)
            {
                _presenter.Hide();
            }
        }

        public void Resume()
        {
            if (!_isPlaying)
            {
                ProcessCurrentNode();
            }
        }

        public void StopPlayback()
        {
            _isPlaying = false;
            _currentNodeId = Data.NodeId.Invalid;
            _scheduler.Clear();
            _presenter.Hide();

            EventBus<DialogueEndedEvent>.Raise(new DialogueEndedEvent { LastNode = _currentNodeId });
        }

        private void ProcessCurrentNode()
        {
            var node = _currentGraph.GetNode(_currentNodeId);
            if (node == null)
            {
                Debug.LogError($"[VoiceoverDialogueEngine] Node {_currentNodeId} not found");
                ((IDialogueEngine)this).StopPlayback();
                return;
            }

            var context = new DialogueContext(state, _gameServices, _commandFactory, _currentNodeId, _elapsedTime);
            node.Process(in context, this, _presenter);
        }

        private void Update()
        {
            if (!_isPlaying)
                return;

            _elapsedTime += Time.deltaTime;

            var context = new DialogueContext(state, _gameServices, _commandFactory, _currentNodeId, _elapsedTime);
            _scheduler.Update(Time.deltaTime, in context);
        }

        private void OnDestroy()
        {
            _presenter?.Hide();
        }
    }

    /// <summary>
    /// Trigger area that starts voiceover dialogue when player enters.
    /// Automatically transitions to next dialogue area smoothly.
    /// </summary>
    public sealed class VoiceoverDialogueTrigger : MonoBehaviour
    {
        [SerializeField] private Data.DialogueGraph dialogueGraph;
        [SerializeField] private Data.NodeId startNode = new(0);
        [SerializeField] private bool autoPlayOnTrigger = true;
        [SerializeField] private string playerTag = "Player";

        private VoiceoverDialogueEngine _engine;
        private bool _hasTriggered;

        private void Start()
        {
            _engine = FindFirstObjectByType<VoiceoverDialogueEngine>();
            if (_engine == null)
            {
                Debug.LogError("[VoiceoverDialogueTrigger] No VoiceoverDialogueEngine found in scene");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!autoPlayOnTrigger || _hasTriggered || dialogueGraph == null || _engine == null)
                return;

            if (other.CompareTag(playerTag))
            {
                _hasTriggered = true;
                ((IDialogueEngine)_engine).PlayFromNode(dialogueGraph, startNode);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                _hasTriggered = false;
            }
        }

        [Button]
        public void TriggerDialogue()
        {
            if (_engine == null) _engine = FindFirstObjectByType<VoiceoverDialogueEngine>();
            if (dialogueGraph == null) return;
            ((IDialogueEngine)_engine).PlayFromNode(dialogueGraph, startNode);
        }
    }
}


