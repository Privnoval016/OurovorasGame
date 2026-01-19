using UnityEngine;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Base class for spatial strategies with common functionality.
    /// Provides helper methods for parameter validation and geometry application.
    /// </summary>
    [System.Serializable]
    public abstract class BaseSpatialStrategy : ISpatialStrategy
    {
        public abstract string Name { get; }
        public abstract IParameterContract RequiredContract { get; }
        
        public virtual void Initialize(SpatialContext context)
        {
            // Default: no initialization needed
        }
        
        public virtual void BeginScope(SpatialNode parent, SpatialContext context)
        {
            // Default: push transform state
            context.PushTransform();
        }
        
        public virtual void EndScope(SpatialNode parent, SpatialContext context)
        {
            // Default: pop transform state
            context.PopTransform();
        }
        
        public abstract void PlaceNode(SpatialNode node, SpatialContext context);
        
        /// <summary>
        /// Validate that a node satisfies the required parameter contract.
        /// Call this at the start of PlaceNode() to ensure parameters are valid.
        /// Only validates terminal symbols (nodes with geometry data).
        /// </summary>
        protected bool ValidateNodeParameters(SpatialNode node)
        {
            if (RequiredContract == null)
                return true; // No contract = no validation needed
            
            // Skip validation for non-terminal symbols (they don't need physical dimensions)
            if (node.GeometryData == null)
                return true;
            
            if (!RequiredContract.ValidateNode(node))
            {
                string missing = RequiredContract.GetMissingParameters(node);
                UnityEngine.Debug.LogWarning(
                    $"Node '{node.SymbolName}' missing required parameters for {Name} strategy. " +
                    $"Missing: {missing}. Expected: {RequiredContract.GetExpectedParameters()}");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Helper: Apply geometry data to a node (scale calculation, offsets, etc.)
        /// This respects mesh-specific properties like alignment, pivot, rotation offsets.
        /// </summary>
        protected void ApplyGeometryData(SpatialNode node, SpatialContext context)
        {
            if (node.GeometryData == null)
                return;
            
            // Calculate scale from parameters if enabled
            if (node.GeometryData.scaleToParameters)
            {
                node.Scale = node.GeometryData.CalculateScaleFromParameters(node);
            }
            else
            {
                node.Scale = node.GeometryData.GetPhysicalSize();
            }
            
            // Apply position offset
            node.Position += context.CurrentOrientation * node.GeometryData.positionOffset;
            
            // Apply rotation offset
            if (node.GeometryData.rotationOffset != Vector3.zero)
            {
                node.Rotation *= Quaternion.Euler(node.GeometryData.rotationOffset);
            }
            
            // Apply alignment adjustments
            ApplyAlignment(node, context);
        }
        
        /// <summary>
        /// Helper: Apply alignment mode from geometry data
        /// </summary>
        protected void ApplyAlignment(SpatialNode node, SpatialContext context)
        {
            if (node.GeometryData == null)
                return;
            
            Vector3 alignmentOffset = Vector3.zero;
            Vector3 size = node.Scale;
            
            switch (node.GeometryData.alignment)
            {
                case AlignmentMode.Bottom:
                    alignmentOffset = context.Up * (size.y * 0.5f);
                    break;
                case AlignmentMode.Top:
                    alignmentOffset = -context.Up * (size.y * 0.5f);
                    break;
                case AlignmentMode.Left:
                    alignmentOffset = context.Right * (size.x * 0.5f);
                    break;
                case AlignmentMode.Right:
                    alignmentOffset = -context.Right * (size.x * 0.5f);
                    break;
                case AlignmentMode.BottomLeft:
                    alignmentOffset = context.Up * (size.y * 0.5f) + context.Right * (size.x * 0.5f);
                    break;
                case AlignmentMode.Custom:
                    alignmentOffset = context.CurrentOrientation * node.GeometryData.GetPivotPosition();
                    break;
                case AlignmentMode.Center:
                case AlignmentMode.Default:
                default:
                    // No adjustment needed
                    break;
            }
            
            node.Position += alignmentOffset;
        }
    }
}
