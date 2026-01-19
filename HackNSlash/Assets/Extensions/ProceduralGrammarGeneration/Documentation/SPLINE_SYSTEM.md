# Spline-Based Building Generation

## Overview

The **SplineFollowStrategy** enables procedural generation of buildings that follow curved paths. Combined with **SplineBuilding.pgr** grammar, you can create structures that wrap around splines while maintaining the same floor/window/door/roof structure as rectangular buildings.

## Components

### 1. SplineFollowStrategy.cs
A spatial strategy that positions grammar symbols along a Bezier spline path.

**Key Features:**
- Follows curved paths defined by control points
- Supports vertical stacking (floors)
- Orthogonal offset for wall placement
- Grid mode for roof tiling along curves
- Face-based placement (4 sides around spline)

**Configuration:**
- `splinePath`: BezierSpline with control points
- `structureHeight`: Total vertical height
- `orthogonalOffset`: Perpendicular offset from spline
- `defaultLayerHeight`: Height per floor
- `ConfigManager`: Symbol behavior configuration

### 2. SplineBuilding.pgr
Grammar definition for spline-based buildings with same structure as ComplexBuilding.

**Parameters:**
- `splineLength`: Length along spline path
- `width`: Perpendicular width
- `height`: Vertical height
- `windowSize`, `doorWidth`, `wallWidth`: Element dimensions
- `roofTileSize`: Roof tile size
- `floorHeight`: Height per floor

### 3. BezierSpline Class
Simple Bezier spline implementation for curved paths.

**Features:**
- Cubic Bezier interpolation
- Control point array
- Point/tangent evaluation at parameter t [0, 1]
- Optional loop mode

## Usage

### Setup in ProceduralGenerator

1. **Select Strategy:**
   - Strategy Type: `Spline Follow`

2. **Configure Spline:**
   ```
   Spline Path:
     Control Points: Array[4+]
       [0] (0, 0, 0)     // Start
       [1] (10, 0, 10)   // Control 1
       [2] (20, 0, 10)   // Control 2
       [3] (30, 0, 0)    // End
   ```

3. **Set Parameters:**
   - Structure Height: 14.0
   - Orthogonal Offset: 0.0 (centered on spline)
   - Default Layer Height: 3.5

4. **Assign Grammar:**
   - Grammar Asset: SplineBuilding

5. **Configure Parameters:**
   - splineLength: 40.0 (approximate path length)
   - width: 30.0 (perpendicular width)
   - height: 14.0 (matches structure height)
   - windowSize: 2.0
   - doorWidth: 1.5
   - wallWidth: 2.0
   - roofTileSize: 2.0
   - floorHeight: 3.5

### Creating Custom Splines

**In Code:**
```csharp
var spline = new BezierSpline
{
    ControlPoints = new Vector3[]
    {
        new Vector3(0, 0, 0),
        new Vector3(10, 0, 15),
        new Vector3(30, 0, 15),
        new Vector3(40, 0, 0)
    },
    Loop = false
};

var strategy = new SplineFollowStrategy(spline, 14f, 0f, 3.5f);
generator.spatialStrategy = strategy;
```

**Via Inspector:**
```
SplineFollowStrategy:
  Spline Path:
    Control Points: Size [4]
      Element 0: (0, 0, 0)
      Element 1: (10, 0, 15)
      Element 2: (30, 0, 15)
      Element 3: (40, 0, 0)
    Loop: false
  Structure Height: 14
  Orthogonal Offset: 0
  Default Layer Height: 3.5
```

## How It Works

### Spline Traversal
1. **Parameter t**: Buildings traverse spline from t=0 to t=1
2. **Element Placement**: Each window/door advances along spline
3. **Face Calculation**: 4 sides positioned relative to spline tangent
4. **Grid Mode**: Roof tiles follow spline curvature

### Coordinate System
- **Front (Face 0)**: Follows spline tangent
- **Right (Face 1)**: Perpendicular to tangent (cross with up)
- **Back (Face 2)**: Opposite of tangent
- **Left (Face 3)**: -Perpendicular to tangent

### Orthogonal Offset
- **Positive**: Shifts building right (perpendicular to tangent)
- **Negative**: Shifts building left
- **Zero**: Centered on spline path

### Roof Tiling
- Grid mode activates for roof section
- Tiles follow spline curvature
- Rows advance perpendicular to spline
- Maintains regular tiling pattern

## Examples

### Straight Building (Same as VerticalStack)
```csharp
var spline = new BezierSpline
{
    ControlPoints = new Vector3[]
    {
        new Vector3(0, 0, 0),
        new Vector3(40, 0, 0)
    }
};
// Result: Linear building, identical to VerticalStackStrategy
```

### Curved Building
```csharp
var spline = new BezierSpline
{
    ControlPoints = new Vector3[]
    {
        new Vector3(0, 0, 0),
        new Vector3(15, 0, 20),
        new Vector3(35, 0, 20),
        new Vector3(50, 0, 0)
    }
};
// Result: Building curves smoothly following path
```

### S-Curve Building
```csharp
var spline = new BezierSpline
{
    ControlPoints = new Vector3[]
    {
        new Vector3(0, 0, 0),
        new Vector3(10, 0, 15),
        new Vector3(20, 0, -15),
        new Vector3(30, 0, 0)
    }
};
// Result: Building with S-shaped path
```

### Offset Building (Parallel Path)
```csharp
var strategy = new SplineFollowStrategy(spline, 14f, 5f, 3.5f);
// Result: Building offset 5 units to the right of spline
```

## Comparison: Vertical Stack vs Spline Follow

| Feature | VerticalStack | SplineFollow |
|---------|---------------|--------------|
| **Path** | Straight rectangular | Curved spline |
| **Floors** | Stacked vertically | Stacked vertically |
| **Front/Back** | Parallel straight | Follow spline curve |
| **Sides** | Perpendicular | Perpendicular to tangent |
| **Roof** | 2D grid (X, Z) | Follows spline curvature |
| **Parameters** | length, width | splineLength, width |
| **Use Case** | Regular buildings | Curved structures, walls |

## Design Philosophy

### Modularity
The system is **completely modular**:
1. **SplineFollowStrategy** - Drop-in spatial strategy (no core changes)
2. **SplineBuilding.pgr** - Grammar using same symbols as ComplexBuilding
3. **No modifications needed** to existing code

### Grammar Reuse
SplineBuilding uses identical symbol names to ComplexBuilding:
- Same terminal symbols: Window, Door, Wall, RoofTile
- Same behaviors: FloorMarker, FloorSide, ElementRow, etc.
- Same geometry assets work for both

### Extensibility
To create new curved structures:
1. Write new .pgr grammar (reuse symbols or create new ones)
2. Use SplineFollowStrategy as-is
3. Configure spline and parameters
4. Generate!

## Advanced Usage

### Dynamic Splines
```csharp
// Create spline from scene objects
var spline = new BezierSpline
{
    ControlPoints = waypoints.Select(w => w.position).ToArray()
};

// Update strategy
var strategy = generator.spatialStrategy as SplineFollowStrategy;
strategy.splinePath = spline;
generator.Generate();
```

### Procedural Walls
```csharp
// Create defensive wall following terrain
var spline = CreateSplineFromTerrainContour(heightThreshold);
var strategy = new SplineFollowStrategy(spline, 8f, 0f, 4f);

// Use different grammar
generator.grammarAsset = wallGrammar;
generator.Generate();
```

### Multi-Segment Buildings
```csharp
// Generate multiple buildings along same spline
for (int i = 0; i < segments; i++)
{
    var segmentSpline = ExtractSplineSegment(mainSpline, i, segmentLength);
    var strategy = new SplineFollowStrategy(segmentSpline, 14f, 0f, 3.5f);
    generator.spatialStrategy = strategy;
    generator.Generate();
}
```

## Troubleshooting

### Building doesn't follow curve
- **Issue**: Control points too few
- **Fix**: Use at least 4 control points for smooth curves

### Roof tiles don't align
- **Issue**: splineLength parameter doesn't match actual spline length
- **Fix**: Adjust splineLength to approximate path length

### Elements stretched/compressed
- **Issue**: elementSize doesn't match spline traversal
- **Fix**: Ensure doorWidth + wallWidth divides evenly into splineLength

### Building offset wrong direction
- **Issue**: Orthogonal offset sign
- **Fix**: Positive = right, negative = left (relative to spline direction)

## Next Steps

1. ✅ Test with SplineBuilding.pgr grammar
2. ✅ Experiment with different spline shapes
3. → Create custom grammars for other curved structures
4. → Add spline editing tools in Unity scene view
5. → Create spline presets (circle, spiral, etc.)

---

**The spline system is fully modular and requires zero changes to existing code!**
