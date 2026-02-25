using System;
using System.Collections.Generic;
using UnityEngine;

namespace Extensions.UtilityAI
{
    /** <summary>Orchestrates context updates and best-action selection for a single AI agent.</summary> */
    public class AIBrain<TKey>
    {
        public Context<TKey> Context;
        public AIBrainUser<TKey> User;

        public string LastChosenActionName { get; private set; }
        public float LastChosenUtility { get; private set; }

        // Cached per-action utility scores from the last evaluation tick, used by editor tooling.
        public readonly Dictionary<string, float> LastUtilityScores = new();

        public AIBrain(AIBrainUser<TKey> user)
        {
            User = user;
        }

        public void Initialize()
        {
            Context = new Context<TKey>(this);
        }

        public void UpdateContext()
        {
            ContextPayload<TKey>[] payloads = User.OnContextUpdate();
            foreach (var payload in payloads)
                Context.SetData(payload.Key, payload.Value);
        }

        public void CalculateBestAction()
        {
            AIAction<TKey> bestAction = null;
            float highestUtility = float.MinValue;

            LastUtilityScores.Clear();

            foreach (var action in User.GetActions())
            {
                float utility = action.CalculateUtility(Context);
                LastUtilityScores[action.name] = utility;
                if (utility > highestUtility)
                {
                    highestUtility = utility;
                    bestAction = action;
                }
            }

            if (bestAction != null)
            {
                LastChosenActionName = bestAction.name;
                LastChosenUtility = highestUtility;
                User.ExecuteNewAction(bestAction, Context, highestUtility);
            }
        }
    }

    /**
     * <summary>
     * Non-generic interface exposing brain state to editor tooling without needing to know the concrete <c>TKey</c> type.
     * Automatically implemented by <see cref="AIBrainUser{TKey}"/>.
     * </summary>
     */
    public interface IAIBrainAccessor
    {
        string KeyTypeName { get; }
        string CurrentActionName { get; }
        string LastChosenActionName { get; }
        float LastChosenUtility { get; }
        IReadOnlyList<ActionDebugInfo> GetActionDebugInfos();
        IReadOnlyList<(string key, string value)> GetContextSnapshot();
    }

    /** <summary>Snapshot of a single action's state, consumed by editor tooling.</summary> */
    public struct ActionDebugInfo
    {
        public string ActionName;
        public string ActionTypeName;
        public ConsiderationBases.Consideration Consideration;
        public float LastUtility;
        public bool IsChosen;
    }

    /**
     * <summary>
     * Abstract <see cref="MonoBehaviour"/> that owns an <see cref="AIBrain{TKey}"/> and drives its lifecycle.
     * Also implements <see cref="IAIBrainAccessor"/> so the editor window can inspect any brain generically.
     * </summary>
     */
    public abstract class AIBrainUser<TKey> : MonoBehaviour, IAIBrainAccessor
    {
        public abstract List<AIAction<TKey>> GetActions();
        public abstract Sensor<TKey> GetSensor();
        public abstract ContextPayload<TKey>[] OnContextUpdate();
        public abstract void ExecuteNewAction(AIAction<TKey> action, Context<TKey> context, float highestUtility);

        /** <summary>Must return the live <see cref="AIBrain{TKey}"/> instance owned by this component.</summary> */
        public abstract AIBrain<TKey> GetBrain();

        /** <summary>Name of the action currently executing, or null if idle. Used by editor tooling.</summary> */
        public abstract string CurrentActionName { get; }

        #region IAIBrainAccessor

        public string KeyTypeName => typeof(TKey).Name;
        public string LastChosenActionName => GetBrain()?.LastChosenActionName;
        public float LastChosenUtility => GetBrain()?.LastChosenUtility ?? 0f;

        public IReadOnlyList<ActionDebugInfo> GetActionDebugInfos()
        {
            var brain = GetBrain();
            var result = new List<ActionDebugInfo>();
            foreach (var action in GetActions())
            {
                float utility = 0f;
                brain?.LastUtilityScores.TryGetValue(action.name, out utility);
                result.Add(new ActionDebugInfo
                {
                    ActionName = action.name,
                    ActionTypeName = action.GetType().Name,
                    Consideration = action.consideration,
                    LastUtility = brain != null ? utility : -1f,
                    IsChosen = action.name == LastChosenActionName
                });
            }
            return result;
        }

        public IReadOnlyList<(string key, string value)> GetContextSnapshot()
        {
            var brain = GetBrain();
            if (brain?.Context == null) return Array.Empty<(string, string)>();
            return brain.Context.GetSnapshot();
        }

        #endregion
    }

    public struct ContextPayload<TKey>
    {
        public TKey Key;
        public object Value;

        public ContextPayload(TKey key, object value)
        {
            Key = key;
            Value = value;
        }

        public static implicit operator ContextPayload<TKey>((TKey key, object value) tuple)
        {
            return new ContextPayload<TKey>(tuple.key, tuple.value);
        }
    }
}
