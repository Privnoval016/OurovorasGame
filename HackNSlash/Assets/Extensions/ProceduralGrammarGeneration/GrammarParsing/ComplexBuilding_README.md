# ComplexBuilding Grammar - 3D Building Generator

## Overview
Generates a complete 3D building with:
- **4 lateral faces** (North, East, South, West)
- **Ground floor**: 1 door + 2 walls per side (4 doors total)
- **Upper floors**: Alternating windows and walls per side
- **Roof**: Tiled roof structure

## Axiom Parameters
```
Building(length: float, width: float, height: float, floors: int)
```

Example: `Building(length=50, width=40, height=100, floors=4)`

## Grammar Structure

### 1. Building → Structure + Roof
```
Building(50, 40, 100, 4)
  ├─ Structure(50, 40, 100, 4)  [vertical floors]
  └─ Roof(50, 40)                [horizontal tiles]
```

### 2. Structure → Floors (Recursive)
```
Structure(50, 40, 100, 4)
  ├─ Floor(50, 40, 25, 3) + FloorMarker(3)
  ├─ Floor(50, 40, 25, 2) + FloorMarker(2)
  ├─ Floor(50, 40, 25, 1) + FloorMarker(1)
  └─ Floor(50, 40, 25, 0) + FloorMarker(0)
```

### 3. Floor → 4 Sides
```
Floor(50, 40, 25, floorNum)
  ├─ FloorSide(50, 40, 25, floorNum, 0)  [North: length]
  ├─ FloorSide(40, 50, 25, floorNum, 1)  [East: width]
  ├─ FloorSide(50, 40, 25, floorNum, 2)  [South: length]
  └─ FloorSide(40, 50, 25, floorNum, 3)  [West: width]
```

Note: Sides alternate between `length` and `width` to form a closed rectangle.

### 4. Floor Side → Elements

**Ground Floor (floorNum == 0):**
```
FloorSide(50, 40, 25, 0, sideNum)
  ├─ Wall(16.67, 25, 0.2)
  ├─ Door(16.67, 22.5, 0.3)
  └─ Wall(16.67, 25, 0.2)
```

**Upper Floors (floorNum > 0):**
```
FloorSide(50, 40, 25, floorNum, sideNum)
  └─ WallStrip → Alternating Pattern:
       Window(10, 17.5, 0.15)
       Wall(10, 25, 0.2)
       Window(10, 17.5, 0.15)
       Wall(10, 25, 0.2)
       Window(10, 17.5, 0.15)
```

### 5. Roof → Tiles
```
Roof(50, 40)
  └─ RoofStrip → Tiles:
       RoofTile(10, 0.3, 8) × 25 tiles
       (5 tiles wide × 5 tiles deep)
```

## Terminal Symbols

All terminal symbols use **width, height, depth** parameters (compatible with LWHParameterContract):

| Symbol | Parameters | Description |
|--------|------------|-------------|
| `Window` | width, height, depth | Glass window opening (depth=0.15m) |
| `Door` | width, height, depth | Entrance door (depth=0.3m, height=90% of floor) |
| `Wall` | width, height, depth | Solid wall segment (depth=0.2m) |
| `RoofTile` | width, height, depth | Individual roof tile (height=0.3m) |
| `FloorMarker` | floorNum | Marks floor level for VerticalStackStrategy |

## Example Generation

### Input
```csharp
// In SpatialTester:
Axiom: Building
Parameters:
  - length: 50
  - width: 40
  - height: 100
  - floors: 4
```

### Output Structure
```
Floor 3 (top):
  North: W-W-W-W-W (5 windows alternating with walls)
  East:  W-W-W-W-W
  South: W-W-W-W-W
  West:  W-W-W-W-W
  
Floor 2:
  North: W-W-W-W-W
  East:  W-W-W-W-W
  South: W-W-W-W-W
  West:  W-W-W-W-W
  
Floor 1:
  North: W-W-W-W-W
  East:  W-W-W-W-W
  South: W-W-W-W-W
  West:  W-W-W-W-W

Floor 0 (ground):
  North: Wall-Door-Wall (3 elements)
  East:  Wall-Door-Wall
  South: Wall-Door-Wall
  West:  Wall-Door-Wall (4 doors total, one per side)

Roof:
  5x5 grid of RoofTiles (25 tiles total)
```

## Spatial Interpretation

The grammar outputs **abstract parameters** (width, height, depth). The **VerticalStackStrategy** interprets these into **3D positions**:

1. **FloorMarker** sets vertical position: `Y = floorNum * heightPerFloor`
2. **Elements placed horizontally** at current floor level
3. **Sides are placed sequentially** (strategy doesn't rotate - just advances horizontally)

**Note:** For true 3D closed building, you'd need a more advanced strategy that:
- Rotates each side by 90° around building center
- Positions sides at correct X/Z offsets to form a rectangle
- Places roof above top floor

Current VerticalStackStrategy places everything in a line - suitable for testing parameter flow and terminal symbol generation.

## Testing

To test with SpatialTester:
1. Set `grammarAsset` to ComplexBuilding
2. Set axiom parameters:
   - `length` = 50 (float)
   - `width` = 40 (float)
   - `height` = 100 (float)
   - `floors` = 4 (int)
3. Assign `geometryLibrary` with Window, Door, Wall, RoofTile, FloorMarker geometry data
4. Run "Test Spatial Generation"
5. Check `spatial_output.txt` for terminal symbol positions

Expected output: ~100-150 terminal symbols with proper width/height/depth parameters.
