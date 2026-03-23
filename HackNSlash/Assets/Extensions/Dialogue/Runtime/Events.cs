using Extensions.EventBus;

namespace Extensions.Dialogue.Runtime
{
    /// <summary>
    /// Published when dialogue starts.
    /// </summary>
    public struct DialogueStartedEvent : IEvent
    {
        public Data.NodeId StartNode;
    }

    /// <summary>
    /// Published when dialogue advances to next node.
    /// </summary>
    public struct DialogueAdvancedEvent : IEvent
    {
        public Data.NodeId PreviousNode;
        public Data.NodeId NextNode;
    }

    /// <summary>
    /// Published when a choice is selected.
    /// </summary>
    public struct ChoiceSelectedEvent : IEvent
    {
        public int ChoiceIndex;
        public Data.NodeId TargetNode;
    }

    /// <summary>
    /// Published when dialogue ends.
    /// </summary>
    public struct DialogueEndedEvent : IEvent
    {
        public Data.NodeId LastNode;
    }
}

