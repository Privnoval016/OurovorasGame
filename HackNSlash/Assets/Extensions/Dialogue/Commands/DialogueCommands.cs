using UnityEngine;

namespace Extensions.Dialogue.Commands
{
    /// <summary>
    /// Empty base command for testing.
    /// </summary>
    public sealed class EmptyCommand : Runtime.IDialogueCommand
    {
        public void Execute(in Runtime.DialogueContext context)
        {
            // No-op
        }
    }

    /// <summary>
    /// Debug command that logs a message.
    /// </summary>
    public sealed class DebugLogCommand : Runtime.IDialogueCommand
    {
        private string _message;

        public DebugLogCommand(string message)
        {
            _message = message;
        }

        public void Execute(in Runtime.DialogueContext context)
        {
            Debug.Log($"[Dialogue] {_message}");
        }
    }

    /// <summary>
    /// Command that sets a boolean state value.
    /// </summary>
    public sealed class SetBoolCommand : Runtime.IDialogueCommand
    {
        private Data.StateKey<bool> _key;
        private bool _value;

        public SetBoolCommand(Data.StateKey<bool> key, bool value)
        {
            _key = key;
            _value = value;
        }

        public void Execute(in Runtime.DialogueContext context)
        {
            context.State.Set(_key, _value);
        }
    }

    /// <summary>
    /// Command that sets an integer state value.
    /// </summary>
    public sealed class SetIntCommand : Runtime.IDialogueCommand
    {
        private Data.StateKey<int> _key;
        private int _value;

        public SetIntCommand(Data.StateKey<int> key, int value)
        {
            _key = key;
            _value = value;
        }

        public void Execute(in Runtime.DialogueContext context)
        {
            context.State.Set(_key, _value);
        }
    }

    /// <summary>
    /// Command that increments an integer state value.
    /// </summary>
    public sealed class IncrementIntCommand : Runtime.IDialogueCommand
    {
        private Data.StateKey<int> _key;
        private int _amount;

        public IncrementIntCommand(Data.StateKey<int> key, int amount = 1)
        {
            _key = key;
            _amount = amount;
        }

        public void Execute(in Runtime.DialogueContext context)
        {
            int currentValue = context.State.Get(_key);
            context.State.Set(_key, currentValue + _amount);
        }
    }
}

