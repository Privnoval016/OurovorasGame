using System.Collections.Generic;
using System.Linq;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Parameter contract for Length/Width/Height based placement.
    /// Terminal symbols must provide: width, height, depth (or length) parameters.
    /// 
    /// Used by strategies that place objects in 3D space based on their logical dimensions.
    /// Example: VerticalStackStrategy, GridLayoutStrategy, etc.
    /// </summary>
    public class LWHParameterContract : IParameterContract
    {
        public string Name => "Length/Width/Height";
        
        // Parameter names to check for
        private static readonly string[] WidthNames = { "width", "w" };
        private static readonly string[] HeightNames = { "height", "h" };
        private static readonly string[] DepthNames = { "depth", "d", "length", "l" };
        
        public bool ValidateNode(SpatialNode node)
        {
            if (node == null)
                return false;
            
            return HasAnyParameter(node, WidthNames) &&
                   HasAnyParameter(node, HeightNames) &&
                   HasAnyParameter(node, DepthNames);
        }
        
        public string GetMissingParameters(SpatialNode node)
        {
            var missing = new List<string>();
            
            if (!HasAnyParameter(node, WidthNames))
                missing.Add("width/w");
            if (!HasAnyParameter(node, HeightNames))
                missing.Add("height/h");
            if (!HasAnyParameter(node, DepthNames))
                missing.Add("depth/d/length/l");
            
            return missing.Count > 0 ? string.Join(", ", missing) : "none";
        }
        
        public string GetExpectedParameters()
        {
            return "width/w (float), height/h (float), depth/d/length/l (float)";
        }
        
        /// <summary>
        /// Get the width parameter from the node
        /// </summary>
        public float GetWidth(SpatialNode node)
        {
            return GetFirstParameter<float>(node, WidthNames, 1.0f);
        }
        
        /// <summary>
        /// Get the height parameter from the node
        /// </summary>
        public float GetHeight(SpatialNode node)
        {
            return GetFirstParameter<float>(node, HeightNames, 1.0f);
        }
        
        /// <summary>
        /// Get the depth parameter from the node (also checks for "length")
        /// </summary>
        public float GetDepth(SpatialNode node)
        {
            return GetFirstParameter<float>(node, DepthNames, 1.0f);
        }
        
        /// <summary>
        /// Get dimensions as a Vector3 (width, height, depth)
        /// </summary>
        public UnityEngine.Vector3 GetDimensions(SpatialNode node)
        {
            return new UnityEngine.Vector3(GetWidth(node), GetHeight(node), GetDepth(node));
        }
        
        // Helper: check if node has any of the given parameter names
        private bool HasAnyParameter(SpatialNode node, string[] paramNames)
        {
            return paramNames.Any(name => node.HasParameter(name));
        }
        
        // Helper: get the first parameter that exists from a list of names
        private T GetFirstParameter<T>(SpatialNode node, string[] paramNames, T defaultValue)
        {
            foreach (var name in paramNames)
            {
                if (node.HasParameter(name))
                    return node.GetParameter<T>(name);
            }
            return defaultValue;
        }
    }
}
