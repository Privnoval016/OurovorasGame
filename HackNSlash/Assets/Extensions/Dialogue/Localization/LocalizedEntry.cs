using System;
using UnityEngine;

namespace Extensions.Dialogue.Localization
{
    /// <summary>
    /// A single localized text entry supporting multiple languages.
    /// </summary>
    [Serializable]
    public sealed class LocalizedEntry
    {
        [TextArea] [SerializeField] public string English = "";
        [TextArea] [SerializeField] public string Japanese = "";
        [TextArea] [SerializeField] public string Korean = "";
        [TextArea] [SerializeField] public string Spanish = "";
        [TextArea] [SerializeField] public string French = "";

        public string GetText(SystemLanguage language) => language switch
        {
            SystemLanguage.Japanese => Japanese,
            SystemLanguage.Korean => Korean,
            SystemLanguage.Spanish => Spanish,
            SystemLanguage.French => French,
            _ => English
        };
    }

    /// <summary>
    /// Localization table managing all text entries for a dialogue system.
    /// Keyed by TextKey for type-safe lookups.
    /// </summary>
    [Serializable]
    public sealed class LocalizationTable : ScriptableObject
    {
        [SerializeField] private LocalizedEntry[] _entries = Array.Empty<LocalizedEntry>();
        
        private System.Collections.Generic.Dictionary<int, LocalizedEntry> _cache;

        public LocalizedEntry GetEntry(Data.TextKey key)
        {
            _cache ??= BuildCache();
            return _cache.TryGetValue(key.Value, out var entry) ? entry : null;
        }

        public string GetText(Data.TextKey key, SystemLanguage language)
        {
            var entry = GetEntry(key);
            return entry?.GetText(language) ?? "";
        }

        private System.Collections.Generic.Dictionary<int, LocalizedEntry> BuildCache()
        {
            var cache = new System.Collections.Generic.Dictionary<int, LocalizedEntry>();
            for (int i = 0; i < _entries.Length; i++)
            {
                cache[i] = _entries[i];
            }
            return cache;
        }

        #if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/Dialogue/Localization Table")]
        public static void CreateLocalizationTable()
        {
            var instance = CreateInstance<LocalizationTable>();
            string path = UnityEditor.EditorUtility.SaveFilePanelInProject("Create Localization Table", "LocalizationTable", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                UnityEditor.AssetDatabase.CreateAsset(instance, path);
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }
        #endif
    }
}

