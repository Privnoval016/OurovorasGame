# Procedural Grammar Generator - Frontend System

## Overview
Production-ready entry point with polished custom editor for generating procedural 3D structures from grammar rules.

## File Structure
```
Frontend/
├── Core/
│   ├── ProceduralGenerator.cs          # Main MonoBehaviour component
│   └── ProceduralGeneratorExample.cs   # Runtime usage examples
├── Editor/
│   └── ProceduralGeneratorEditor.cs    # Custom Inspector UI
└── README.md                            # Complete documentation
```

## Key Features

### ProceduralGenerator Component
- **Complete Pipeline**: Grammar → Derivation → Spatial → Mesh
- **Auto-Configuration**: Detects axiom parameters automatically
- **Status Tracking**: Real-time progress and error messages
- **Statistics**: Detailed generation metrics
- **Serialization**: All settings saved with scene

### Custom Editor
- **Intuitive UI**: Collapsible sections, tooltips, help boxes
- **Visual Polish**: Color-coded buttons, formatted parameter fields
- **Smart Validation**: Prevents common errors before generation
- **Live Updates**: Auto-refresh when grammar changes
- **Comprehensive Stats**: Foldout panel with generation details

### Features
- ✅ Grammar asset configuration with validation
- ✅ Spatial strategy selection and parameter editing
- ✅ Geometry library management
- ✅ Auto-populated axiom parameters
- ✅ Advanced options (non-terminals, auto-clear)
- ✅ Large generate button with status feedback
- ✅ Clear button with confirmation dialog
- ✅ Real-time status messages
- ✅ Detailed generation statistics
- ✅ Fully serializable configuration

## Quick Start

1. **Add Component**
   ```
   GameObject → Add Component → ProceduralGenerator
   ```

2. **Configure in Inspector**
   - Assign Grammar Asset
   - Select Spatial Strategy (Vertical Stack)
   - Add Geometry Assets
   - Set Axiom Parameters

3. **Generate**
   - Click large green "GENERATE" button
   - View status and statistics
   - Generated objects appear as children

## Editor UI Sections

| Section | Purpose |
|---------|---------|
| **Grammar Configuration** | Grammar asset, axiom, max iterations |
| **Spatial Strategy** | Strategy type and configuration |
| **Geometry Library** | Visual assets for terminal symbols |
| **Axiom Parameters** | Initial values (auto-populated) |
| **Advanced Options** | Include non-terminals, auto-clear |
| **Generation Controls** | GENERATE and Clear buttons |
| **Status** | Real-time progress messages |
| **Generation Statistics** | Detailed metrics (collapsible) |

## API Usage

### Basic Generation
```csharp
var generator = GetComponent<ProceduralGenerator>();
generator.grammarAsset = myGrammar;
generator.Generate();
```

### With Parameters
```csharp
generator.axiomParameters[0].floatValue = 40f; // length
generator.axiomParameters[1].floatValue = 30f; // width
generator.Generate();
```

### Check Status
```csharp
if (generator.HasGeneration)
{
    Debug.Log($"Status: {generator.GenerationStatus}");
    Debug.Log($"Objects: {generator.LastStats.generatedObjectCount}");
}
```

### Runtime Example
See `ProceduralGeneratorExample.cs` for:
- Auto-generation on start
- Periodic regeneration
- Parameter animation
- Keyboard controls (G=Generate, C=Clear)
- Parameter randomization
- Parameter scaling

## Editor Customization

The custom editor provides:
- **Foldout Headers**: Collapsible sections for organization
- **Help Boxes**: Contextual guidance and warnings
- **Color Coding**: Green for generate, red for clear
- **Smart Fields**: Appropriate editors for each parameter type
- **Auto-Update**: Refreshes when grammar or axiom changes
- **Validation**: Checks before generation
- **Statistics**: Expandable metrics panel

## Keyboard Shortcuts (with Example Script)
- **G**: Generate
- **C**: Clear

## Integration Examples

### UI Button
```csharp
public void OnGenerateButton()
{
    GetComponent<ProceduralGenerator>().Generate();
}
```

### Unity Event
```csharp
// Add ProceduralGenerator.Generate() to button's onClick event
```

### Timeline/Animation
```csharp
public void OnTimelineGenerate()
{
    var gen = FindObjectOfType<ProceduralGenerator>();
    gen.axiomParameters[0].floatValue = animatedValue;
    gen.Generate();
}
```

## Design Philosophy

### User Experience
- **Simplicity**: One-click generation
- **Clarity**: Clear status messages and validation
- **Discoverability**: Tooltips and help boxes throughout
- **Visual Feedback**: Colors, icons, formatted text
- **Error Prevention**: Validation before execution

### Architecture
- **Separation**: Core logic in MonoBehaviour, UI in Editor
- **Extensibility**: Easy to add new strategies
- **Maintainability**: Clean, documented code
- **Serialization**: Everything persists correctly
- **Performance**: Efficient Inspector updates

## Next Steps

After setting up the frontend:
1. ✅ Create simple grammar
2. ✅ Test with basic parameters
3. ✅ Add geometry assets
4. ✅ Experiment with strategies
5. → **Next**: Mesh baking system (upcoming)

## Comparison: MeshTester vs ProceduralGenerator

| Feature | MeshTester | ProceduralGenerator |
|---------|------------|---------------------|
| Purpose | Testing | Production |
| Editor | Default | Custom |
| UI Polish | Basic | Professional |
| Help/Guidance | Minimal | Extensive |
| Status Display | Console | Inspector |
| Statistics | Console | Inspector Panel |
| Validation | Basic | Comprehensive |
| Organization | Flat | Sectioned |
| User Experience | Developer | End-User |

## Technical Details

### Serialization
- Uses `[SerializeReference]` for polymorphic strategy
- `GenerationStats` struct for metrics
- All parameters properly serialized
- Scene integration fully supported

### Editor Updates
- `OnValidate` pattern for grammar changes
- Property drawer for parameters
- Custom foldout state management
- Proper dirty flag handling

### Error Handling
- Pre-generation validation
- Try-catch with detailed messages
- Status updates at each stage
- Console logging for debugging

## Documentation
- [Complete README](README.md) - Full usage guide
- [Main Documentation](../Documentation/INDEX.md) - System overview
- [Spatial System](../Documentation/SPATIAL_SYSTEM.md) - Strategy details
- [Grammar Quickstart](../Documentation/QUICKSTART.md) - Grammar basics

---

**Ready to generate!** Add the component, configure in Inspector, click GENERATE.
