using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Extensions.UtilityAI
{
    /** <summary>
     * Serialisable inspector field for picking a <see cref="ContextKey{TValue}"/> in the Inspector.
     * Stores the internal ID for serialisation — the actual key object is resolved at runtime
     * by matching the stored ID against keys discovered via reflection across all
     * <see cref="AIContextKeyAttribute"/>-tagged static classes.
     * The custom <c>ContextKeyFieldDrawer</c> renders this as a grouped dropdown; users never
     * type strings directly.
     * </summary>
     */
    [Serializable]
    public struct ContextKeyField
    {
        [SerializeField] internal string internalId;
        [SerializeField] internal string displayName;

        // ── Resolution cache — built once, never rebuilt at runtime ──────────────
        // Maps internalId → ContextKey object. Populated lazily on first Resolve() call.
        private static Dictionary<string, ContextKey> _resolveCache;

        private static void EnsureCache()
        {
            if (_resolveCache != null) return;
            _resolveCache = new Dictionary<string, ContextKey>(64);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.GetCustomAttribute<AIContextKeyAttribute>() == null) continue;
                        foreach (var fi in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (!typeof(ContextKey).IsAssignableFrom(fi.FieldType)) continue;
                            if (fi.GetValue(null) is not ContextKey ck) continue;
                            _resolveCache[ck.InternalId] = ck;
                        }
                    }
                }
                catch { /* skip assemblies that fail reflection */ }
            }
        }

        /** <summary>
         * Returns the internal string ID to pass to <see cref="IContextBase.GetData{TValue}"/>.
         * </summary>
         */
        public string ResolveId() => internalId;

        /** <summary>
         * Construct a <see cref="ContextKeyField"/> from a raw internal ID string.
         * Used by the parser / code-generator to populate fields programmatically.
         * The displayName is resolved from the cache if the key is registered;
         * otherwise the id is used as a fallback display name.
         * </summary>
         */
        public static ContextKeyField FromId(string id)
        {
            if (string.IsNullOrEmpty(id)) return default;
            EnsureCache();
            _resolveCache.TryGetValue(id, out var ck);
            return new ContextKeyField { internalId = id, displayName = ck?.DisplayName ?? id };
        }

        /** <summary>
         * Resolves the field to the actual <see cref="ContextKey"/> object via a cached dictionary lookup — O(1).
         * The cache is built once on first call and never rebuilt during a play session.
         * </summary>
         */
        public ContextKey Resolve()
        {
            if (string.IsNullOrEmpty(internalId)) return null;
            EnsureCache();
            _resolveCache.TryGetValue(internalId, out ContextKey key);
            return key;
        }

        public override string ToString() =>
            string.IsNullOrEmpty(displayName) ? "(none)" : displayName;

#if UNITY_EDITOR
        // ── Editor-only cached key list ───────────────────────────────────────────
        private static List<(ContextKey key, string display, string category)> _allKeysCache;

        /** <summary>
         * Returns all <see cref="ContextKey"/> instances discovered on <c>[AIContextKey]</c>-tagged types.
         * Result is cached after the first call. Call <see cref="InvalidateEditorCache"/> after
         * adding new key classes to force a refresh.
         * </summary>
         */
        public static List<(ContextKey key, string display, string category)> GetAllKeys()
        {
            if (_allKeysCache != null) return _allKeysCache;
            _allKeysCache = new List<(ContextKey, string, string)>(32);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.GetCustomAttribute<AIContextKeyAttribute>() == null) continue;
                        foreach (var fi in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (!typeof(ContextKey).IsAssignableFrom(fi.FieldType)) continue;
                            if (fi.GetValue(null) is not ContextKey ck) continue;
                            _allKeysCache.Add((ck, ck.DisplayName, ck.Category));
                        }
                    }
                }
                catch { /* skip assemblies that fail reflection */ }
            }
            return _allKeysCache;
        }

        /** <summary>Clears both the runtime resolve cache and the editor key list cache. Call after a domain reload or when adding new key classes.</summary> */
        public static void InvalidateEditorCache()
        {
            _allKeysCache = null;
            _resolveCache = null;
        }

        // Automatically clears caches on every domain reload so a fresh play session
        // always rebuilds from the current assembly state.
        [UnityEditor.InitializeOnLoadMethod]
        private static void ClearCachesOnDomainReload()
        {
            _allKeysCache = null;
            _resolveCache = null;
        }
#endif
    }
}


