using System;
using System.Collections.Generic;
using UnityEngine;
using ProceduralGrammarGeneration.GrammarParsing;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Converts a grammar derivation tree into a spatial scene graph.
    /// This is the main entry point for spatial interpretation.
    /// </summary>
    public class SpatialInterpreter
    {
        private ISpatialStrategy _strategy;
        private Dictionary<string, SymbolGeometryData> _geometryLibrary;
        
        public SpatialInterpreter(ISpatialStrategy strategy)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _geometryLibrary = new Dictionary<string, SymbolGeometryData>();
        }
        
        /// <summary>
        /// Register geometry data for a symbol
        /// </summary>
        public void RegisterGeometry(SymbolGeometryData geometryData)
        {
            if (geometryData == null)
                throw new ArgumentNullException(nameof(geometryData));
            
            if (string.IsNullOrEmpty(geometryData.symbolName))
                throw new ArgumentException("Geometry data must have a symbol name");
            
            _geometryLibrary[geometryData.symbolName] = geometryData;
        }
        
        /// <summary>
        /// Register multiple geometry data assets
        /// </summary>
        public void RegisterGeometryLibrary(IEnumerable<SymbolGeometryData> geometryDataList)
        {
            foreach (var data in geometryDataList)
                RegisterGeometry(data);
        }
        
        /// <summary>
        /// Interpret a derivation tree into a spatial graph.
        /// Validates terminal nodes against strategy's parameter contract.
        /// </summary>
        public SpatialGraph Interpret(DerivationTree tree)
        {
            if (tree == null)
                throw new ArgumentNullException(nameof(tree));
            
            Debug.Log($"[SpatialInterpreter] Interpreting derivation tree with strategy: {_strategy.Name}");
            
            // Log contract requirements
            if (_strategy.RequiredContract != null)
            {
                Debug.Log($"[SpatialInterpreter] Strategy requires: {_strategy.RequiredContract.GetExpectedParameters()}");
            }
            
            var graph = new SpatialGraph();
            var context = new SpatialContext();
            
            // Copy root symbol parameters into context for strategies to use
            // This makes axiom parameters (windowSize, doorWidth, etc.) available globally
            if (tree.Root != null && tree.Root.Symbol != null)
            {
                foreach (var kvp in tree.Root.Symbol.Parameters)
                {
                    context.Variables[kvp.Key] = kvp.Value;
                }
            }
            
            // Initialize strategy
            _strategy.Initialize(context);
            
            // Convert root node
            graph.Root = ConvertNode(tree.Root, context);
            graph.RegisterNode(graph.Root);
            
            // Recursively convert all children
            ConvertChildren(tree.Root, graph.Root, context, graph);
            
            // Finalize
            graph.RecalculateBounds();
            
            Debug.Log($"[SpatialInterpreter] Converted derivation tree to spatial graph: {graph.NodesById.Count} nodes");
            
            return graph;
        }
        
        private SpatialNode ConvertNode(DerivationNode derivationNode, SpatialContext context)
        {
            var spatialNode = new SpatialNode(derivationNode.Symbol.Type.Name);
            spatialNode.Id = context.NodeIdCounter++;
            
            // Copy parameters from derivation node
            foreach (var kvp in derivationNode.Symbol.Parameters)
            {
                spatialNode.Parameters[kvp.Key] = kvp.Value;
            }
            
            // Attach geometry data if available
            if (_geometryLibrary.TryGetValue(spatialNode.SymbolName, out var geometryData))
            {
                spatialNode.GeometryData = geometryData;
            }
            
            // Let strategy place the node
            _strategy.PlaceNode(spatialNode, context);
            
            return spatialNode;
        }
        
        private void ConvertChildren(DerivationNode derivationNode, SpatialNode spatialNode, SpatialContext context, SpatialGraph graph)
        {
            if (derivationNode.Children.Count == 0)
                return;
            
            // Begin scope for children
            _strategy.BeginScope(spatialNode, context);
            
            foreach (var child in derivationNode.Children)
            {
                var childSpatialNode = ConvertNode(child, context);
                childSpatialNode.Parent = spatialNode;
                spatialNode.AddChild(childSpatialNode);
                graph.RegisterNode(childSpatialNode);
                
                // Recursively convert grandchildren
                ConvertChildren(child, childSpatialNode, context, graph);
            }
            
            // End scope
            _strategy.EndScope(spatialNode, context);
        }
    }
}
