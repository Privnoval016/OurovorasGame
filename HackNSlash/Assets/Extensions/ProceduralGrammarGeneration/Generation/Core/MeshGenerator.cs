using System.Collections.Generic;
using UnityEngine;
using ProceduralGrammarGeneration.Spatial;

namespace ProceduralGrammarGeneration.Generation
{
    /// <summary>
    /// Converts a spatial graph to actual Unity GameObjects with meshes.
    /// Non-MonoBehaviour class for generating geometry from spatial interpretation results.
    /// Uses GPU instancing for optimized rendering of repeated elements.
    /// </summary>
    public class MeshGenerator
    {
        private Dictionary<int, GameObject> _nodeToGameObject = new Dictionary<int, GameObject>();
        
        /// <summary>
        /// Generate Unity GameObjects from a spatial graph.
        /// </summary>
        /// <param name="graph">The spatial graph to convert</param>
        /// <param name="parent">Parent transform for all generated objects</param>
        /// <param name="includeNonTerminals">Whether to create GameObjects for non-terminal nodes</param>
        /// <returns>Root GameObject containing all generated geometry</returns>
        public GameObject Generate(SpatialGraph graph, Transform parent = null, bool includeNonTerminals = false)
        {
            _nodeToGameObject.Clear();
            
            // Create root container
            GameObject root = new GameObject("GeneratedStructure");
            if (parent != null)
                root.transform.SetParent(parent, false);
            
            // Process all nodes
            foreach (var kvp in graph.NodesById)
            {
                SpatialNode node = kvp.Value;
                
                // Skip non-terminals unless requested
                if (!includeNonTerminals && node.Children.Count > 0)
                    continue;
                
                GenerateNode(node, root.transform);
            }
            
            Debug.Log($"Generated {_nodeToGameObject.Count} GameObjects from spatial graph");
            return root;
        }
        
        /// <summary>
        /// Generate a single GameObject from a spatial node.
        /// </summary>
        private GameObject GenerateNode(SpatialNode node, Transform parent)
        {
            GameObject go;
            
            // Check if we have geometry data for this symbol
            if (node.GeometryData != null && node.GeometryData.prefab != null)
            {
                // Instantiate the assigned prefab
                go = Object.Instantiate(node.GeometryData.prefab, parent);
                go.name = $"{node.SymbolName}_{node.Id}";
            }
            else
            {
                // No geometry data - create a cube placeholder
                go = CreateCubePlaceholder(node, parent);
                
                if (node.GeometryData == null)
                {
                    Debug.LogWarning($"Missing geometry data for terminal symbol '{node.SymbolName}' (ID: {node.Id}). Using cube placeholder.");
                }
            }
            
            // Apply transform from spatial node
            go.transform.localPosition = node.Position;
            go.transform.localRotation = node.Rotation;
            go.transform.localScale = node.Scale;
            
            // Store reference
            _nodeToGameObject[node.Id] = go;
            
            return go;
        }
        
        /// <summary>
        /// Create a cube primitive as a placeholder for missing geometry.
        /// Uses default Unity material to avoid runtime material creation and Odin validation issues.
        /// </summary>
        private GameObject CreateCubePlaceholder(SpatialNode node, Transform parent)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(parent, false);
            cube.name = $"{node.SymbolName}_{node.Id}_PLACEHOLDER";
            
            // Keep default material - all placeholders will be white
            // This avoids creating runtime materials that cause Odin validation errors
            // When you create proper SymbolGeometryData assets, the cubes will be replaced
            
            return cube;
        }
        
        /// <summary>
        /// Get the GameObject associated with a spatial node ID.
        /// </summary>
        public GameObject GetGameObject(int nodeId)
        {
            _nodeToGameObject.TryGetValue(nodeId, out GameObject go);
            return go;
        }
        
        /// <summary>
        /// Clean up all generated GameObjects.
        /// </summary>
        public void Clear()
        {
            foreach (var go in _nodeToGameObject.Values)
            {
                if (go != null)
                    Object.Destroy(go);
            }
            _nodeToGameObject.Clear();
        }
    }
}
