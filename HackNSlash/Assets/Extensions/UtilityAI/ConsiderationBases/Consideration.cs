using System;
using UnityEngine;

namespace Extensions.UtilityAI.ConsiderationBases
{
    /** <summary>
     * Abstract base for all Considerations. Now a plain <c>[Serializable]</c> class so it can
     * live inline inside an <see cref="Extensions.UtilityAI.AIActionBase"/> via
     * <c>[SerializeReference]</c>, eliminating the need for per-consideration ScriptableObject assets.
     *
     * To share/link a consideration across multiple actions, use <see cref="ConsiderationAsset"/>
     * which wraps one in a ScriptableObject.
     * </summary>
     */
    [Serializable]
    public abstract class Consideration
    {
        /** <summary>Evaluate the context and return a [0,1] utility score.</summary> */
        public abstract float Evaluate(IContextBase context);

        /** <summary>Human-readable display name shown in the editor picker.</summary> */
        public virtual string DisplayName => GetType().Name.Replace("Consideration", "");
    }

    /** <summary>
     * ScriptableObject wrapper that holds a single inline <see cref="Consideration"/>.
     * Use this when you want to share or link the same consideration across multiple action assets.
     * Drag it into the <c>Linked Asset</c> slot on the action's consideration panel.
     * </summary>
     */
    [CreateAssetMenu(fileName = "ConsiderationAsset", menuName = "UtilityAI/Considerations/Linked Asset")]
    public class ConsiderationAsset : ScriptableObject
    {
        [SerializeReference]
        public Consideration consideration;
    }

    // ── Legacy SO base kept so existing concrete SO subclasses still compile ─────
    // New considerations should NOT inherit this — inherit Consideration directly.
    /** <summary>
     * Legacy adapter. New considerations should extend <see cref="Consideration"/> directly
     * as plain serializable classes.
     * </summary>
     */
    public abstract class ConsiderationSo : ScriptableObject
    {
        public abstract float Evaluate(IContextBase context);
        public virtual string DisplayName => GetType().Name.Replace("Consideration", "");
    }

    /** <summary>
     * Inline consideration that delegates to an external <see cref="ConsiderationSo"/> asset.
     * </summary>
     */
    [Serializable]
    public class LinkedSoConsideration : Consideration
    {
        public ConsiderationSo asset;
        public override float Evaluate(IContextBase context) => asset != null ? asset.Evaluate(context) : 0f;
        public override string DisplayName => asset != null ? asset.DisplayName : "Linked (empty)";
    }

    // ── Serialisable struct kept for backward compat ──────────────────────────────
    [Serializable]
    public struct EnumContextKey
    {
        [SerializeField] public string enumTypeName;
        [SerializeField] public int enumValue;

        public object GetKey()
        {
            if (string.IsNullOrEmpty(enumTypeName)) return null;
            Type t = ResolveType();
            return t != null ? Enum.ToObject(t, enumValue) : null;
        }

        public Type ResolveType()
        {
            if (string.IsNullOrEmpty(enumTypeName)) return null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(enumTypeName);
                if (t != null && t.IsEnum) return t;
            }
            return null;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(enumTypeName)) return "(none)";
            Type t = ResolveType();
            return t == null ? $"({enumTypeName}?).{enumValue}" : Enum.GetName(t, enumValue) ?? enumValue.ToString();
        }
    }
}
