using System.Collections.Generic;
using UnityEngine;

namespace Extensions.UtilityAI
{
    /** <summary>
     * Stores all runtime context values for one AI agent.
     * The dictionary is keyed by <see cref="ContextKey"/> object identity —
     * no strings, no enums, no casting.
     * </summary>
     * <remarks>
     * Typed API (preferred):
     * <code>
     * float hp     = context.Get(EnemyContextKeys.SelfHealthNorm);
     * bool aggro   = context.Get(EnemyContextKeys.IsAggro);
     * Transform p  = context.GetTarget(EnemyContextKeys.Player);
     * context.Set(EnemyContextKeys.SelfHealthNorm, 0.8f);
     * </code>
     * </remarks>
     */
    public class EnemyContext : IContextBase
    {
        public AIBrain Brain;
        public ISensor Sensor;

        private readonly Dictionary<ContextKey, object> _data = new(ContextKeyRefComparer.Instance);

        public EnemyContext(AIBrain brain)
        {
            Brain = brain;
            Sensor = brain.User.GetSensor();
        }

        #region Typed API

        /** <summary>Read a strongly-typed value. Returns <c>default</c> if the key has not been written.</summary> */
        public TValue Get<TValue>(ContextKey<TValue> key)
        {
            if (key == null) return default;
            return _data.TryGetValue(key, out object raw) && raw is TValue v ? v : default;
        }

        /** <summary>Write a strongly-typed value.</summary> */
        public void Set<TValue>(ContextKey<TValue> key, TValue value)
        {
            if (key != null) _data[key] = value;
        }

        /** <summary>Look up a sensor target Transform by a <see cref="ContextKey{Transform}"/>.</summary> */
        public Transform GetTarget(ContextKey<Transform> key)
        {
            if (key == null || Sensor == null) return null;
            return Sensor.GetNearestTarget(key);
        }

        #endregion

        #region IContextBase (used by Consideration infrastructure)

        public TValue GetData<TValue>(object key)
        {
            if (key is not ContextKey k) return default;
            return _data.TryGetValue(k, out object raw) && raw is TValue v ? v : default;
        }

        public bool SetData<TValue>(object key, TValue value)
        {
            if (key is not ContextKey k) return false;
            _data[k] = value;
            return true;
        }

        public Transform GetSensorTarget(object key)
        {
            if (key is not ContextKey<Transform> k || Sensor == null) return null;
            return Sensor.GetNearestTarget(k);
        }

        public Transform GetBrainTransform() => Brain?.User.transform;

        #endregion

        /** <summary>Editor snapshot — all live context values as (displayName, valueString) pairs.</summary> */
        public IReadOnlyList<(string key, string value)> GetSnapshot()
        {
            var result = new List<(string, string)>(_data.Count);
            foreach (var kvp in _data)
                result.Add((kvp.Key?.ToString() ?? "(null)", kvp.Value?.ToString() ?? "null"));
            return result;
        }
    }

    /** <summary>
     * Non-generic interface used by <see cref="ConsiderationBases.Consideration"/> subclasses
     * which cannot know the concrete key type at compile time.
     * </summary>
     */
    public interface IContextBase
    {
        TValue GetData<TValue>(object key);
        bool SetData<TValue>(object key, TValue value);
        // Accepts either a ContextKey<Transform> or a string InternalId.
        Transform GetSensorTarget(object key);
        Transform GetBrainTransform();
    }
}