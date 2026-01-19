using UnityEngine;
using UnityEngine.Splines;
using System.Linq;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Strategy that follows a spline path to generate buildings along curves.
    /// Supports vertical stacking and orthogonal offsets from the spline.
    /// Completely grammar-agnostic - works with any symbols configured via SpatialBehavior enum.
    /// </summary>
    [System.Serializable]
    public class SplineFollowStrategy : BaseSpatialStrategy
    {
        public override string Name => "Spline Follow";
        public override IParameterContract RequiredContract => null;
        
        [Header("Spline Configuration")]
        [Tooltip("Unity Spline Container to follow (assign a GameObject with SplineContainer component)")]
        public SplineContainer splineContainer;
        
        [Tooltip("Vertical height for structure")]
        public float structureHeight = 14f;
        
        [Tooltip("Orthogonal offset from spline (positive = right, negative = left)")]
        public float orthogonalOffset = 0f;
        
        [Tooltip("Default layer height for vertical stacking")]
        public float defaultLayerHeight = 3.5f;
        
        [Tooltip("Configuration for symbol spatial behaviors")]
        public SpatialConfigManager ConfigManager = SpatialConfigManager.CreateBuildingDefaults();
        
        // Spline traversal state
        private float _currentSplineT = 0f; // Parameter along spline [0, 1]
        private float _splineSegmentLength = 0f; // Length of one segment
        
        // Vertical tracking
        private float _currentY = 0f;
        private float _layerHeight = 0f;
        
        // Horizontal tracking along spline
        private float _currentDistance = 0f; // Distance along spline
        
        // Face-based placement
        private int _currentFace = 0; // 0=front, 1=right, 2=back, 3=left
        private Quaternion _currentRotation = Quaternion.identity;
        
        // Section dimensions
        private float _sectionLength = 0f;
        private float _sectionWidth = 0f;
        
        // Placement mode
        private bool _gridMode = false;
        
        public SplineFollowStrategy() { }
        
        public SplineFollowStrategy(SplineContainer container, float height, float offset, float layerHeight = 3.5f)
        {
            splineContainer = container;
            structureHeight = height;
            orthogonalOffset = offset;
            defaultLayerHeight = layerHeight;
            
            if (ConfigManager == null || ConfigManager.Configs == null || ConfigManager.Configs.Count == 0)
            {
                ConfigManager = SpatialConfigManager.CreateBuildingDefaults();
            }
        }
        
        public override void Initialize(SpatialContext context)
        {
            base.Initialize(context);
            
            // Calculate spline length and provide it as a context variable
            // This allows grammars to use splineLength without passing it as a parameter
            if (splineContainer != null && splineContainer.Spline != null)
            {
                float splineLength = splineContainer.Spline.GetLength();
                context.SetVariable("splineLength", splineLength);
            }
        }
        
        public void Reset()
        {
            _currentSplineT = 0f;
            _currentY = 0f;
            _layerHeight = 0f;
            _currentDistance = 0f;
            _currentFace = 0;
            _currentRotation = Quaternion.identity;
            _gridMode = false;
            _sectionLength = 0f;
            _sectionWidth = 0f;
            _splineSegmentLength = 0f;
        }
        
        public override void PlaceNode(SpatialNode node, SpatialContext context)
        {
            var config = ConfigManager?.GetConfig(node.SymbolName);
            var behavior = config?.behavior ?? SpatialBehavior.PlaceElement;
            
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
                    HandleAdvanceRow(node, context);
                    return;
                    
                case SpatialBehavior.StructuralMarker:
                    HandleStructuralMarker(node, context);
                    return;
                    
                case SpatialBehavior.PlaceElement:
                default:
                    HandlePlaceElement(node, context, config);
                    return;
            }
        }
        
        private void HandleResetVertical(SpatialNode node, SpatialContext context)
        {
            // Get layer/floor number and height
            int layerNum = context.GetVariable<int>("layerNum", 
                          context.GetVariable<int>("floorNum", 0));
            
            _layerHeight = context.GetVariable<float>("layerHeight",
                          context.GetVariable<float>("floorHeight", defaultLayerHeight));
            
            _currentY = layerNum * _layerHeight;
            
            // Reset horizontal position for new layer
            _currentDistance = 0f;
            _currentSplineT = 0f;
            
            // Minimal visual representation
            node.Position = GetPositionAlongSpline(_currentSplineT, _currentY);
            node.Rotation = GetRotationAlongSpline(_currentSplineT);
            node.Scale = Vector3.one * 0.1f;
            
            context.CurrentPosition = node.Position;
        }
        
        private void HandleChangeSide(SpatialNode node, SpatialContext context)
        {
            // Get face/side number
            _currentFace = context.GetVariable<int>("faceNum",
                         context.GetVariable<int>("sideNum", 0));
            
            // Get section dimensions
            float sideLength = context.GetVariable<float>("sideLength",
                             context.GetVariable<float>("faceLength", 0f));
            
            // Update rotation based on face
            _currentRotation = GetRotationForFace(_currentFace);
            
            // Reset distance for this side
            _currentDistance = 0f;
            _currentSplineT = 0f;
            
            // Store section dimensions
            if (_currentFace == 0 || _currentFace == 2) // Front/Back
            {
                _splineSegmentLength = sideLength;
            }
            
            // Minimal visual
            node.Position = GetPositionAlongSpline(_currentSplineT, _currentY);
            node.Rotation = _currentRotation;
            node.Scale = Vector3.one * 0.1f;
            
            context.CurrentPosition = node.Position;
        }
        
        private void HandleStartSection(SpatialNode node, SpatialContext context)
        {
            // Start grid mode (e.g., for roof)
            _gridMode = true;
            
            // Get section dimensions
            _sectionLength = context.GetVariable<float>("length", 0f);
            _sectionWidth = context.GetVariable<float>("width", 0f);
            
            // Reset to start of spline at top height
            _currentSplineT = 0f;
            _currentDistance = 0f;
            _currentY = structureHeight;
            
            // Position at start of section
            node.Position = GetPositionAlongSpline(_currentSplineT, _currentY);
            node.Rotation = GetRotationAlongSpline(_currentSplineT);
            node.Scale = Vector3.one * 0.1f;
            
            context.CurrentPosition = node.Position;
        }
        
        private void HandleAdvanceRow(SpatialNode node, SpatialContext context)
        {
            if (_gridMode)
            {
                // Move to next row in grid (advance along spline width direction)
                float rowSpacing = context.GetVariable<float>("roofTileSize",
                                 context.GetVariable<float>("rowSpacing", 2.0f));
                
                // Reset horizontal position
                _currentDistance = 0f;
                _currentSplineT = 0f;
                
                // This would advance perpendicular to spline, but for simplicity
                // we'll increment a Z offset that gets applied in grid placement
                // Store the current row in context for grid positioning
                int currentRow = context.GetVariable<int>("currentRow", 0);
                context.SetVariable("currentRow", currentRow + 1);
            }
            
            node.Position = GetPositionAlongSpline(_currentSplineT, _currentY);
            node.Rotation = GetRotationAlongSpline(_currentSplineT);
            node.Scale = Vector3.one * 0.1f;
            
            context.CurrentPosition = node.Position;
        }
        
        private void HandleStructuralMarker(SpatialNode node, SpatialContext context)
        {
            // Non-visual grouping node
            node.Position = GetPositionAlongSpline(_currentSplineT, _currentY);
            node.Rotation = _currentRotation;
            node.Scale = Vector3.one * 0.1f;
            
            context.CurrentPosition = node.Position;
        }
        
        private void HandlePlaceElement(SpatialNode node, SpatialContext context, SymbolSpatialConfig config)
        {
            float elementWidth = GetElementWidth(node, context, config);
            
            Vector3 position;
            if (_gridMode)
            {
                // Grid mode: tile along spline path
                position = GetGridPosition(context);
            }
            else
            {
                // Face mode: position along spline with face offset
                position = GetFacePosition();
            }
            
            node.Position = position;
            node.Rotation = GetRotationAlongSpline(_currentSplineT) * _currentRotation;
            node.Scale = Vector3.one;
            
            // Apply geometry
            if (node.GeometryData != null)
            {
                if (_gridMode)
                {
                    // Grid: only scale
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
                    // Face: full geometry
                    ApplyGeometryData(node, context);
                    elementWidth += node.GeometryData.spacing;
                }
            }
            
            // Advance along spline
            _currentDistance += elementWidth;
            if (_splineSegmentLength > 0)
            {
                _currentSplineT = _currentDistance / _splineSegmentLength;
            }
            
            context.CurrentPosition = position;
        }
        
        private Vector3 GetGridPosition(SpatialContext context)
        {
            // Get grid row offset
            int currentRow = context.GetVariable<int>("currentRow", 0);
            float rowSpacing = context.GetVariable<float>("roofTileSize", 2.0f);
            
            // Position along spline + perpendicular row offset
            Vector3 splinePos = GetPositionAlongSpline(_currentSplineT, _currentY);
            Vector3 splineTangent = GetTangentAlongSpline(_currentSplineT);
            Vector3 perpendicular = Vector3.Cross(splineTangent, Vector3.up).normalized;
            
            return splinePos + perpendicular * (currentRow * rowSpacing);
        }
        
        private Vector3 GetFacePosition()
        {
            // Get base position along spline with face-based offset
            Vector3 basePos = GetPositionAlongSpline(_currentSplineT, _currentY);
            
            if (_currentFace == 0) // Front - no offset
            {
                return basePos;
            }
            else if (_currentFace == 1) // Right
            {
                Vector3 tangent = GetTangentAlongSpline(_currentSplineT);
                Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
                return basePos + right * _sectionWidth;
            }
            else if (_currentFace == 2) // Back
            {
                Vector3 tangent = GetTangentAlongSpline(_currentSplineT);
                return basePos - tangent * _sectionLength;
            }
            else // Left (face 3)
            {
                Vector3 tangent = GetTangentAlongSpline(_currentSplineT);
                Vector3 left = -Vector3.Cross(Vector3.up, tangent).normalized;
                return basePos + left * _sectionWidth;
            }
        }
        
        private Vector3 GetPositionAlongSpline(float t, float yOffset)
        {
            if (splineContainer == null || splineContainer.Spline == null || splineContainer.Spline.Count < 2)
            {
                return new Vector3(0, yOffset, 0);
            }
            
            // Evaluate spline at t [0, 1] and transform to world space
            Vector3 splinePoint = (Vector3)splineContainer.EvaluatePosition(t);
            
            // Apply orthogonal offset
            if (Mathf.Abs(orthogonalOffset) > 0.001f)
            {
                Vector3 tangent = ((Vector3)splineContainer.EvaluateTangent(t)).normalized;
                Vector3 perpendicular = Vector3.Cross(tangent, Vector3.up).normalized;
                splinePoint += perpendicular * orthogonalOffset;
            }
            
            splinePoint.y = yOffset;
            return splinePoint;
        }
        
        private Vector3 GetTangentAlongSpline(float t)
        {
            if (splineContainer == null || splineContainer.Spline == null || splineContainer.Spline.Count < 2)
            {
                return Vector3.forward;
            }
            
            return ((Vector3)splineContainer.EvaluateTangent(t)).normalized;
        }
        
        private Quaternion GetRotationAlongSpline(float t)
        {
            Vector3 tangent = GetTangentAlongSpline(t);
            if (tangent.sqrMagnitude < 0.001f)
                return Quaternion.identity;
            
            return Quaternion.LookRotation(tangent, Vector3.up);
        }
        
        private Quaternion GetRotationForFace(int face)
        {
            return face switch
            {
                0 => Quaternion.identity,           // Front (0°)
                1 => Quaternion.Euler(0, 90, 0),    // Right (90°)
                2 => Quaternion.Euler(0, 180, 0),   // Back (180°)
                3 => Quaternion.Euler(0, 270, 0),   // Left (270°)
                _ => Quaternion.identity
            };
        }
        
        private float GetElementWidth(SpatialNode node, SpatialContext context, SymbolSpatialConfig config)
        {
            // Priority: Context parameter > Config default
            if (config != null && !string.IsNullOrEmpty(config.widthParameter))
            {
                float contextWidth = context.GetVariable<float>(config.widthParameter, 0f);
                if (contextWidth > 0f)
                    return contextWidth;
            }
            
            // Fallback to config default
            if (config != null)
                return config.defaultWidth;
            
            return 1.0f;
        }
        
        private void ApplyGeometryData(SpatialNode node, SpatialContext context)
        {
            if (node.GeometryData == null) return;
            
            node.Position += node.Rotation * node.GeometryData.positionOffset;
            node.Rotation *= Quaternion.Euler(node.GeometryData.rotationOffset);
            
            if (node.GeometryData.scaleToParameters)
            {
                node.Scale = node.GeometryData.CalculateScaleFromParameters(node);
            }
            else
            {
                node.Scale = node.GeometryData.GetPhysicalSize();
            }
        }
    }
}
