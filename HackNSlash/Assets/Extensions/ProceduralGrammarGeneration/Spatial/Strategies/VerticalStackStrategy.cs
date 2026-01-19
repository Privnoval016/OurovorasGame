using UnityEngine;
using System.Linq;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Generic strategy that stacks elements vertically with configurable behaviors.
    /// Supports both face-based placement (4 sides of a structure) and grid-based placement.
    /// Completely grammar-agnostic - works with any symbols configured via SpatialBehavior enum.
    /// </summary>
    [System.Serializable]
    public class VerticalStackStrategy : BaseSpatialStrategy
    {
        public override string Name => "Vertical Stack";
        public override IParameterContract RequiredContract => null; // No contract - uses GeometryData
        
        [SerializeField]
        private float _defaultLayerHeight = 25f;
        
        [Tooltip("Configuration for symbol spatial behaviors. Use inspector to edit behaviors for specific symbols.")]
        public SpatialConfigManager ConfigManager = SpatialConfigManager.CreateBuildingDefaults();
        
        // Vertical tracking
        private float _currentY = 0f;
        private float _layerHeight = 0f;
        
        // Horizontal tracking
        private float _currentX = 0f;
        private float _currentZ = 0f;
        
        // Face-based placement
        private int _currentFace = 0;
        private Quaternion _currentRotation = Quaternion.identity;
        
        // Section dimensions
        private float _sectionLength = 0f;
        private float _sectionWidth = 0f;
        
        // Placement mode
        private bool _gridMode = false;

        public VerticalStackStrategy()
        {
            
        }
        
        public VerticalStackStrategy(float defaultLayerHeight)
        {
            _defaultLayerHeight = defaultLayerHeight;
            
            // Ensure config manager is initialized
            if (ConfigManager == null || ConfigManager.Configs == null || ConfigManager.Configs.Count == 0)
            {
                ConfigManager = SpatialConfigManager.CreateBuildingDefaults();
            }
        }
        
        public override void Initialize(SpatialContext context)
        {
            _currentY = 0f;
            _layerHeight = _defaultLayerHeight;
            _currentX = 0f;
            _currentZ = 0f;
            _currentFace = 0;
            _currentRotation = Quaternion.identity;
            _gridMode = false;
            _sectionLength = 0f;
            _sectionWidth = 0f;
        }
        
        public override void PlaceNode(SpatialNode node, SpatialContext context)
        {
            // Get configuration for this symbol (if any)
            var config = ConfigManager?.GetConfig(node.SymbolName);
            var behavior = config?.behavior ?? SpatialBehavior.PlaceElement;
            
            // Handle different behaviors
            switch (behavior)
            {
                case SpatialBehavior.ResetVertical:
                    HandleResetVertical(node, context);
                    return;
                    
                case SpatialBehavior.ChangeSide:
                    HandleChangeSide(node, context);
                    return;
                    
                case SpatialBehavior.StartSection:
                    HandleStartSection(node, context);
                    return;
                    
                case SpatialBehavior.AdvanceRow:
                    HandleAdvanceRow(node, context, config);
                    return;
                    
                case SpatialBehavior.StructuralMarker:
                    HandleStructuralMarker(node, context, config);
                    return;
                    
                case SpatialBehavior.PlaceElement:
                default:
                    HandlePlaceElement(node, context, config);
                    return;
            }
        }
        
        #region Behavior Handlers
        
        /// <summary>
        /// Handles ResetVertical behavior.
        /// Sets Y position for a new layer and resets horizontal tracking.
        /// </summary>
        private void HandleResetVertical(SpatialNode node, SpatialContext context)
        {
            // Try layerNum parameter, fallback to generic index parameter
            int layerNum = node.GetParameter("layerNum", node.GetParameter("floorNum", 0));
            _currentY = layerNum * _layerHeight;
            _currentX = 0f;
            _currentZ = 0f;
            _currentFace = 0;
            _gridMode = false;
            
            node.Position = new Vector3(0, _currentY, 0);
            node.Rotation = Quaternion.identity;
            node.Scale = Vector3.one * 0.1f;
            
            context.CurrentPosition = new Vector3(0, _currentY, 0);
        }
        
        /// <summary>
        /// Handles ChangeSide behavior.
        /// Changes rotation and positioning axis for different faces of a structure.
        /// </summary>
        private void HandleChangeSide(SpatialNode node, SpatialContext context)
        {
            _currentFace = node.GetParameter("faceNum", node.GetParameter("sideNum", 0));
            _currentX = 0f;
            _currentZ = 0f;
            
            // Get face length from node parameters
            float faceLength = node.GetParameter("faceLength", node.GetParameter("sideLength", 10f));
            
            // Update section dimensions based on face
            // Faces 0 and 2 use length (opposite sides), Faces 1 and 3 use width (perpendicular sides)
            if (_currentFace == 0 || _currentFace == 2)
                _sectionLength = faceLength;
            else
                _sectionWidth = faceLength;
            
            // Set rotation based on which face (0=front, 1=right, 2=back, 3=left)
            _currentRotation = _currentFace switch
            {
                0 => Quaternion.Euler(0, 0, 0),
                1 => Quaternion.Euler(0, 90, 0),
                2 => Quaternion.Euler(0, 180, 0),
                3 => Quaternion.Euler(0, 270, 0),
                _ => Quaternion.identity
            };
            
            node.Position = new Vector3(0, _currentY, 0);
            node.Rotation = Quaternion.identity;
            node.Scale = Vector3.one * 0.1f;
        }
        
        /// <summary>
        /// Handles StartSection behavior.
        /// Starts a new placement section with grid-based positioning.
        /// Calculates Y position from height parameters in context.
        /// </summary>
        private void HandleStartSection(SpatialNode node, SpatialContext context)
        {
            // Reset to grid origin
            _currentX = 0f;
            _currentZ = 0f;
            _currentFace = 0;
            _currentRotation = Quaternion.identity;
            _gridMode = true;
            
            // Calculate Y from total height in context (generic: could be building height, tower height, etc.)
            if (context.Variables.TryGetValue("height", out var heightObj) && 
                context.Variables.TryGetValue("layerHeight", out var layerHeightObj))
            {
                float height = heightObj is float h ? h : (heightObj is int hi ? (float)hi : 14f);
                _currentY = height;
            }
            else if (context.Variables.TryGetValue("height", out heightObj) && 
                     context.Variables.TryGetValue("floorHeight", out layerHeightObj))
            {
                // Fallback: support legacy floorHeight parameter name
                float height = heightObj is float h ? h : (heightObj is int hi ? (float)hi : 14f);
                _currentY = height;
            }
            else if (_currentY < 1f)
            {
                // Fallback: use accumulated Y with default layer count
                _currentY = _layerHeight * 4;
            }
            
            node.Position = new Vector3(0, _currentY, 0);
            node.Rotation = Quaternion.identity;
            node.Scale = Vector3.one * 0.1f;
        }
        
        /// <summary>
        /// Handles AdvanceRow behavior.
        /// Advances to next row in Z axis for grid-based placement.
        /// </summary>
        private void HandleAdvanceRow(SpatialNode node, SpatialContext context, SymbolSpatialConfig config)
        {
            // Place the row marker at current position
            node.Position = new Vector3(0, _currentY, _currentZ);
            node.Rotation = Quaternion.identity;
            node.Scale = Vector3.one * 0.1f;
            
            // Reset X for elements in this row
            _currentX = 0f;
            
            // Advance Z for next row using configured width parameter
            float rowAdvance = GetElementWidth(node, context, config);
            _currentZ += rowAdvance;
        }
        
        /// <summary>
        /// Handles StructuralMarker behavior.
        /// Non-visual grouping symbols with minimal scale.
        /// </summary>
        private void HandleStructuralMarker(SpatialNode node, SpatialContext context, SymbolSpatialConfig config)
        {
            node.Position = new Vector3(_currentX, _currentY, _currentZ);
            node.Rotation = _currentRotation;
            node.Scale = Vector3.one * (config?.markerScale ?? 0.1f);
        }
        
        /// <summary>
        /// Handles PlaceElement behavior.
        /// Places elements sequentially in either grid mode or face mode.
        /// </summary>
        private void HandlePlaceElement(SpatialNode node, SpatialContext context, SymbolSpatialConfig config)
        {
            // Get element width from configuration and context
            float elementWidth = GetElementWidth(node, context, config);
            
            // Calculate position based on placement mode
            Vector3 position;
            if (_gridMode)
            {
                // Grid mode: simple 2D grid positioning (X, Y, Z)
                position = new Vector3(_currentX, _currentY, _currentZ);
            }
            else
            {
                // Face mode: position elements around faces of a rectangular structure
                position = CalculatePositionForFace(_currentX, _currentY, _currentZ, _currentFace);
            }
            
            node.Position = position;
            node.Rotation = _currentRotation;
            node.Scale = Vector3.one; // Always (1,1,1) - geometry defines size
            
            // Apply geometry-specific adjustments
            if (node.GeometryData != null)
            {
                // In grid mode, skip position/rotation offsets to maintain exact grid alignment
                if (_gridMode)
                {
                    // Only apply scale from geometry, no position offsets
                    if (node.GeometryData.scaleToParameters)
                    {
                        node.Scale = node.GeometryData.CalculateScaleFromParameters(node);
                    }
                    else
                    {
                        node.Scale = node.GeometryData.GetPhysicalSize();
                    }
                }
                else
                {
                    // For face-based elements: apply full geometry transformation
                    ApplyGeometryData(node, context);
                    
                    // Add spacing for face-based elements
                    elementWidth += node.GeometryData.spacing;
                }
            }
            
            // Advance position for next element
            _currentX += elementWidth;
            context.CurrentPosition = position;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Get element width from:
        /// In grid mode: Context variables > Config default (geometry ignored for consistent tiling)
        /// In face mode: GeometryData > Context variables > Node parameters > Config default
        /// </summary>
        private float GetElementWidth(SpatialNode node, SpatialContext context, SymbolSpatialConfig config)
        {
            // Grid mode: prioritize context + config for consistent grid, ignore geometry width
            if (_gridMode)
            {
                if (config != null && !string.IsNullOrEmpty(config.widthParameter))
                {
                    float contextValue = GetContextFloat(context, config.widthParameter, -1f);
                    if (contextValue > 0)
                    {
                        return contextValue;
                    }
                }
                // Fallback to config default in grid mode
                return config?.defaultWidth ?? 2.0f;
            }
            
            // Face mode: use geometry data if available
            if (node.GeometryData != null)
            {
                Vector3 geometrySize = node.GeometryData.GetPhysicalSize();
                return geometrySize.x;
            }
            
            // Check context variables
            if (config != null && !string.IsNullOrEmpty(config.widthParameter))
            {
                float contextValue = GetContextFloat(context, config.widthParameter, -1f);
                if (contextValue > 0)
                {
                    return contextValue;
                }
            }
            
            // Check node parameters (backwards compatibility)
            if (node.Parameters.ContainsKey("width"))
            {
                return node.GetParameter("width", 1.0f);
            }
            
            // Final fallback to config default or 1.0
            return config?.defaultWidth ?? 1.0f;
        }
        
        /// <summary>
        /// Helper to safely extract float value from context variables.
        /// Handles type conversion for int/double to float.
        /// </summary>
        /// <summary>
        /// Helper to safely extract float value from context variables.
        /// Handles type conversion for int/double to float.
        /// </summary>
        private float GetContextFloat(SpatialContext context, string key, float defaultValue)
        {
            if (context.Variables.TryGetValue(key, out var value))
            {
                if (value is float f) return f;
                if (value is int i) return (float)i;
                if (value is double d) return (float)d;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Calculate position for an element based on current face of a rectangular structure.
        /// Structure has 4 faces forming a rectangle with proper corner connections.
        /// </summary>
        private Vector3 CalculatePositionForFace(float offset, float y, float zOffset, int faceNum)
        {
            // Generic rectangular structure layout (top-down view):
            //       Face 0
            //   +------------+
            //   |            |
            // F |            | F
            // a |            | a
            // c |            | c
            // e |            | e
            // 3 |            | 1
            //   |            |
            //   +------------+
            //       Face 2
            //
            // Face 0: Start at (0, y, 0), move along +X to (length, y, 0)
            // Face 1: Start at (length, y, 0), move along +Z to (length, y, width)
            // Face 2: Start at (length, y, width), move along -X to (0, y, width)
            // Face 3: Start at (0, y, width), move along -Z to (0, y, 0)
            
            return faceNum switch
            {
                0 => new Vector3(offset, y, 0),
                1 => new Vector3(_sectionLength, y, offset),
                2 => new Vector3(_sectionLength - offset, y, _sectionWidth),
                3 => new Vector3(0, y, _sectionWidth - offset),
                _ => new Vector3(offset, y, zOffset)
            };
        }
        
        #endregion
    }
}
