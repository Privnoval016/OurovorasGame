using System;
using UnityEngine;

namespace Extensions.Dialogue.Data
{
    /// <summary>
    /// Base class for all dialogue nodes in a graph.
    /// </summary>
    [Serializable]
    public abstract class DialogueNode : Runtime.IDialogueNode
    {
        [SerializeField] public NodeId Id;
        [SerializeField] public NodeId[] NextNodes = Array.Empty<NodeId>();

        /// <summary>
        /// Process this node polymorphically without switch statements.
        /// </summary>
        public abstract void Process(in Runtime.DialogueContext context, Runtime.IDialogueEngine engine, Runtime.IDialoguePresenter presenter);

        /// <summary>
        /// Get a display name for this node in the editor.
        /// </summary>
        public abstract string GetDisplayName(NodeId id);
    }

    /// <summary>
    /// A line node represents a single line of dialogue spoken by a character.
    /// It can have conditions, commands, and branching to next nodes.
    /// </summary>
    [Serializable]
    public sealed class LineNode : DialogueNode
    {
        [SerializeField] public TextKey TextKey;
        [SerializeField] public SpeakerId Speaker = SpeakerId.None;
        [SerializeField] public StyleId Style = StyleId.Default;
        
        [SerializeField] public string[] ConditionSerializedReferences = Array.Empty<string>();
        [SerializeField] public CommandData[] Commands = Array.Empty<CommandData>();

        public override void Process(in Runtime.DialogueContext context, Runtime.IDialogueEngine engine, Runtime.IDialoguePresenter presenter)
        {
            // Execute commands
            if (Commands != null && context.CommandFactory != null)
            {
                foreach (var cmdData in Commands)
                {
                    var cmd = context.CommandFactory.Create(cmdData);
                    if (cmd != null)
                    {
                        cmd.Execute(in context);
                    }
                }
            }

            // Format and display text
            var formattedText = new Runtime.DefaultTextFormatter().Format(TextKey, in context);
            presenter.ShowLine(formattedText, Speaker, Style);

            // Auto advance
            engine.AdvanceToNext();
        }

        public override string GetDisplayName(NodeId id) => $"Line #{id.Value}";
    }

    /// <summary>
    /// A choice node presents multiple options to the player.
    /// Each option can have its own conditions and leads to a different node.
    /// </summary>
    [Serializable]
    public sealed class ChoiceNode : DialogueNode
    {
        [SerializeField] public ChoiceOption[] Options = Array.Empty<ChoiceOption>();

        public override void Process(in Runtime.DialogueContext context, Runtime.IDialogueEngine engine, Runtime.IDialoguePresenter presenter)
        {
            // For voiceover dialogue, auto advance through choices
            // For interactive dialogue, this would show UI
            if (NextNodes.Length > 0)
            {
                engine.JumpToNode(NextNodes[0]);
            }
            else
            {
                engine.StopPlayback();
            }
        }

        public override string GetDisplayName(NodeId id) => $"Choice #{id.Value}";
    }

    /// <summary>
    /// A single choice option within a ChoiceNode.
    /// </summary>
    [Serializable]
    public sealed class ChoiceOption
    {
        [SerializeField] public TextKey TextKey;
        [SerializeField] public NodeId NextNode;
        [SerializeField] public string[] ConditionSerializedReferences = Array.Empty<string>();
    }
}

