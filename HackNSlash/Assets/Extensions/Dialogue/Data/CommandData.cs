using System;
using UnityEngine;

namespace Extensions.Dialogue.Data
{
    /// <summary>
    /// Base class for dialogue commands. All dialogue commands should derive from this.
    /// Commands are data-only and are converted to executable IDialogueCommand instances by a factory.
    /// </summary>
    [Serializable]
    public abstract class CommandData
    {
    }

    /// <summary>
    /// Generic command data that can execute arbitrary code through a serialized action ID.
    /// Used as a fallback for custom dialogue commands.
    /// </summary>
    [Serializable]
    public sealed class CustomCommandData : CommandData
    {
        [SerializeField] public int ActionId;
        [TextArea] [SerializeField] public string CustomData;
    }
}

