# Grammar Visual Editor - Quick Start Guide

## Overview

The Procedural Grammar Visual Editor provides a professional, user-friendly interface for creating and editing grammar definitions without writing code.

## Creating a New Grammar Asset

### Method 1: Project Window
1. Right-click in the Project window
2. Select `Create > Procedural Grammar > Grammar Asset`
3. Name your asset (e.g., "BuildingGrammar")

### Method 2: Editor Window
1. Open `Window > Procedural Grammar > Grammar Editor`
2. Click the "New" button in the toolbar
3. Choose a location and name for your asset

## Using the Grammar Editor

### Opening the Editor

**Option 1:** Menu
- Go to `Window > Procedural Grammar > Grammar Editor`
- Select your grammar asset from the dropdown

**Option 2:** Inspector
- Select a Grammar Asset in the Project window
- Click "Open in Grammar Editor" button in the Inspector

### Editor Layout

The editor has 4 main tabs:

#### 1. Symbols Tab
Define the building blocks of your grammar.

**Creating a Symbol:**
1. Click "+ Add Symbol"
2. Enter a name (e.g., "Building", "Window", "Floor")
3. Add parameters:
   - Click "+ Add Parameter"
   - Set name, type, and default value
   - For spatial types (Spline, Heightmap, PointField), assign Unity objects

**Parameter Types:**
- `Float`, `Int`, `String` - Basic values
- `Vector2`, `Vector3` - Unity vectors
- `SplineCurve` - Path definitions
- `Heightmap` - Height field data
- `PointField` - Scattered 3D points
- `SpatialConstant` - Constant spatial value

#### 2. Rules Tab
Define how symbols transform into other symbols.

**Creating a Rule:**
1. Click "+ Add Rule"
2. Name your rule (e.g., "ExpandBuilding")
3. Select a **Predecessor** (left side):
   - Choose which symbol this rule applies to
   - Parameters are automatically copied from the symbol definition

4. Define **Productions** (right side):
   - Each production is one possible outcome
   - Use multiple productions for randomness (set weights)
   - Add conditions to control when productions fire

**Production Steps:**
- **Symbol**: Output a symbol with parameters
  - Select the symbol type
  - Assign values to its parameters (can reference input parameters)
  
- **SetParameter**: Set a parameter value
  - Useful for modifying state
  
- **ModifyParameter**: Apply mathematical operations
  - Add, Subtract, Multiply, Divide parameter values

**Example Rule:**
```
Rule: ExpandFloor
Predecessor: Floor(height, width)
Production 1 [0.7]:
  - Step 1: Room(height=height, width=width/2)
  - Step 2: Room(height=height, width=width/2)
Production 2 [0.3]:
  - Step 1: LargeRoom(height=height, width=width)
```

#### 3. Settings Tab
Configure grammar-level settings.

- **Grammar Name**: Identifier for this grammar
- **Axiom**: The starting symbol (where derivation begins)
- **Max Iterations**: Maximum recursion depth (prevents infinite loops)
- **Export Path**: Default location for .pgr file export

#### 4. Export Tab
Import/Export and preview .pgr format.

- **Export to .PGR**: Save grammar as text file
- **Import from .PGR**: Load existing grammar file
- **Preview**: See the generated .pgr code

## Workflow: Visual Design → Code → Execution

### Step 1: Design Visually
Create your grammar using the visual editor:
1. Define symbols (Symbols tab)
2. Create production rules (Rules tab)
3. Set axiom and iterations (Settings tab)

### Step 2: Validate
- Check the footer for validation status
- Green checkmark = ready to use
- Yellow warning = click to see errors

### Step 3: Export (Optional)
- Export to .pgr for version control
- Share grammar files with team members
- Can reimport later

### Step 4: Use in Code

```csharp
using ProceduralGrammarGeneration.GrammarParsing;
using UnityEngine;

public class ProceduralGenerator : MonoBehaviour
{
    public GrammarAsset grammarAsset;
    
    void Start()
    {
        // Option 1: Compile from asset directly
        var engine = GrammarAssetIO.CompileGrammar(grammarAsset);
        
        // Option 2: Export to string and compile
        string pgrCode = GrammarAssetIO.ExportToPGR(grammarAsset);
        var engine2 = new GrammarEngine();
        engine2.CompileGrammarFromString(pgrCode);
        
        // Execute grammar
        var result = engine.Execute(grammarAsset.axiom, grammarAsset.maxIterations);
        
        // Process result...
        Debug.Log($"Generated {result.NodeCount} nodes");
    }
}
```

## Example: Simple Building Grammar

### Symbols
1. **Building** (floors: int = 3)
2. **Floor** (height: float = 3.0)
3. **Wall** (length: float = 10.0)
4. **Window** (size: float = 1.0)

### Rules

**Rule 1: Building → Floors**
- Predecessor: Building(floors)
- Production: Repeat `Floor(height=3.0)` floors times

**Rule 2: Floor → Walls**
- Predecessor: Floor(height)
- Production: 
  - Wall(length=10, height=height)
  - Wall(length=10, height=height)
  - Wall(length=8, height=height)
  - Wall(length=8, height=height)

**Rule 3: Wall → Windows**
- Predecessor: Wall(length, height)
- Production (if length > 5):
  - Window(size=1.0)
  - Window(size=1.0)

### Settings
- Axiom: `Building`
- Max Iterations: 10

## Working with Spatial Data

Spatial parameters allow Unity editor tools to define procedural paths:

### Setup Spline Path
1. Create a symbol: `TreeTrunk(path: SplineCurve)`
2. In Unity scene, create a Spline using Unity Splines package
3. Assign the spline to the parameter's "Spatial Data" field
4. At runtime, use `UnitySpatialConverter` to convert to grammar format

```csharp
var converter = new UnitySpatialConverter();
var splineCurve = converter.ConvertSpline(unitySplineComponent);

// Create symbol with spatial data
var symbol = new Symbol(trunkType);
symbol.SetSpatialParameter("path", splineCurve);
```

## Tips & Best Practices

1. **Start Simple**: Begin with 2-3 symbols and basic rules, then expand
2. **Name Clearly**: Use descriptive names (Building, Floor, Wall vs. A, B, C)
3. **Use Weights**: Add variety with weighted productions (70% small, 30% large)
4. **Add Conditions**: Control generation with parameter conditions
5. **Test Early**: Use "Test Compile" button frequently to catch errors
6. **Version Control**: Export to .pgr files for Git tracking

## Integration with Backend System

The visual editor integrates seamlessly with the existing grammar backend:

- **Lexer, Parser, Analyzer**: Used during import/export
- **FileIO Interface**: GrammarAssetIO implements the bridge
- **Spatial System**: Full support for splines, heightmaps, point fields
- **Execution Engine**: Compiles assets to executable grammars

You can freely mix:
- Visual editor for quick iteration
- .pgr files for version control
- Code-based generation for runtime procedural content

## Troubleshooting

**Error: "Symbol not defined"**
- Ensure all symbols referenced in rules exist in Symbols tab

**Error: "Axiom not defined"**
- Set the Axiom in Settings tab to match an existing symbol

**Compilation fails**
- Click "Show Errors" in the footer to see details
- Check parameter names match between symbols and rules

**Spatial data not working**
- Ensure Unity Splines package is installed
- Verify object assignment in parameter fields
- Check UnitySpatialConverter is used at runtime

## Next Steps

1. Create your first grammar asset
2. Define a simple symbol (e.g., "Start")
3. Add a basic rule
4. Test compile
5. Export and use in your project!

For more details, see the main documentation in the Documentation/ folder.
