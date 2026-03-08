using System;
using System.Collections.Generic;
using UnityEngine;

namespace Extensions.UtilityAI
{
    /** <summary>Sensor interface — decouples <see cref="EnemyContext"/> from the concrete sensor type.</summary> */
    public interface ISensor
    {
        Transform GetNearestTarget(ContextKey<Transform> key);
    }

    /** <summary>
     * Detects nearby objects using a trigger sphere. Override <see cref="HasDetectionTag"/>
     * to map each <see cref="ContextKey{Transform}"/> to a concrete detection condition.
     * </summary>
     */
    public abstract class Sensor : MonoBehaviour, ISensor
    {
        public float detectionRadius = 10f;

        private readonly HashSet<Transform> _detected = new(10);
        private SphereCollider _col;

        private static readonly Collider[] OverlapBuffer = new Collider[64];

        protected virtual void Awake()
        {
            if (!TryGetComponent(out _col))
                _col = gameObject.AddComponent<SphereCollider>();
            _col.isTrigger = true;
            _col.radius = detectionRadius;

            int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, OverlapBuffer);
            for (int i = 0; i < count; i++)
                TryAdd(OverlapBuffer[i]);
        }

        private void OnTriggerEnter(Collider other) => TryAdd(other);
        private void OnTriggerExit(Collider other) => _detected.Remove(other.transform);

        private void TryAdd(Collider other)
        {
            if (IsValidTarget(other))
                _detected.Add(other.transform);
        }

        /** <summary>Returns true if <paramref name="other"/> should be tracked by this sensor at all.</summary> */
        protected abstract bool IsValidTarget(Collider other);

        /** <summary>Returns true if <paramref name="other"/> matches the given context key.</summary> */
        protected abstract bool HasDetectionTag(ContextKey<Transform> key, Collider other);

        public Transform GetNearestTarget(ContextKey<Transform> key)
        {
            Transform nearest = null;
            float nearestSqr = float.MaxValue;
            Vector3 pos = transform.position;

            foreach (var t in _detected)
            {
                if (t == null) continue;
                if (!t.TryGetComponent(out Collider col)) continue;
                if (!HasDetectionTag(key, col)) continue;
                float sqr = (t.position - pos).sqrMagnitude;
                if (sqr < nearestSqr) { nearestSqr = sqr; nearest = t; }
            }
            return nearest;
        }
    }

    /** <summary>Orchestrates context updates and best-action selection for a single AI agent.</summary> */
    public class AIBrain
    {
        public EnemyContext Context;
        public AIBrainUser User;

        public string LastChosenActionName { get; private set; }
        public float LastChosenUtility { get; private set; }
        public readonly Dictionary<string, float> LastUtilityScores = new();

        public AIBrain(AIBrainUser user) { User = user; }

        public void Initialize()
        {
            Context = new EnemyContext(this);
        }

        public void UpdateContext()
        {
            foreach (var payload in User.OnContextUpdate())
                Context.SetData(payload.Key, payload.Value);
        }

        public void CalculateBestAction()
        {
            AIActionBase bestAction = null;
            float highest = float.MinValue;
            LastUtilityScores.Clear();

            foreach (var action in User.GetActions())
            {
                float u = action.CalculateUtility(Context);
                LastUtilityScores[action.name] = u;
                if (u > highest) { highest = u; bestAction = action; }
            }

            if (bestAction != null)
            {
                LastChosenActionName = bestAction.name;
                LastChosenUtility = highest;
                User.ExecuteNewAction(bestAction, Context, highest);
            }
        }
    }

    /** <summary>
     * Non-generic interface exposing brain state to editor tooling.
     * Implemented automatically by <see cref="AIBrainUser"/>.
     * </summary>
     */
    public interface IAIBrainAccessor
    {
        string CurrentActionName { get; }
        string LastChosenActionName { get; }
        float LastChosenUtility { get; }
        IReadOnlyList<ActionDebugInfo> GetActionDebugInfos();
        IReadOnlyList<(string key, string value)> GetContextSnapshot();
    }

    /** <summary>Snapshot of a single action used by editor tooling.</summary> */
    public struct ActionDebugInfo
    {
        public string ActionName;
        public string ActionTypeName;
        public ConsiderationBases.Consideration Consideration;
        public float LastUtility;
        public bool IsChosen;
    }

    /** <summary>
     * Abstract MonoBehaviour that owns an <see cref="AIBrain"/> and drives its lifecycle.
     * </summary>
     */
    public abstract class AIBrainUser : MonoBehaviour, IAIBrainAccessor
    {
        #region Static Registry

        /** <summary>All currently active <see cref="AIBrainUser"/> instances in the scene.</summary> */
        public static IReadOnlyList<AIBrainUser> All => _registry;
        private static readonly List<AIBrainUser> _registry = new();

        /** <summary>Fired when any <see cref="AIBrainUser"/> is enabled or disabled.</summary> */
        public static event Action RegistryChanged;

        protected virtual void OnEnable()
        {
            _registry.Add(this);
            RegistryChanged?.Invoke();
        }

        protected virtual void OnDisable()
        {
            _registry.Remove(this);
            RegistryChanged?.Invoke();
        }

        #endregion
        public abstract List<AIActionBase> GetActions();
        public abstract ISensor GetSensor();
        public abstract ContextPayload[] OnContextUpdate();
        public abstract void ExecuteNewAction(AIActionBase action, EnemyContext context, float utility);
        public abstract AIBrain GetBrain();
        public abstract string CurrentActionName { get; }

        public string LastChosenActionName => GetBrain()?.LastChosenActionName;
        public float LastChosenUtility => GetBrain()?.LastChosenUtility ?? 0f;

        public IReadOnlyList<ActionDebugInfo> GetActionDebugInfos()
        {
            var brain = GetBrain();
            var result = new List<ActionDebugInfo>();
            foreach (var action in GetActions())
            {
                float u = 0f;
                brain?.LastUtilityScores.TryGetValue(action.name, out u);
                result.Add(new ActionDebugInfo
                {
                    ActionName = action.name,
                    ActionTypeName = action.GetType().Name,
                    Consideration = action.consideration,
                    LastUtility = brain != null ? u : -1f,
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
    }

    /** <summary>Base for all AI actions. Non-generic — keyed by <see cref="ContextKey"/> identity.</summary> */
    public abstract class AIActionBase : ScriptableObject
    {
        [Header("General Settings")]
        [SerializeReference]
        public ConsiderationBases.Consideration consideration;

        /**
         * <summary>
         * When <c>true</c> the brain may replace this action mid-execution if a higher-utility
         * action is scored on the next think tick.
         * Defaults to <c>false</c> — override to <c>true</c> for actions that are designed to
         * be overridden by the brain (e.g. idle stalls, stun holds).
         * </summary>
         */
        public virtual bool IsInterruptible => false;

        public float CalculateUtility(IContextBase context)
        {
            if (consideration == null) return 1f;
            return Mathf.Clamp01(consideration.Evaluate(context));
        }
    }

    /** <summary>
     * Payload carrying one context write. Use <see cref="ContextPayload.From{TValue}"/> to construct.
     * </summary>
     */
    public readonly struct ContextPayload
    {
        public readonly ContextKey Key;
        public readonly object Value;

        private ContextPayload(ContextKey key, object value) { Key = key; Value = value; }

        /** <summary>Create a typed payload with compile-time value type enforcement.</summary> */
        public static ContextPayload From<TValue>(ContextKey<TValue> key, TValue value)
            => new ContextPayload(key, value);

        /** <summary>Implicit from tuple for concise array literals in <c>OnContextUpdate</c>.</summary> */
        public static implicit operator ContextPayload((ContextKey key, object value) t)
            => new ContextPayload(t.key, t.value);
    }
}
