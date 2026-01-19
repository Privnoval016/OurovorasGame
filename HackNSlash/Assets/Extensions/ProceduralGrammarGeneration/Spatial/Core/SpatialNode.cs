using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Represents a node in the spatial scene graph with concrete 3D transform data.
    /// This is the output of spatial interpretation - each node has a position, rotation, and scale in world space.
    /// </summary>
    public class SpatialNode
    {
        /// <summary>
        /// Unique identifier for this node in the spatial graph
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Name of the grammar symbol this node represents (e.g., "Window", "Door", "Wall")
        /// </summary>
        public string SymbolName { get; set; }
        
        /// <summary>
        /// Abstract parameters from the grammar derivation (width, height, floorNum, etc.)
        /// These are semantic/logical values, not necessarily physical dimensions
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }
        
        /// <summary>
        /// World-space position of this node
        /// </summary>
        public Vector3 Position { get; set; }
        
        /// <summary>
        /// World-space rotation of this node
        /// </summary>
        public Quaternion Rotation { get; set; }
        
        /// <summary>
        /// Local scale of this node (often derived from width/height parameters)
        /// </summary>
        public Vector3 Scale { get; set; }
        
        /// <summary>
        /// Child nodes in the spatial hierarchy
        /// </summary>
        public List<SpatialNode> Children { get; set; }
        
        /// <summary>
        /// Parent node (null if root)
        /// </summary>
        public SpatialNode Parent { get; set; }
        
        /// <summary>
        /// Reference to geometry data for this symbol (mesh, prefab, etc.)
        /// </summary>
        public SymbolGeometryData GeometryData { get; set; }
        
        /// <summary>
        /// Optional metadata for custom processing
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        public SpatialNode()
        {
            Parameters = new Dictionary<string, object>();
            Children = new List<SpatialNode>();
            Metadata = new Dictionary<string, object>();
            Scale = Vector3.one;
            Rotation = Quaternion.identity;
        }
        
        public SpatialNode(string symbolName) : this()
        {
            SymbolName = symbolName;
        }
        
        /// <summary>
        /// Add a child node to this node
        /// </summary>
        public void AddChild(SpatialNode child)
        {
            Children.Add(child);
            child.Parent = this;
        }
        
        /// <summary>
        /// Get world-space transform matrix
        /// </summary>
        public Matrix4x4 GetWorldMatrix()
        {
            return Matrix4x4.TRS(Position, Rotation, Scale);
        }
        
        /// <summary>
        /// Get local-space transform relative to parent
        /// </summary>
        public Matrix4x4 GetLocalMatrix()
        {
            if (Parent == null)
                return GetWorldMatrix();
            
            Matrix4x4 parentInverse = Parent.GetWorldMatrix().inverse;
            return parentInverse * GetWorldMatrix();
        }
        
        /// <summary>
        /// Get parameter value with type casting and numeric type conversion
        /// </summary>
        public T GetParameter<T>(string name, T defaultValue = default)
        {
            if (!Parameters.TryGetValue(name, out var value))
                return defaultValue;
            
            // Direct type match
            if (value is T typedValue)
                return typedValue;
            
            // Handle numeric conversions between int and float
            if (typeof(T) == typeof(float))
            {
                if (value is int intVal)
                    return (T)(object)(float)intVal;
                if (value is double doubleVal)
                    return (T)(object)(float)doubleVal;
            }
            else if (typeof(T) == typeof(int))
            {
                if (value is float floatVal)
                    return (T)(object)(int)floatVal;
                if (value is double doubleVal)
                    return (T)(object)(int)doubleVal;
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Check if parameter exists
        /// </summary>
        public bool HasParameter(string name)
        {
            return Parameters.ContainsKey(name);
        }
        
        /// <summary>
        /// Recursively collect all descendant nodes
        /// </summary>
        public List<SpatialNode> GetAllDescendants()
        {
            var result = new List<SpatialNode>();
            CollectDescendants(this, result);
            return result;
        }
        
        private void CollectDescendants(SpatialNode node, List<SpatialNode> result)
        {
            foreach (var child in node.Children)
            {
                result.Add(child);
                CollectDescendants(child, result);
            }
        }
        
        public override string ToString()
        {
            return $"{SymbolName} @ {Position} (children: {Children.Count})";
        }
    }
}
