using System;
using UnityEngine;

namespace Extensions.Dialogue.Data
{
    /// <summary>
    /// Root data structure for a complete dialogue graph.
    /// </summary>
    public sealed class DialogueGraph : ScriptableObject
    {
        [SerializeField] public string GraphName;
        [SerializeField] public NodeId StartNode;
        [SerializeField] public DialogueNode[] Nodes = Array.Empty<DialogueNode>();

        /// <summary>
        /// Quickly lookup a node by its ID. Build this on runtime access.
        /// </summary>
        private System.Collections.Generic.Dictionary<int, DialogueNode> _nodeCache;

        public DialogueNode GetNode(NodeId id)
        {
            _nodeCache ??= BuildNodeCache();
            return _nodeCache.TryGetValue(id.Value, out var node) ? node : null;
        }

        private System.Collections.Generic.Dictionary<int, DialogueNode> BuildNodeCache()
        {
            var cache = new System.Collections.Generic.Dictionary<int, DialogueNode>();
            foreach (var node in Nodes)
            {
                cache[node.Id.Value] = node;
            }
            return cache;
        }
    }
}