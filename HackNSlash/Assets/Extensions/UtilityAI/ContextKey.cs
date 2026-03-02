using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Extensions.UtilityAI
{
    /** <summary>
     * Non-generic base for all context keys. Used as the dictionary key type in
     * <see cref="EnemyContext"/> so the infrastructure never touches raw strings.
     * Equality is reference-based — two keys are the same only if they are the same object,
     * which is guaranteed by declaring keys as <c>static readonly</c> fields.
     * </summary>
     */
    public abstract class ContextKey
    {
        /** <summary>Human-readable label shown in editor tooling.</summary> */
        public readonly string DisplayName;

        /** <summary>Category label used for grouping in editor dropdowns.</summary> */
        public readonly string Category;

        /** <summary>
         * Stable identifier used only by serialisation and editor tooling.
         * Never used directly in game code.
         * </summary>
         */
        public readonly string InternalId;

        protected ContextKey(string internalId, string displayName, string category)
        {
            InternalId = internalId;
            DisplayName = displayName ?? internalId;
            Category = category ?? string.Empty;
        }

        public override string ToString() => DisplayName;
    }

    /** <summary>
     * Strongly-typed context key. Declare as <c>static readonly</c> fields in a class
     * tagged with <see cref="AIContextKeyAttribute"/>.
     * </summary>
     * <typeparam name="TValue">The value type stored under this key.</typeparam>
     */
    public sealed class ContextKey<TValue> : ContextKey
    {
        public ContextKey(string internalId, string displayName = null, string category = null)
            : base(internalId, displayName ?? internalId, category) { }
    }

    /** <summary>
     * Reference-equality comparer for <see cref="ContextKey"/> dictionary keys.
     * Two keys are equal only if they are the exact same object.
     * </summary>
     */
    public sealed class ContextKeyRefComparer : IEqualityComparer<ContextKey>
    {
        public static readonly ContextKeyRefComparer Instance = new();
        private ContextKeyRefComparer() { }

        public bool Equals(ContextKey x, ContextKey y) => ReferenceEquals(x, y);
        public int GetHashCode(ContextKey obj) => RuntimeHelpers.GetHashCode(obj);
    }
}



