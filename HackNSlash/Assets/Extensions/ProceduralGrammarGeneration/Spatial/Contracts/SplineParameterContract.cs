using System.Collections.Generic;
using System.Linq;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Parameter contract for spline-based placement.
    /// Terminal symbols must provide: splinePos (0-1), height, thickness parameters.
    /// 
    /// Used by strategies that place objects along a spline/curve path.
    /// Example: SplineFollowStrategy, RoadGeneratorStrategy, etc.
    /// </summary>
    public class SplineParameterContract : IParameterContract
    {
        public string Name => "Spline/Position/Height";
        
        // Parameter names to check for
        private static readonly string[] SplinePosNames = { "splinePos", "splinePosition", "t", "position" };
        private static readonly string[] HeightNames = { "height", "h" };
        private static readonly string[] ThicknessNames = { "thickness", "width", "w" };
        
        public bool ValidateNode(SpatialNode node)
        {
            if (node == null)
                return false;
            
            return HasAnyParameter(node, SplinePosNames) &&
                   HasAnyParameter(node, HeightNames) &&
                   HasAnyParameter(node, ThicknessNames);
        }
        
        public string GetMissingParameters(SpatialNode node)
        {
            var missing = new List<string>();
            
            if (!HasAnyParameter(node, SplinePosNames))
                missing.Add("splinePos/splinePosition/t/position");
            if (!HasAnyParameter(node, HeightNames))
                missing.Add("height/h");
            if (!HasAnyParameter(node, ThicknessNames))
                missing.Add("thickness/width/w");
            
            return missing.Count > 0 ? string.Join(", ", missing) : "none";
        }
        
        public string GetExpectedParameters()
        {
            return "splinePos/t (float 0-1), height/h (float), thickness/width/w (float)";
        }
        
        /// <summary>
        /// Get the position along the spline (0-1, where 0 = start, 1 = end)
        /// </summary>
        public float GetSplinePosition(SpatialNode node)
        {
            return GetFirstParameter<float>(node, SplinePosNames, 0.0f);
        }
        
        /// <summary>
        /// Get the height parameter (vertical offset from spline)
        /// </summary>
        public float GetHeight(SpatialNode node)
        {
            return GetFirstParameter<float>(node, HeightNames, 1.0f);
        }
        
        /// <summary>
        /// Get the thickness parameter (perpendicular to spline direction)
        /// </summary>
        public float GetThickness(SpatialNode node)
        {
            return GetFirstParameter<float>(node, ThicknessNames, 1.0f);
        }
        
        /// <summary>
        /// Get normalized position (ensures 0-1 range)
        /// </summary>
        public float GetNormalizedSplinePosition(SpatialNode node)
        {
            float t = GetSplinePosition(node);
            return UnityEngine.Mathf.Clamp01(t);
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
