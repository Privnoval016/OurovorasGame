# Spatial Interpretation System

## Overview

The **Spatial Interpretation System** converts abstract grammar derivation trees into concrete 3D spatial representations. This is a separate architectural layer that maintains clean separation of concerns:

- **Grammar Layer**: Defines WHAT to generate (abstract symbols, parameters, rules)
- **Spatial Layer**: Defines WHERE to place it (positions, rotations, scales in 3D space)

## Architecture

```
GrammarAsset → Grammar Engine → Derivation Tree
                                      ↓
                            Spatial Interpreter
                                      ↓
                              Spatial Graph
                                      ↓
                          Unity Scene Instantiator
                                      ↓
                            GameObjects in Scene
```

## Key Components

### 1. **SpatialNode**
Represents a placed element in 3D space with:
- Position, rotation, scale (concrete transforms)
- Parameters from grammar (width, height, indices)
- Geometry data reference (mesh, prefab)
- Hierarchical relationships (parent/children)

### 2. **SpatialGraph**
Container for the entire spatial scene:
- Root node
- Index of all nodes by ID
- Bounds calculation
- Statistics (terminal count, depth, symbol distribution)

### 3. **SpatialContext**
Tracks state during spatial traversal:
- Current position and orientation
- Transform stack (for hierarchical scopes)
- Context variables (splines, constraints, custom data)
- Helper methods (Advance, Elevate, Rotate, etc.)

### 4. **ISpatialStrategy**
Pluggable placement logic:
- `PlaceNode()`: Positions a single node
- `BeginScope()` / `EndScope()`: Handle hierarchical contexts
- `Initialize()`: Setup before processing

**Built-in Strategies:**
- `VerticalStackStrategy`: Stack floors vertically (for buildings)
- More strategies can be added (SplineFollow, Grid, Radial, etc.)

### 5. **SymbolGeometryData** (ScriptableObject)
Defines physical properties for terminal symbols:
- **Identification**: Symbol name (matches grammar terminal)
- **Geometry**: Prefab or mesh to instantiate
- **Physical Dimensions**: Actual width/height/depth in meters
- **Scaling**: Scale to match grammar parameters or use fixed size
- **Alignment**: How to align relative to placement point
- **Rendering**: Layer, tag, shadow settings

### 6. **SpatialInterpreter**
Main conversion engine:
- Walks derivation tree
- Creates spatial nodes
- Applies strategy for placement
- Looks up geometry data
- Builds spatial graph

### 7. **UnitySceneInstantiator**
Converts spatial graph to GameObjects:
- Instantiates prefabs or creates mesh objects
- Sets transforms (position, rotation, scale)
- Applies rendering settings
- Creates hierarchical or flat structure

## Usage Example

```csharp
// 1. Compile grammar
var engine = new GrammarEngine();
var grammarDef = engine.CompileFromSource(pgrContent, out string error);

// 2. Generate derivation tree
var params = new Dictionary<string, object>
{
    { "width", 100f },
    { "height", 100f },
    { "floors", 4 }
};
var tree = engine.GenerateFromSymbol("Facade", params, 100);

// 3. Create spatial interpreter with strategy
var strategy = new VerticalStackStrategy(heightPerFloor: 25f);
var interpreter = new SpatialInterpreter(strategy);

// 4. Register geometry library
interpreter.RegisterGeometry(windowGeometry);
interpreter.RegisterGeometry(doorGeometry);
interpreter.RegisterGeometry(wallGeometry);

// 5. Interpret tree → spatial graph
var spatialGraph = interpreter.Interpret(tree);

// 6. Instantiate in Unity scene
var instantiator = new UnitySceneInstantiator(parentTransform, hierarchical: true);
var buildingObject = instantiator.Instantiate(spatialGraph);
```

## Creating Geometry Data

1. Create ScriptableObject: `Create > Procedural Grammar > Symbol Geometry Data`
2. Set symbol name (e.g., "Window", "Door", "Wall")
3. Assign prefab or mesh
4. Set physical dimensions (actual size in meters)
5. Configure scaling behavior:
   - **Scale to parameters**: Mesh scales to match grammar's width/height
   - **Fixed dimensions**: Use constant physical size

## Creating Custom Strategies

```csharp
public class MyCustomStrategy : BaseSpatialStrategy
{
    public override string Name => "My Strategy";
    
    public override void PlaceNode(SpatialNode node, SpatialContext context)
    {
        // 1. Read node parameters
        float width = node.GetParameter<float>("width", 1f);
        int index = node.GetParameter<int>("index", 0);
        
        // 2. Calculate position
        node.Position = context.CurrentPosition + new Vector3(index * width, 0, 0);
        node.Rotation = context.CurrentOrientation;
        
        // 3. Apply geometry data (scale, offsets, alignment)
        ApplyGeometryData(node, context);
        
        // 4. Update context for next node
        context.StrafeRight(width);
    }
}
```

## Data Flow: Abstract → Physical

**Grammar (Abstract):**
```
Floor(width=100, height=25, floorNum=2)
  Window(width=20, height=25)
  Window(width=20, height=25)
```

**Spatial Graph (Physical):**
```
Floor @ (0, 50, 0), Rotation: Identity
  Window @ (0, 50, 0), Scale: (20, 25, 1)
  Window @ (20, 50, 0), Scale: (20, 25, 1)
```

**Unity Scene:**
```
Floor_GameObject (position: 0, 50, 0)
  ├─ Window_Prefab (position: 0, 50, 0, scale: 20, 25, 1)
  └─ Window_Prefab (position: 20, 50, 0, scale: 20, 25, 1)
```

## Benefits of This Architecture

1. **Separation of Concerns**: Grammar stays abstract and reusable
2. **Multiple Interpretations**: Same grammar, different placement strategies
3. **Flexibility**: Easy to add new strategies without touching grammar
4. **Testability**: Can validate spatial logic independently
5. **Extensibility**: Custom strategies, geometry data, context variables
6. **Unity Integration**: Clean bridge to GameObjects/Prefabs

## Next Steps

- Implement SplineFollowStrategy for curved buildings
- Add spatial constraints (collision avoidance, alignment)
- Support spatial parameter types (Vector3, Quaternion, Spline references)
- Add spatial debugging visualization tools
