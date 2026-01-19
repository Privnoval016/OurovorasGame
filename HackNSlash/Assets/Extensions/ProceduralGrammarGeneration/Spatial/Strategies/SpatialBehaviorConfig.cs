using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Defines different spatial behaviors for symbols during placement.
    /// Extensible enum for various positioning strategies.
    /// </summary>
    public enum SpatialBehavior
    {
        /// <summary>Standard element placement (default)</summary>
        PlaceElement,
        
        /// <summary>Resets vertical position (e.g., floor markers)</summary>
        ResetVertical,
        
        /// <summary>Changes rotation/side (e.g., building side markers)</summary>
        ChangeSide,
        
        /// <summary>Starts a new section (e.g., roof marker)</summary>
        StartSection,
        
        /// <summary>Advances to next row (e.g., roof row)</summary>
        AdvanceRow,
        
        /// <summary>Structural marker with minimal visual (e.g., grouping symbols)</summary>
        StructuralMarker
    }
    
    /// <summary>
    /// Configuration for how a specific symbol behaves spatially.
    /// Maps symbol names to behaviors and sizing parameters.
    /// </summary>
    [Serializable]
    public class SymbolSpatialConfig
    {
        [Tooltip("Name of the symbol this config applies to")]
        public string symbolName;
        
        [Tooltip("Spatial behavior for this symbol")]
        public SpatialBehavior behavior = SpatialBehavior.PlaceElement;
        
        [Tooltip("Context variable name for element width (e.g., 'windowSize', 'doorWidth')")]
        public string widthParameter = "";
        
        [Tooltip("Default width if parameter not found in context")]
        public float defaultWidth = 1.0f;
        
        [Tooltip("Scale multiplier for marker symbols (non-visual)")]
        public float markerScale = 0.1f;
        
        public SymbolSpatialConfig() { }
        
        public SymbolSpatialConfig(string name, SpatialBehavior behavior, string widthParam = "", float defaultWidth = 1.0f)
        {
            this.symbolName = name;
            this.behavior = behavior;
            this.widthParameter = widthParam;
            this.defaultWidth = defaultWidth;
        }
    }
    
    /// <summary>
    /// Helper class to manage symbol spatial configurations.
    /// Provides lookup and default configurations.
    /// </summary>
    [Serializable]
    public class SpatialConfigManager
    {
        [SerializeField]
        private List<SymbolSpatialConfig> _configs = new List<SymbolSpatialConfig>();
        
        public List<SymbolSpatialConfig> Configs => _configs;
        
        /// <summary>
        /// Get configuration for a specific symbol name.
        /// Returns null if no config found.
        /// </summary>
        public SymbolSpatialConfig GetConfig(string symbolName)
        {
            return _configs.FirstOrDefault(c => c.symbolName == symbolName);
        }
        
        /// <summary>
        /// Check if a symbol has a specific behavior configured.
        /// </summary>
        public bool HasBehavior(string symbolName, SpatialBehavior behavior)
        {
            var config = GetConfig(symbolName);
            return config != null && config.behavior == behavior;
        }
        
        /// <summary>
        /// Get width parameter name for a symbol.
        /// Returns null if no config or no parameter defined.
        /// </summary>
        public string GetWidthParameter(string symbolName)
        {
            var config = GetConfig(symbolName);
            return config != null && !string.IsNullOrEmpty(config.widthParameter) 
                ? config.widthParameter 
                : null;
        }
        
        /// <summary>
        /// Get default width for a symbol.
        /// </summary>
        public float GetDefaultWidth(string symbolName)
        {
            var config = GetConfig(symbolName);
            return config?.defaultWidth ?? 1.0f;
        }
        
        /// <summary>
        /// Add a configuration for a symbol.
        /// </summary>
        public void AddConfig(SymbolSpatialConfig config)
        {
            // Remove existing config for same symbol
            _configs.RemoveAll(c => c.symbolName == config.symbolName);
            _configs.Add(config);
        }
        
        /// <summary>
        /// Create default configuration for common building symbols.
        /// Useful as a starting template.
        /// </summary>
        public static SpatialConfigManager CreateBuildingDefaults()
        {
            var manager = new SpatialConfigManager();
            
            // Structural markers
            manager.AddConfig(new SymbolSpatialConfig("FloorMarker", SpatialBehavior.ResetVertical));
            manager.AddConfig(new SymbolSpatialConfig("FloorSide", SpatialBehavior.ChangeSide));
            manager.AddConfig(new SymbolSpatialConfig("Roof", SpatialBehavior.StartSection));
            manager.AddConfig(new SymbolSpatialConfig("RoofRow", SpatialBehavior.AdvanceRow, "roofTileSize", 2.0f));
            
            // Floor elements
            manager.AddConfig(new SymbolSpatialConfig("Window", SpatialBehavior.PlaceElement, "windowSize", 2.0f));
            manager.AddConfig(new SymbolSpatialConfig("Door", SpatialBehavior.PlaceElement, "doorWidth", 1.5f));
            manager.AddConfig(new SymbolSpatialConfig("Wall", SpatialBehavior.PlaceElement, "wallWidth", 2.0f));
            
            // Roof elements
            manager.AddConfig(new SymbolSpatialConfig("RoofTile", SpatialBehavior.PlaceElement, "roofTileSize", 2.0f));
            
            // Grouping symbols (non-visual)
            manager.AddConfig(new SymbolSpatialConfig("Floor", SpatialBehavior.StructuralMarker));
            manager.AddConfig(new SymbolSpatialConfig("ElementRow", SpatialBehavior.StructuralMarker));
            manager.AddConfig(new SymbolSpatialConfig("RoofTileRow", SpatialBehavior.StructuralMarker));
            
            return manager;
        }
    }
}
