using System;
using System.Collections.Generic;
using Extensions.EventBus;
using UnityEngine;

namespace Extensions.Dialogue.Runtime
{
    /// <summary>
    /// Core dialogue execution engine.
    /// Manages graph traversal, node evaluation, command execution, and state transitions.
    /// Implements IDialogueEngine for consistency with VoiceoverDialogueEngine.
    /// </summary>
    public sealed class DialogueEngine : MonoBehaviour, IDialogueEngine
    {
        [SerializeField] private Data.DialogueGraph _currentGraph;
        [SerializeField] private DialogueState state = new();
        
        private IDialogueEngine.EngineState _engineState = IDialogueEngine.EngineState.Idle;
        private Data.NodeId _currentNodeId = Data.NodeId.Invalid;
        private IGameServices _gameServices;
        private IDialoguePresenter _presenter;
        private ICommandFactory _commandFactory;
        private ITextFormatter _textFormatter;
        private DialogueScheduler _scheduler = new();
        private float _elapsedTime;

        IDialogueEngine.EngineState IDialogueEngine.CurrentState => _engineState;
        public Data.NodeId CurrentNode => _currentNodeId;
        public DialogueState State => state;

        /// <summary>
        /// Initialize the engine with dependencies.
        /// </summary>
        public void Initialize(IGameServices gameServices, IDialoguePresenter presenter, ICommandFactory commandFactory, ITextFormatter textFormatter = null)
        {
            _gameServices = gameServices;
            _presenter = presenter;
            _commandFactory = commandFactory;
            _textFormatter = textFormatter ?? new DefaultTextFormatter();
        }

        /// <summary>
        /// Start dialogue with a given graph.
        /// </summary>
        public void StartDialogue(Data.DialogueGraph graph)
        {
            ((IDialogueEngine)this).PlayFromNode(graph, graph.StartNode);
        }

        void IDialogueEngine.PlayFromNode(Data.DialogueGraph graph, Data.NodeId startNodeId) => PlayFromNode(graph, startNodeId);

        void IDialogueEngine.Play(Data.DialogueGraph graph) => Play(graph);

        void IDialogueEngine.AdvanceToNext() => AdvanceToNext();

        void IDialogueEngine.JumpToNode(Data.NodeId nodeId) => JumpToNode(nodeId);

        void IDialogueEngine.Pause() => Pause();

        void IDialogueEngine.Resume() => Resume();

        void IDialogueEngine.StopPlayback() => StopPlayback();

        /// <summary>
        /// Play from a specific node.
        /// </summary>
        public void PlayFromNode(Data.DialogueGraph graph, Data.NodeId startNodeId)
        {
            if (_engineState != IDialogueEngine.EngineState.Idle)
            {
                Debug.LogWarning("[DialogueEngine] Cannot start dialogue while engine is already playing");
                return;
            }

            _currentGraph = graph;
            _currentNodeId = startNodeId;
            _engineState = IDialogueEngine.EngineState.Playing;
            _elapsedTime = 0f;
            _scheduler.Clear();

            EventBus<DialogueStartedEvent>.Raise(new DialogueStartedEvent { StartNode = _currentNodeId });
            ProcessCurrentNode();
        }

        /// <summary>
        /// Play from start node.
        /// </summary>
        public void Play(Data.DialogueGraph graph)
        {
            PlayFromNode(graph, graph.StartNode);
        }

        /// <summary>
        /// Advance to the next node.
        /// </summary>
        public void AdvanceToNext()
        {
            if (_engineState != IDialogueEngine.EngineState.Playing)
            {
                Debug.LogWarning("[DialogueEngine] Cannot advance: engine not playing");
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

        /// <summary>
        /// Jump directly to a specific node.
        /// </summary>
        public void JumpToNode(Data.NodeId nodeId)
        {
            if (_engineState == IDialogueEngine.EngineState.Idle)
            {
                Debug.LogWarning("[DialogueEngine] Cannot jump: engine not playing");
                return;
            }

            var previousNodeId = _currentNodeId;
            _currentNodeId = nodeId;
            _engineState = IDialogueEngine.EngineState.Playing;

            EventBus<DialogueAdvancedEvent>.Raise(new DialogueAdvancedEvent
            {
                PreviousNode = previousNodeId,
                NextNode = _currentNodeId
            });

            ProcessCurrentNode();
        }

        /// <summary>
        /// Pause playback.
        /// </summary>
        public void Pause()
        {
            _engineState = IDialogueEngine.EngineState.Paused;
            _presenter.Hide();
        }

        /// <summary>
        /// Resume from pause.
        /// </summary>
        public void Resume()
        {
            if (_engineState == IDialogueEngine.EngineState.Paused)
            {
                _engineState = IDialogueEngine.EngineState.Playing;
                ProcessCurrentNode();
            }
        }

        /// <summary>
        /// Select a choice from a choice node.
        /// </summary>
        public void SelectChoice(int index)
        {
            if (_engineState != IDialogueEngine.EngineState.Playing)
            {
                Debug.LogWarning("[DialogueEngine] Cannot select choice: engine not playing");
                return;
            }

            var currentNode = _currentGraph.GetNode(_currentNodeId) as Data.ChoiceNode;
            if (currentNode == null)
            {
                Debug.LogError("[DialogueEngine] Current node is not a choice node");
                return;
            }

            if (index < 0 || index >= currentNode.Options.Length)
            {
                Debug.LogError($"[DialogueEngine] Choice index {index} out of range");
                return;
            }

            var option = currentNode.Options[index];
            var previousNodeId = _currentNodeId;
            _currentNodeId = option.NextNode;

            EventBus<ChoiceSelectedEvent>.Raise(new ChoiceSelectedEvent
            {
                ChoiceIndex = index,
                TargetNode = _currentNodeId
            });

            ProcessCurrentNode();
        }

        /// <summary>
        /// End dialogue and return to idle state.
        /// </summary>
        public void StopPlayback()
        {
            var lastNode = _currentNodeId;
            _engineState = IDialogueEngine.EngineState.Idle;
            _currentNodeId = Data.NodeId.Invalid;
            _scheduler.Clear();
            _presenter.Hide();

            EventBus<DialogueEndedEvent>.Raise(new DialogueEndedEvent { LastNode = lastNode });
        }

        /// <summary>
        /// Process the current node polymorphically without switch statements.
        /// </summary>
        private void ProcessCurrentNode()
        {
            var node = _currentGraph.GetNode(_currentNodeId);
            if (node == null)
            {
                Debug.LogError($"[DialogueEngine] Node {_currentNodeId} not found in graph");
                ((IDialogueEngine)this).StopPlayback();
                return;
            }

            var context = new DialogueContext(state, _gameServices, _commandFactory, _currentNodeId, _elapsedTime);

            // Polymorphic processing - node handles itself based on type
            node.Process(in context, this, _presenter);
            _engineState = IDialogueEngine.EngineState.Playing;
        }

        private void Update()
        {
            if (_engineState == IDialogueEngine.EngineState.Idle || _engineState == IDialogueEngine.EngineState.Paused)
                return;

            _elapsedTime += Time.deltaTime;

            var context = new DialogueContext(state, _gameServices, _commandFactory, _currentNodeId, _elapsedTime);
            _scheduler.Update(Time.deltaTime, in context);
        }
    }

    /// <summary>
    /// Factory for creating IDialogueCommand instances from CommandData.
    /// </summary>
    public interface ICommandFactory
    {
        IDialogueCommand Create(Data.CommandData commandData);
    }

    /// <summary>
    /// Default command factory with registration support.
    /// </summary>
    public sealed class DefaultCommandFactory : ICommandFactory
    {
        private Dictionary<System.Type, System.Func<Data.CommandData, IDialogueCommand>> _registry = new();

        public void Register<TData, TCommand>(System.Func<TData, TCommand> factory) where TData : Data.CommandData where TCommand : IDialogueCommand
        {
            _registry[typeof(TData)] = cmdData => factory((TData)cmdData);
        }

        public IDialogueCommand Create(Data.CommandData commandData)
        {
            if (commandData == null)
                return null;

            var type = commandData.GetType();
            if (_registry.TryGetValue(type, out var factory))
            {
                return factory(commandData);
            }

            Debug.LogWarning($"[DefaultCommandFactory] No factory registered for {type.Name}");
            return null;
        }
    }

    /// <summary>
    /// Interface for formatting dialogue text with variables and styling.
    /// </summary>
    public interface ITextFormatter
    {
        FormattedText Format(Data.TextKey key, in DialogueContext context);
    }

    /// <summary>
    /// Simple default text formatter (no interpolation).
    /// </summary>
    public sealed class DefaultTextFormatter : ITextFormatter
    {
        private Localization.LocalizationTable _localizationTable;

        public DefaultTextFormatter(Localization.LocalizationTable localizationTable = null)
        {
            _localizationTable = localizationTable;
        }

        public FormattedText Format(Data.TextKey key, in DialogueContext context)
        {
            string text = "";

            if (_localizationTable != null)
            {
                text = _localizationTable.GetText(key, Application.systemLanguage);
            }

            return new FormattedText(text);
        }
    }
}

