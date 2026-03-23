using System;
using UnityEngine;

namespace Extensions.Dialogue.Runtime
{
    /// <summary>
    /// Context struct passed through the dialogue pipeline.
    /// Contains state, game services, and current execution information.
    /// </summary>
    public readonly struct DialogueContext
    {
        public readonly DialogueState State;
        public readonly IGameServices GameServices;
        public readonly ICommandFactory CommandFactory;
        public readonly Data.NodeId CurrentNode;
        public readonly float ElapsedTime;

        public DialogueContext(DialogueState state, IGameServices gameServices, ICommandFactory commandFactory, Data.NodeId currentNode = default, float elapsedTime = 0f)
        {
            State = state;
            GameServices = gameServices;
            CommandFactory = commandFactory;
            CurrentNode = currentNode;
            ElapsedTime = elapsedTime;
        }
    }

    /// <summary>
    /// Interface for game service integration.
    /// Dialogue system uses this to interact with game systems without tight coupling.
    /// </summary>
    public interface IGameServices
    {
    }
}

