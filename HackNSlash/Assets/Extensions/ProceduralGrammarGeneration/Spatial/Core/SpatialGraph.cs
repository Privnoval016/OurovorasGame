using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Container for a complete spatial scene graph.
    /// This is the output of spatial interpretation - a hierarchy of positioned nodes ready for instantiation.
    /// </summary>
    public class SpatialGraph
    {
        /// <summary>
        /// Root node of the spatial hierarchy
        /// </summary>
        public SpatialNode Root { get; set; }
        
        /// <summary>
        /// All nodes indexed by ID for quick lookup
        /// </summary>
        public Dictionary<int, SpatialNode> NodesById { get; private set; }
        
        /// <summary>
        /// Bounds of the entire spatial graph
        /// </summary>
        public Bounds Bounds { get; private set; }
        
        /// <summary>
        /// Metadata about the spatial interpretation process
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }
        
        private int _nextNodeId = 0;

        public SpatialGraph()
        {
            NodesById = new Dictionary<int, SpatialNode>();
            Metadata = new Dictionary<string, object>();
            Bounds = new Bounds();
        }
        
        /// <summary>
        /// Register a node in the graph and assign it an ID
        /// </summary>
        public void RegisterNode(SpatialNode node)
        {
            if (node.Id == 0)
                node.Id = _nextNodeId++;
            
            NodesById[node.Id] = node;
        }
        
        /// <summary>
        /// Get all terminal (leaf) nodes in the graph
        /// </summary>
        public List<SpatialNode> GetTerminalNodes()
        {
            var terminals = new List<SpatialNode>();
            if (Root != null)
                CollectTerminalNodes(Root, terminals);
            return terminals;
        }
        
        private void CollectTerminalNodes(SpatialNode node, List<SpatialNode> terminals)
        {
            if (node.Children.Count == 0)
            {
                terminals.Add(node);
            }
            else
            {
                foreach (var child in node.Children)
                    CollectTerminalNodes(child, terminals);
            }
        }
        
        /// <summary>
        /// Get all nodes with a specific symbol name
        /// </summary>
        public List<SpatialNode> GetNodesBySymbol(string symbolName)
        {
            return NodesById.Values.Where(n => n.SymbolName == symbolName).ToList();
        }
        
        /// <summary>
        /// Recalculate bounds to encompass all nodes
        /// </summary>
        public void RecalculateBounds()
        {
            if (Root == null || NodesById.Count == 0)
            {
                Bounds = new Bounds();
                return;
            }
            
            bool first = true;
            Bounds bounds = new Bounds();
            
            foreach (var node in NodesById.Values)
            {
                if (first)
                {
                    bounds = new Bounds(node.Position, node.Scale);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(node.Position + node.Scale * 0.5f);
                    bounds.Encapsulate(node.Position - node.Scale * 0.5f);
                }
            }
            
            Bounds = bounds;
        }
        
        /// <summary>
        /// Get statistics about the spatial graph
        /// </summary>
        public SpatialGraphStats GetStats()
        {
            var stats = new SpatialGraphStats(true);
            stats.TotalNodes = NodesById.Count;
            stats.TerminalNodes = GetTerminalNodes().Count;
            stats.MaxDepth = CalculateMaxDepth(Root, 0);
            
            // Count symbols by type
            foreach (var node in NodesById.Values)
            {
                if (!stats.SymbolCounts.ContainsKey(node.SymbolName))
                    stats.SymbolCounts[node.SymbolName] = 0;
                stats.SymbolCounts[node.SymbolName]++;
            }
            
            return stats;
        }
        
        private int CalculateMaxDepth(SpatialNode node, int currentDepth)
        {
            if (node == null)
                return currentDepth;
            
            int maxChildDepth = currentDepth;
            foreach (var child in node.Children)
            {
                int childDepth = CalculateMaxDepth(child, currentDepth + 1);
                if (childDepth > maxChildDepth)
                    maxChildDepth = childDepth;
            }
            
            return maxChildDepth;
        }
    }
    
    /// <summary>
    /// Statistics about a spatial graph
    /// </summary>
    public struct SpatialGraphStats
    {
        public int TotalNodes;
        public int TerminalNodes;
        public int MaxDepth;
        public Dictionary<string, int> SymbolCounts;
        
        public SpatialGraphStats(bool init)
        {
            TotalNodes = 0;
            TerminalNodes = 0;
            MaxDepth = 0;
            SymbolCounts = new Dictionary<string, int>();
        }
    }
}
