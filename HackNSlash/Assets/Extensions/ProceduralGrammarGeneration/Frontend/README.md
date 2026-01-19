# Procedural Grammar Generator - Frontend

## Overview

The **ProceduralGenerator** component is the production-ready entry point for creating procedural 3D structures from grammar rules. It provides a complete, streamlined pipeline from grammar definition to generated meshes with an intuitive custom editor interface.

## Quick Start

### 1. Setup
1. Create an empty GameObject in your scene
2. Add the `ProceduralGenerator` component
3. The custom editor UI will appear in the Inspector

### 2. Configure Grammar
1. **Grammar Asset**: Assign your grammar asset (create via `Assets > Create > Procedural Grammar > Grammar Asset`)
2. **Max Iterations**: Set maximum rule expansion iterations (default: 100)

The editor will automatically display:
- Axiom symbol name
- Number of symbols and rules
- Parameter fields for the axiom (auto-populated)

### 3. Configure Spatial Strategy
1. **Strategy Type**: Select your spatial interpretation strategy (currently: Vertical Stack)
2. **Strategy Settings**: Expand to configure strategy-specific parameters
   - For Vertical Stack: Configure `ConfigManager` to set symbol behaviors
   - Adjust layer heights and other strategy parameters

### 4. Add Geometry Assets
1. **Geometry Library**: Add SymbolGeometryData assets for terminal symbols
   - Each geometry asset defines visual representation for a symbol type (Window, Door, Wall, etc.)
   - Without geometry assets, cube placeholders are used

### 5. Set Parameters
1. **Axiom Parameters**: Configure initial values for your grammar's axiom symbol
   - Parameters are automatically detected from the grammar
   - Supported types: Float, Int, String, Bool, Vector3, Color
   - Example: For a Building grammar, set `length`, `width`, `height`, etc.

### 6. Generate
1. Click the large **GENERATE** button
2. Watch the status bar for progress
3. Generated structure appears as children of the GameObject

## Editor Interface

### Sections

#### Grammar Configuration
- **Grammar Asset**: The grammar defining generation rules
- **Axiom Symbol**: Starting symbol (auto-detected, read-only)
- **Symbols/Rules**: Grammar statistics (auto-detected, read-only)
- **Max Iterations**: Maximum expansion iterations

#### Spatial Strategy
- **Strategy Type**: Dropdown to select strategy
- **Strategy Configuration**: Expandable section showing all strategy parameters
  - Configure symbol behaviors (ResetVertical, ChangeSide, PlaceElement, etc.)
  - Set default dimensions and parameters

#### Geometry Library
- **Geometry Assets**: Array of SymbolGeometryData assets
- Each asset maps to a terminal symbol name
- Defines mesh, material, dimensions, and placement offsets

#### Axiom Parameters
- Auto-populated based on grammar axiom definition
- Each parameter shown in its appropriate editor field
- Values preserved when switching grammars

#### Advanced Options
- **Include Non-Terminals**: Include non-terminal nodes in hierarchy (for debugging)
- **Auto Clear Previous**: Automatically clear previous generation before creating new

#### Generation Controls
- **GENERATE**: Large green button to start generation
- **Clear Generated Content**: Red button to remove generated content (requires confirmation)

#### Status
- Real-time status messages during generation
- Error messages displayed with context
- Color-coded message types (Info, Warning, Error)

#### Generation Statistics
- Foldout showing detailed stats from last generation
- Grammar name and configuration
- Derivation depth and terminal count
- Generated object count
- Bounding box information

## Features

### Automatic Parameter Detection
When you assign or change a grammar asset, the editor automatically:
- Detects the axiom symbol
- Creates parameter fields for all axiom parameters
- Preserves existing parameter values when possible

### Visual Feedback
- Color-coded buttons (green for generate, red for clear)
- Status messages with appropriate message types
- Collapsible sections to reduce clutter
- Tooltips on all fields

### Error Handling
- Clear error messages in status bar
- Validation before generation
- Graceful failure with detailed logs

### Serialization
- All configuration is saved with the scene
- Strategy settings fully serializable
- Parameter values preserved across sessions

## Spatial Strategy Configuration

### Vertical Stack Strategy
The default strategy for vertically stacked structures (buildings, towers, etc.)

**Key Settings:**
- **Default Layer Height**: Height of each vertical layer
- **Config Manager**: Behavior configuration for symbols
  - **PlaceElement**: Standard element placement (doors, windows, etc.)
  - **ResetVertical**: Reset Y position (floor markers)
  - **ChangeSide**: Change face/rotation (side markers)
  - **StartSection**: Start new section (roof marker)
  - **AdvanceRow**: Move to next row (roof row)
  - **StructuralMarker**: Non-visual grouping symbol

**Symbol Configuration:**
Each symbol can have:
- **Behavior**: Spatial behavior type
- **Width Parameter**: Context variable name for width (e.g., "windowSize")
- **Default Width**: Fallback width if parameter not found
- **Marker Scale**: Scale for structural markers

## Example Workflow

### Creating a Building
```
1. Assign ComplexBuilding grammar asset
2. Set Vertical Stack strategy with layer height 3.5
3. Add geometry assets: Window, Door, Wall, RoofTile
4. Configure parameters:
   - length: 40
   - width: 30
   - height: 14
   - windowSize: 2
   - doorWidth: 1.5
   - wallWidth: 2
   - roofTileSize: 2
   - floorHeight: 3.5
5. Click GENERATE
6. Generated building appears with 4 floors and tiled roof
```

## Tips & Best Practices

### Performance
- Start with low max iterations (~50) and increase as needed
- Use "Include Non-Terminals" only for debugging
- Clear previous generation to free memory

### Debugging
- Enable "Include Non-Terminals" to see full grammar hierarchy
- Check status messages for detailed error information
- Review generation statistics after each run

### Organization
- Keep geometry assets organized in folders by symbol type
- Use descriptive names for parameters
- Document custom grammars with comments

### Strategy Configuration
- Use StructuralMarker for non-visual grouping symbols
- Configure width parameters to match grammar parameter names
- Set appropriate default widths as fallbacks

## API Reference

### Public Methods
```csharp
// Generate procedural content
void Generate()

// Clear generated content
void Clear()

// Update axiom parameters (called automatically by editor)
void UpdateAxiomParameters()
```

### Public Properties
```csharp
// Current generation status message
string GenerationStatus { get; }

// Statistics from last generation
GenerationStats LastStats { get; }

// Whether generation currently exists
bool HasGeneration { get; }
```

## Integration

### From Code
```csharp
var generator = GetComponent<ProceduralGenerator>();

// Assign grammar
generator.grammarAsset = myGrammarAsset;

// Configure parameters
generator.axiomParameters[0].floatValue = 40f; // length
generator.axiomParameters[1].floatValue = 30f; // width

// Generate
generator.Generate();

// Access stats
Debug.Log($"Generated {generator.LastStats.generatedObjectCount} objects");
```

### From Unity Events
```csharp
// Add to button onClick event
public void OnGenerateButtonClick()
{
    GetComponent<ProceduralGenerator>().Generate();
}
```

## Troubleshooting

### "No grammar asset assigned"
- Assign a GrammarAsset in the Grammar Configuration section

### "Spatial strategy not assigned"
- Select a strategy type from the dropdown
- Strategy should auto-create, but you can manually assign if needed

### "Cube placeholders instead of geometry"
- Add SymbolGeometryData assets to the Geometry Library
- Ensure geometry asset names match terminal symbol names

### "Generated structure is empty"
- Check max iterations is high enough
- Verify grammar has valid rules
- Check axiom parameters are correct

### "Parameters not showing"
- Ensure grammar asset is assigned
- Grammar must have an axiom symbol defined
- Click the grammar asset to force refresh

## Next Steps

After setting up your generator:
1. Test with simple grammars first
2. Gradually increase complexity
3. Experiment with different parameter values
4. Create custom geometry assets for better visuals
5. Configure strategy behaviors for your specific needs

For more information, see:
- [Grammar System Documentation](../Documentation/INDEX.md)
- [Spatial System Documentation](../Documentation/SPATIAL_SYSTEM.md)
- [Parameter Contracts](../Documentation/PARAMETER_CONTRACTS.md)
