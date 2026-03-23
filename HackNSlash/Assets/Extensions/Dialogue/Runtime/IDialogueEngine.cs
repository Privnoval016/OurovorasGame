using Extensions.Dialogue.Data;

namespace Extensions.Dialogue.Runtime
{
    /// <summary>
    /// Shared interface for dialogue engines (manual and voiceover).
    /// Abstracts away the differences between interactive and automatic dialogue.
    /// </summary>
    public interface IDialogueEngine
    {
        /// <summary>
        /// Current state of the dialogue engine.
        /// </summary>
        enum EngineState { Idle, Playing, Paused }

        EngineState CurrentState { get; }
        NodeId CurrentNode { get; }
        DialogueState State { get; }

        /// <summary>
        /// Start playing a dialogue graph from a specific node.
        /// </summary>
        void PlayFromNode(DialogueGraph graph, NodeId startNodeId);

        /// <summary>
        /// Play from the start node of a graph.
        /// </summary>
        void Play(DialogueGraph graph);

        /// <summary>
        /// Advance to the next node in the dialogue.
        /// </summary>
        void AdvanceToNext();

        /// <summary>
        /// Jump directly to a specific node.
        /// </summary>
        void JumpToNode(NodeId nodeId);

        /// <summary>
        /// Pause dialogue playback.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resume from pause.
        /// </summary>
        void Resume();

        /// <summary>
        /// Stop all dialogue playback.
        /// </summary>
        void StopPlayback();
    }

    /// <summary>
    /// Extension methods for dialogue nodes to enable polymorphic processing.
    /// </summary>
    public interface IDialogueNode
    {
        /// <summary>
        /// Process this node in the given context.
        /// Implementations handle their specific logic without switch statements.
        /// </summary>
        void Process(in DialogueContext context, IDialogueEngine engine, IDialoguePresenter presenter);

        /// <summary>
        /// Get a display name for this node in the editor.
        /// Each node type provides its own display name without switch statements.
        /// </summary>
        string GetDisplayName(NodeId id);
    }
}

