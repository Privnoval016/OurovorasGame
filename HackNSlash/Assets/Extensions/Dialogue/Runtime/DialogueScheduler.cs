using System;
using System.Collections.Generic;

namespace Extensions.Dialogue.Runtime
{
    /// <summary>
    /// Manages scheduled command execution with timing.
    /// </summary>
    public sealed class DialogueScheduler
    {
        [Serializable]
        private struct ScheduledCommand
        {
            public float Time;
            public IDialogueCommand Command;
        }

        private List<ScheduledCommand> _queue = new();
        private float _elapsedTime = 0f;

        public void Schedule(IDialogueCommand command, float delay = 0f)
        {
            _queue.Add(new ScheduledCommand { Time = _elapsedTime + delay, Command = command });
        }

        public void Update(float deltaTime, in DialogueContext context)
        {
            _elapsedTime += deltaTime;

            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                if (_queue[i].Time <= _elapsedTime)
                {
                    var cmd = _queue[i];
                    cmd.Command.Execute(in context);
                    _queue.RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            _queue.Clear();
            _elapsedTime = 0f;
        }

        public bool HasPending => _queue.Count > 0;
    }

    /// <summary>
    /// Interface for executable dialogue commands.
    /// </summary>
    public interface IDialogueCommand
    {
        void Execute(in DialogueContext context);
    }
}

