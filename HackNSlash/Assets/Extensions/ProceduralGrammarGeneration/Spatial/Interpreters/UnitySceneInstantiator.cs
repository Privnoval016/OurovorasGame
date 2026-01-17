using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Instantiates a spatial graph as Unity GameObjects in the scene.
    /// </summary>
    public class UnitySceneInstantiator
    {
        private Transform _parentTransform;
        private bool _createHierarchy;
        
        public UnitySceneInstantiator(Transform parent = null, bool createHierarchy = true)
        {
            _parentTransform = parent;
            _createHierarchy = createHierarchy;
        }
        
        /// <summary>
        /// Instantiate a spatial graph in the Unity scene
        /// </summary>
        public GameObject Instantiate(SpatialGraph graph)
        {
            if (graph == null || graph.Root == null)
            {
                Debug.LogError("[UnitySceneInstantiator] Cannot instantiate null graph");
                return null;
            }
            
            // Create root container
            GameObject rootObject = new GameObject($"ProceduralGeneration_{graph.Root.SymbolName}");
            if (_parentTransform != null)
                rootObject.transform.SetParent(_parentTransform);
            
            // Instantiate all nodes
            var instantiatedObjects = new Dictionary<int, GameObject>();
            InstantiateNode(graph.Root, rootObject.transform, instantiatedObjects);
            
            Debug.Log($"[UnitySceneInstantiator] Instantiated {instantiatedObjects.Count} GameObjects");
            
            return rootObject;
        }
        
        private void InstantiateNode(SpatialNode node, Transform parent, Dictionary<int, GameObject> instantiated)
        {
            GameObject nodeObject = null;
            
            // Create GameObject from geometry data
            if (node.GeometryData != null)
            {
                if (node.GeometryData.prefab != null)
                {
                    // Instantiate prefab
                    nodeObject = Object.Instantiate(node.GeometryData.prefab);
                    nodeObject.name = $"{node.SymbolName}_{node.Id}";
                }
                else if (node.GeometryData.mesh != null)
                {
                    // Create primitive from mesh
                    nodeObject = CreateMeshObject(node);
                }
            }
            
            // Fallback: create empty GameObject
            if (nodeObject == null)
            {
                nodeObject = new GameObject($"{node.SymbolName}_{node.Id}");
            }
            
            // Set transform
            nodeObject.transform.position = node.Position;
            nodeObject.transform.rotation = node.Rotation;
            nodeObject.transform.localScale = node.Scale;
            
            // Set parent (hierarchical or flat)
            if (_createHierarchy && parent != null)
            {
                nodeObject.transform.SetParent(parent);
            }
            else if (_parentTransform != null)
            {
                nodeObject.transform.SetParent(_parentTransform);
            }
            
            // Apply geometry settings
            if (node.GeometryData != null)
            {
                nodeObject.layer = node.GeometryData.layer;
                nodeObject.tag = node.GeometryData.tag;
                
                // Configure renderer if present
                var renderer = nodeObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.shadowCastingMode = node.GeometryData.castShadows 
                        ? UnityEngine.Rendering.ShadowCastingMode.On 
                        : UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = node.GeometryData.receiveShadows;
                    
                    if (node.GeometryData.material != null)
                        renderer.material = node.GeometryData.material;
                }
            }
            
            // Store reference
            instantiated[node.Id] = nodeObject;
            
            // Recursively instantiate children
            foreach (var child in node.Children)
            {
                InstantiateNode(child, nodeObject.transform, instantiated);
            }
        }
        
        private GameObject CreateMeshObject(SpatialNode node)
        {
            GameObject obj = new GameObject($"{node.SymbolName}_{node.Id}");
            
            // Add MeshFilter
            var meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.mesh = node.GeometryData.mesh;
            
            // Add MeshRenderer
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            if (node.GeometryData.material != null)
                meshRenderer.material = node.GeometryData.material;
            else
                meshRenderer.material = new Material(Shader.Find("Standard"));
            
            return obj;
        }
    }
}
