using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Extensions.Dialogue.Integration
{
    /// <summary>
    /// Asset manager for dialogue graphs and related resources.
    /// Provides easy access to dialogue graphs from code or inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueAssetManager", menuName = "Dialogue/Dialogue Asset Manager")]
    public sealed class DialogueAssetManager : ScriptableObject
    {
        [SerializeField, InlineEditor] private List<Data.DialogueGraph> _dialogueGraphs = new();
        [SerializeField] private Localization.LocalizationTable _localizationTable;
        [SerializeField] private bool _debugMode = false;

        private Dictionary<string, Data.DialogueGraph> _graphCache;

        public Data.DialogueGraph GetGraph(string graphName)
        {
            _graphCache ??= BuildCache();

            if (_graphCache.TryGetValue(graphName, out var graph))
            {
                return graph;
            }

            if (_debugMode)
                Debug.LogWarning($"[DialogueAssetManager] Graph '{graphName}' not found");

            return null;
        }

        public Localization.LocalizationTable GetLocalizationTable() => _localizationTable;

        private Dictionary<string, Data.DialogueGraph> BuildCache()
        {
            var cache = new Dictionary<string, Data.DialogueGraph>();
            foreach (var graph in _dialogueGraphs)
            {
                cache[graph.GraphName] = graph;
            }
            return cache;
        }

        #if UNITY_EDITOR
        [Button]
        private void RefreshCache()
        {
            _graphCache = null;
            BuildCache();
            Debug.Log("[DialogueAssetManager] Cache refreshed");
        }
        #endif
    }
}

