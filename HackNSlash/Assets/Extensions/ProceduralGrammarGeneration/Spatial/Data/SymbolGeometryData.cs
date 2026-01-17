using UnityEngine;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// ScriptableObject that defines physical geometry data for a terminal grammar symbol.
    /// This bridges abstract grammar symbols (e.g., "Window") with concrete 3D assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSymbolGeometry", menuName = "Procedural Grammar/Symbol Geometry Data", order = 100)]
    public class SymbolGeometryData : ScriptableObject
    {
        [Header("Symbol Identification")]
        [Tooltip("Name of the grammar symbol this data represents (must match terminal symbol name)")]
        public string symbolName;
        
        [TextArea(2, 4)]
        [Tooltip("Description of what this symbol represents")]
        public string description;
        
        [Header("Geometry")]
        [Tooltip("Prefab or mesh to instantiate for this symbol")]
        public GameObject prefab;
        
        [Tooltip("Optional mesh (used if prefab is null)")]
        public Mesh mesh;
        
        [Tooltip("Optional material override")]
        public Material material;
        
        [Header("Physical Dimensions")]
        [Tooltip("Actual physical width of the geometry (meters)")]
        public float physicalWidth = 1.0f;
        
        [Tooltip("Actual physical height of the geometry (meters)")]
        public float physicalHeight = 1.0f;
        
        [Tooltip("Actual physical depth of the geometry (meters)")]
        public float physicalDepth = 1.0f;
        
        [Tooltip("If true, scale the mesh to match grammar parameters. If false, use fixed physical dimensions.")]
        public bool scaleToParameters = true;
        
        [Header("Pivot & Alignment")]
        [Tooltip("Local pivot offset (0.5, 0.5, 0.5 = center)")]
        public Vector3 pivotOffset = new Vector3(0.5f, 0.5f, 0.5f);
        
        [Tooltip("How to align this symbol relative to placement context")]
        public AlignmentMode alignment = AlignmentMode.Default;
        
        [Header("Placement Hints")]
        [Tooltip("Spacing between repeated instances")]
        public float spacing = 0.0f;
        
        [Tooltip("Additional offset applied after placement")]
        public Vector3 positionOffset = Vector3.zero;
        
        [Tooltip("Additional rotation applied after placement")]
        public Vector3 rotationOffset = Vector3.zero;
        
        [Header("Rendering")]
        [Tooltip("Layer to assign to instantiated GameObjects")]
        public int layer = 0;
        
        [Tooltip("Rendering tag")]
        public string tag = "Untagged";
        
        [Tooltip("Cast shadows")]
        public bool castShadows = true;
        
        [Tooltip("Receive shadows")]
        public bool receiveShadows = true;
        
        /// <summary>
        /// Get the physical bounds of this geometry
        /// </summary>
        public Vector3 GetPhysicalSize()
        {
            return new Vector3(physicalWidth, physicalHeight, physicalDepth);
        }
        
        /// <summary>
        /// Calculate scale factor to match target dimensions
        /// </summary>
        public Vector3 CalculateScale(float targetWidth, float targetHeight, float targetDepth = -1)
        {
            if (!scaleToParameters)
                return Vector3.one;
            
            float scaleX = targetWidth / physicalWidth;
            float scaleY = targetHeight / physicalHeight;
            float scaleZ = targetDepth > 0 ? (targetDepth / physicalDepth) : 1.0f;
            
            return new Vector3(scaleX, scaleY, scaleZ);
        }
        
        /// <summary>
        /// Calculate scale factor from grammar parameters
        /// </summary>
        public Vector3 CalculateScaleFromParameters(SpatialNode node)
        {
            float width = node.GetParameter<float>("width", physicalWidth);
            float height = node.GetParameter<float>("height", physicalHeight);
            float depth = node.GetParameter<float>("depth", physicalDepth);
            
            return CalculateScale(width, height, depth);
        }
        
        /// <summary>
        /// Get pivot position in local space
        /// </summary>
        public Vector3 GetPivotPosition()
        {
            Vector3 size = GetPhysicalSize();
            return Vector3.Scale(size, pivotOffset) - size * 0.5f;
        }
        
        /// <summary>
        /// Validate this geometry data
        /// </summary>
        public bool Validate(out string error)
        {
            if (string.IsNullOrEmpty(symbolName))
            {
                error = "Symbol name cannot be empty";
                return false;
            }
            
            if (prefab == null && mesh == null)
            {
                error = "Either prefab or mesh must be assigned";
                return false;
            }
            
            if (physicalWidth <= 0 || physicalHeight <= 0 || physicalDepth <= 0)
            {
                error = "Physical dimensions must be positive";
                return false;
            }
            
            error = null;
            return true;
        }
    }
    
    /// <summary>
    /// How to align a symbol during placement
    /// </summary>
    public enum AlignmentMode
    {
        Default,        // Use placement strategy's default
        Center,         // Center on placement point
        Bottom,         // Align bottom to placement point
        Top,            // Align top to placement point
        Left,           // Align left edge
        Right,          // Align right edge
        BottomLeft,     // Align bottom-left corner
        Custom          // Use custom offset
    }
}
