namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Interface for spatial placement strategies.
    /// Strategies define HOW to place grammar symbols in 3D space.
    /// Different strategies enable different architectural styles (vertical stacking, spline following, etc.)
    /// </summary>
    public interface ISpatialStrategy
    {
        /// <summary>
        /// Name of this strategy (for debugging and selection)
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Place a single spatial node in 3D space.
        /// This method should:
        /// 1. Read node parameters (width, height, indices, etc.)
        /// 2. Use context state (current position, orientation)
        /// 3. Apply geometry data (physical dimensions, offsets)
        /// 4. Set node.Position, node.Rotation, node.Scale
        /// 5. Update context for next node (advance position, rotate, etc.)
        /// </summary>
        /// <param name="node">The spatial node to place</param>
        /// <param name="context">Current spatial context (position, orientation, variables)</param>
        void PlaceNode(SpatialNode node, SpatialContext context);
        
        /// <summary>
        /// Called before processing a set of child nodes.
        /// Can push transform state, modify context, etc.
        /// </summary>
        void BeginScope(SpatialNode parent, SpatialContext context);
        
        /// <summary>
        /// Called after processing a set of child nodes.
        /// Should restore context state if modified in BeginScope.
        /// </summary>
        void EndScope(SpatialNode parent, SpatialContext context);
        
        /// <summary>
        /// Initialize the strategy with any required setup data.
        /// </summary>
        void Initialize(SpatialContext context);
    }
}
