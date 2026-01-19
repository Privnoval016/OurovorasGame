namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Defines a contract for what parameters terminal symbols must provide.
    /// Parameter contracts validate that the grammar output is compatible with a strategy.
    /// 
    /// This separates concerns:
    /// - Grammar defines WHAT parameters terminal symbols have
    /// - Contracts validate WHICH parameters a strategy needs
    /// - Strategies use contracts to access parameters in a type-safe way
    /// - Geometry data defines mesh-specific properties (alignment, pivot, etc.)
    /// </summary>
    public interface IParameterContract
    {
        /// <summary>
        /// Name of this parameter contract (for debugging)
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Validate that a node has all required parameters for this contract
        /// </summary>
        bool ValidateNode(SpatialNode node);
        
        /// <summary>
        /// Get a description of what parameters are missing (for error reporting)
        /// </summary>
        string GetMissingParameters(SpatialNode node);
        
        /// <summary>
        /// Get a description of what parameters this contract expects
        /// </summary>
        string GetExpectedParameters();
    }
}
