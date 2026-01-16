# Spatial Data System

## Overview

The spatial data system provides Unity-independent spatial constraints for procedural generation. It allows you to define paths, height variations, placement points, and other spatial data that guide the procedural models without coupling the grammar backend to Unity.

## Architecture

### Backend (Unity-Independent)

```
SpatialTypes.cs          - Core spatial data structures
├── ISpatialData         - Base interface for all spatial types
├── Vector3D             - Standalone 3D vector (no Unity dependency)
├── SplineCurve          - Smooth curves for paths
├── Heightmap            - 2D height fields for elevation/variation
├── PointField           - Scattered 3D points for placement
└── SpatialConstant      - Constant spatial values
```

### Unity Integration Layer

```
ISpatialDataConverter.cs       - Converter interface
UnitySpatialConverter.cs       - Unity → Grammar conversion
```

## Spatial Data Types

### 1. SplineCurve

**Purpose**: Define smooth paths for buildings, tree trunks, roads, etc.

**Features**:
- Catmull-Rom interpolation for smooth curves
- Open or closed loops
- Tangent calculation at any point
- Distance-based evaluation
- Automatic length calculation

**Usage**:
```csharp
// Create a building path
var controlPoints = new List<Vector3D>
{
    new Vector3D(0, 0, 0),
    new Vector3D(10, 2, 5),
    new Vector3D(20, 1, 10),
    new Vector3D(30, 3, 12)
};

var spline = new SplineCurve(controlPoints, isClosed: false);

// Sample at normalized position (0-1)
Vector3D position = spline.Evaluate(0.5f);      // Midpoint
Vector3D tangent = spline.GetTangent(0.5f);     // Direction at midpoint

// Sample by distance
Vector3D pos10m = spline.EvaluateByDistance(10f); // 10 units along path
```

**Unity Integration**:
```csharp
// In Unity Editor
#if UNITY_EDITOR
var unitySpline = GetComponent<UnityEngine.Splines.Spline>();
var converter = new UnitySpatialConverter();
var grammarSpline = converter.ConvertSpline(unitySpline);

// Pass to grammar
var symbol = new Symbol(buildingType);
symbol.SetSpatialParameter("path", grammarSpline);
#endif
```

### 2. Heightmap

**Purpose**: Define height variations for building profiles, terrain elevation, facade waves, etc.

**Features**:
- 2D height field with world-space size
- Bilinear interpolation for smooth sampling
- UV-based or world-space sampling
- Efficient storage (single-precision float array)

**Usage**:
```csharp
// Create heightmap (16x16 resolution, 50x30x50 world size)
var heightmap = new Heightmap(16, 16, new Vector3D(50, 30, 50));

// Set height values (e.g., from procedural noise)
for (int y = 0; y < 16; y++)
{
    for (int x = 0; x < 16; x++)
    {
        float height = GenerateHeight(x, y); // Your algorithm
        heightmap.SetValue(x, y, height);
    }
}

// Sample at UV coordinates (0-1, 0-1)
float height = heightmap.Sample(0.5f, 0.5f);

// Sample at world position
float heightAtPos = heightmap.SampleWorld(25f, 25f);
```

**Unity Integration**:
```csharp
// Convert from Unity Terrain
var terrain = GetComponent<Terrain>();
var converter = new UnitySpatialConverter();
var grammarHeightmap = converter.ConvertHeightmap(
    terrain, 
    width: 64, 
    height: 64, 
    worldSize: new Vector3D(100, 50, 100)
);

// Or from Texture2D (grayscale)
var texture = Resources.Load<Texture2D>("HeightProfile");
var grammarHeightmap = converter.ConvertHeightmap(
    texture,
    width: 32,
    height: 32,
    worldSize: new Vector3D(50, 20, 50)
);
```

### 3. PointField

**Purpose**: Define scattered placement points for windows, doors, vegetation, decorations, etc.

**Features**:
- Collection of 3D points with optional metadata
- Nearest point queries
- Radius-based queries
- Metadata support (scale, rotation, type)

**Usage**:
```csharp
// Create point field
var points = new List<Vector3D>
{
    new Vector3D(5, 2, 0),
    new Vector3D(10, 2, 0),
    new Vector3D(15, 2, 0),
    new Vector3D(20, 2, 0)
};

var pointField = new PointField(points);

// Add metadata
pointField.Metadata[0]["type"] = "door";
pointField.Metadata[1]["type"] = "window";

// Find nearest point
int nearestIdx = pointField.FindNearest(new Vector3D(12, 2, 0));

// Find all points in radius
var nearbyPoints = pointField.FindInRadius(center, radius: 5f);
```

**Unity Integration**:
```csharp
// Convert from Transform array
var windowTransforms = windowParent.GetComponentsInChildren<Transform>();
var converter = new UnitySpatialConverter();
var grammarPointField = converter.ConvertPointField(windowTransforms);

// Or from GameObject array
var placementObjects = FindGameObjectsWithTag("Placement");
var grammarPointField = converter.ConvertPointField(placementObjects);

// Or from ParticleSystem
var particles = GetComponent<ParticleSystem>();
var grammarPointField = converter.ConvertPointField(particles);
```

### 4. SpatialConstant

**Purpose**: Wrap constant values as spatial parameters for consistency.

**Usage**:
```csharp
var constant = new SpatialConstant(5.0f);
symbol.SetSpatialParameter("baseHeight", constant);

float value = constant.GetValue<float>();
```

## Integration with Grammar System

### Adding Spatial Parameters to Symbols

```csharp
// Create symbol
var buildingType = new SymbolType("Building", 0);
var symbol = new Symbol(buildingType);

// Add regular parameters
symbol.Parameters["width"] = 20f;
symbol.Parameters["height"] = 30f;

// Add spatial parameters
var path = new SplineCurve(pathPoints, false);
symbol.SetSpatialParameter("path", path);

var heightProfile = new Heightmap(16, 16, new Vector3D(20, 30, 20));
symbol.SetSpatialParameter("heightProfile", heightProfile);

var windowPlacements = new PointField(windowPoints);
symbol.SetSpatialParameter("windowPlacements", windowPlacements);
```

### Retrieving Spatial Parameters

```csharp
// Get spatial parameter
if (symbol.TryGetSpatialParameter<SplineCurve>("path", out var path))
{
    Vector3D midpoint = path.Evaluate(0.5f);
    // Use midpoint for positioning
}

// Direct get (throws if not found)
var heightProfile = symbol.GetSpatialParameter<Heightmap>("heightProfile");
float centerHeight = heightProfile.Sample(0.5f, 0.5f);
```

## Use Cases

### 1. Building with Path Following

```csharp
// Define building path with spline
var buildingPath = new SplineCurve(new List<Vector3D>
{
    new Vector3D(0, 0, 0),
    new Vector3D(50, 0, 0),
    new Vector3D(100, 0, 20),
    new Vector3D(150, 0, 40)
}, isClosed: false);

// Add height variation along path
var heightProfile = new Heightmap(32, 1, new Vector3D(150, 50, 1));
// Fill with variation data...

// Create building symbol
var building = new Symbol(buildingType);
building.SetSpatialParameter("path", buildingPath);
building.SetSpatialParameter("heightProfile", heightProfile);

// During generation:
// - Sample path to get position and orientation
// - Sample heightProfile to get local height
// - Place floors/walls accordingly
```

### 2. Tree with Trunk Spline

```csharp
// Define trunk growth path
var trunkPath = new SplineCurve(new List<Vector3D>
{
    new Vector3D(0, 0, 0),
    new Vector3D(1, 5, 0.5),
    new Vector3D(0.5, 10, 1),
    new Vector3D(0, 15, 1.5)
}, isClosed: false);

// Branch placement points along trunk
var branchPoints = new PointField(GenerateBranchPoints(trunkPath));

var tree = new Symbol(treeType);
tree.SetSpatialParameter("trunk", trunkPath);
tree.SetSpatialParameter("branches", branchPoints);
```

### 3. Facade with Window Placement

```csharp
// Generate window placement grid with variations
var windowGrid = GenerateWindowGrid(width: 20, height: 10, spacing: 2);

// Add height variation to facade
var facadeHeightmap = GenerateFacadeVariation(20, 10);

var facade = new Symbol(facadeType);
facade.SetSpatialParameter("windowPoints", windowGrid);
facade.SetSpatialParameter("heightVariation", facadeHeightmap);
```

## Unity Editor Workflow

### Phase 1: Define in Unity Editor

1. Create splines using Unity Spline tools
2. Define heightmaps using Terrain or textures
3. Place GameObjects for point fields
4. Preview in Editor

### Phase 2: Convert to Grammar Format

```csharp
public class GrammarSpatialBridge : MonoBehaviour
{
    public UnityEngine.Splines.Spline buildingPath;
    public Terrain heightTerrain;
    public Transform[] windowPlacements;

    public void ConvertToGrammar()
    {
        var converter = new UnitySpatialConverter();
        
        // Convert spatial data
        var grammarPath = converter.ConvertSpline(buildingPath);
        var grammarHeight = converter.ConvertHeightmap(
            heightTerrain, 64, 64, new Vector3D(100, 50, 100)
        );
        var grammarWindows = converter.ConvertPointField(windowPlacements);
        
        // Pass to grammar engine
        var engine = new GrammarEngine();
        var symbol = new Symbol(buildingType);
        symbol.SetSpatialParameter("path", grammarPath);
        symbol.SetSpatialParameter("heightProfile", grammarHeight);
        symbol.SetSpatialParameter("windows", grammarWindows);
        
        var tree = engine.Generate(symbol);
        // Use tree to generate mesh...
    }
}
```

### Phase 3: Generate Geometry

The grammar system generates a derivation tree with spatial parameters, which you then use to create actual geometry in Unity.

## Benefits

1. **Unity Independence**: Backend has zero Unity dependencies
2. **Testability**: Can test spatial logic without Unity
3. **Flexibility**: Easy to add new spatial types
4. **Performance**: Efficient spatial queries
5. **Serialization**: All spatial data can be serialized
6. **Editor Preview**: Unity tools for visual editing
7. **Runtime Generation**: Convert once, generate many times

## Future Extensions

- **SDF (Signed Distance Field)**: For complex shape constraints
- **VectorField**: For flow-based generation (wind, growth)
- **Graph**: For network-based structures (roads, pipes)
- **Volume**: For 3D density fields
- **AnimationCurve**: For time-based variations
