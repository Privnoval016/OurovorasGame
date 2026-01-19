# Parameter Contracts Architecture

## Overview

Parameter contracts define a standardized interface between the abstract grammar system and concrete spatial placement strategies. They solve the scalability problem of having strategies work with arbitrary parameter sets by establishing **validation contracts** that both grammars and strategies must respect.

## The Problem We're Solving

**Without Contracts:**
- Strategies have to manually check for parameters: "does this node have 'width'? 'w'? 'length'?"
- Edge case detection everywhere: "if spline-based, check X; if LWH-based, check Y"
- Hard to add new structure types without modifying existing strategies
- No way to validate grammar output before placement

**With Contracts:**
- Strategies declare what they need: `RequiredContract = new LWHParameterContract()`
- Contracts validate upfront: `contract.ValidateNode(node)`
- Type-safe parameter access: `contract.GetWidth(node)` handles variations
- Easy to add new strategies: just define a new contract

## Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│ GRAMMAR LAYER (Abstract Symbolic)                       │
│ - Defines terminal symbols with parameters              │
│ - Example: Wall(width: 2.0, height: 3.0, depth: 0.5)   │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ PARAMETER CONTRACT LAYER (Validation)                   │
│ - Defines what parameters are expected                  │
│ - Validates grammar output                              │
│ - Provides type-safe parameter access                   │
│ Example: LWHParameterContract expects width/height/depth│
└─────────────────────────────────────────────────────────┐
                        ↓
┌─────────────────────────────────────────────────────────┐
│ SPATIAL STRATEGY LAYER (Placement Logic)                │
│ - Uses contract to read grammar parameters              │
│ - Uses geometry data for mesh-specific properties       │
│ - Combines both to determine final placement            │
│ Example: VerticalStackStrategy uses LWHParameterContract│
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ GEOMETRY DATA LAYER (Mesh-Specific Properties)          │
│ - Defines mesh bounds, alignment, pivot, materials      │
│ - Tightly coupled to the mesh asset                     │
│ Example: WindowGeometry has bottom-center alignment     │
└─────────────────────────────────────────────────────────┘
```

## Separation of Responsibility

### 1. Grammar (Defines WHAT parameters exist)
```csharp
// Grammar defines terminal symbols with parameters
Terminal Wall(width: float, height: float, depth: float)
Terminal Window(width: float, height: float, depth: float)
```

The grammar is the **source of truth** for what parameters exist in the abstract structure.

### 2. Parameter Contract (Defines WHICH parameters are needed)
```csharp
public class LWHParameterContract : IParameterContract
{
    public bool ValidateNode(SpatialNode node)
    {
        return node.HasParameter("width") && 
               node.HasParameter("height") && 
               node.HasParameter("depth");
    }
    
    public float GetWidth(SpatialNode node) { /* handles "width", "w" variations */ }
    public Vector3 GetDimensions(SpatialNode node) { /* returns (w, h, d) */ }
}
```

Contracts handle **parameter name variations** and provide **type-safe accessors**.

### 3. Spatial Strategy (Defines HOW to place using parameters)
```csharp
public class VerticalStackStrategy : BaseSpatialStrategy
{
    public override IParameterContract RequiredContract => new LWHParameterContract();
    
    public override void PlaceNode(SpatialNode node, SpatialContext context)
    {
        // Validate using contract
        if (!ValidateNodeParameters(node))
            return; // Contract validation failed, logged warning
        
        // Get dimensions from grammar parameters via contract
        Vector3 dimensions = _contract.GetDimensions(node);
        
        // Set position (strategy's responsibility)
        node.Position = new Vector3(_currentXOffset, _currentFloorY, 0);
        
        // Set initial scale from grammar parameters
        node.Scale = dimensions;
        
        // Apply geometry data (respects mesh-specific properties)
        ApplyGeometryData(node, context);
    }
}
```

Strategy uses **contract for grammar parameters**, **geometry data for mesh properties**.

### 4. Geometry Data (Defines mesh-specific properties)
```csharp
// ScriptableObject configured per mesh
public class SymbolGeometryData : ScriptableObject
{
    public string symbolName = "Window";
    public GameObject prefab;
    
    // MESH-SPECIFIC PROPERTIES (tightly coupled to mesh)
    public Vector3 bounds = new Vector3(2, 3, 0.5); // Actual mesh dimensions
    public AlignmentMode alignment = AlignmentMode.BottomCenter; // How mesh should align
    public Vector3 pivotOffset = new Vector3(0.5f, 0f, 0.5f); // Where mesh pivot is
    public Vector3 rotationOffset = Vector3.zero; // Mesh-specific rotation
    public Material material; // Visual override
}
```

Geometry data is the **mesh authority** - if mesh needs specific alignment, system respects it.

## Example: Building vs Road

### Building (Uses LWHParameterContract)

**Grammar:**
```
Terminal Wall(width: float, height: float, depth: float)
Terminal Window(width: float, height: float, depth: float)

Floor(w, h) -> Wall(w, h, 0.2) | Window(2, 1.5, 0.1)
```

**Strategy:** `VerticalStackStrategy` requires `LWHParameterContract`

**Geometry:** 
- `WallGeometry.bounds = (1, 1, 1)` → scales to match grammar parameters
- `WallGeometry.alignment = BottomCenter` → walls align on bottom
- `WindowGeometry.alignment = Center` → windows align centered

**Result:** Strategy reads width/height/depth from grammar, places elements horizontally/vertically, respects each mesh's alignment preference.

---

### Road (Uses SplineParameterContract)

**Grammar:**
```
Terminal RoadSegment(splinePos: float, height: float, thickness: float)
Terminal Guardrail(splinePos: float, height: float, thickness: float)

Road(spline) -> RoadSegment(0.0, 0.1, 4.0) | Guardrail(0.0, 0.8, 0.1)
```

**Strategy:** `SplineFollowStrategy` requires `SplineParameterContract`

**Geometry:**
- `RoadSegmentGeometry.bounds = (5, 0.1, 5)` → scales to thickness
- `RoadSegmentGeometry.alignment = BottomCenter` → sits on spline
- `GuardrailGeometry.alignment = Bottom` → along spline edge

**Result:** Strategy reads splinePos/thickness from grammar, places along spline curve, respects each mesh's alignment along the path.

## How to Add a New Structure Type

1. **Define Parameter Contract**
   ```csharp
   public class MyCustomContract : IParameterContract
   {
       public bool ValidateNode(SpatialNode node) 
       { 
           return node.HasParameter("customParam");
       }
       public float GetCustomParam(SpatialNode node) { /* ... */ }
   }
   ```

2. **Create Strategy Using Contract**
   ```csharp
   public class MyCustomStrategy : BaseSpatialStrategy
   {
       public override IParameterContract RequiredContract => new MyCustomContract();
       private MyCustomContract _contract = new MyCustomContract();
       
       public override void PlaceNode(SpatialNode node, SpatialContext context)
       {
           if (!ValidateNodeParameters(node)) return;
           
           float param = _contract.GetCustomParam(node);
           // Use param for placement...
           ApplyGeometryData(node, context); // Respect mesh properties
       }
   }
   ```

3. **Define Grammar with Required Parameters**
   ```
   Terminal MyElement(customParam: float)
   ```

4. **Create Geometry Data for Mesh**
   - Right-click → Create → Procedural Grammar → Symbol Geometry Data
   - Set symbolName = "MyElement"
   - Set prefab, bounds, alignment (mesh-specific properties)

**Done!** No edge cases, no hardcoding, fully scalable.

## Benefits

| Benefit | How It's Achieved |
|---------|-------------------|
| **No Edge Cases** | Contracts validate upfront, strategies only run on valid nodes |
| **Type Safety** | Contract methods provide typed accessors (GetWidth(), GetSplinePosition()) |
| **Parameter Variations** | Contracts handle "width" vs "w" vs "length" internally |
| **Clear Expectations** | Strategy declares `RequiredContract`, users know what grammar needs |
| **Mesh Authority** | Geometry data defines mesh-specific properties, system respects them |
| **Easy Extensibility** | New structure type = new contract + new strategy, no existing code changes |
| **Validation Errors** | Clear messages: "Missing: width/height. Expected: LWH contract" |

## Built-in Contracts

### LWHParameterContract
- **Purpose:** Length/Width/Height based placement (buildings, boxes, walls)
- **Parameters:** width/w, height/h, depth/d/length/l (flexible names)
- **Usage:** `VerticalStackStrategy`, grid layouts, box placement
- **Methods:** `GetWidth()`, `GetHeight()`, `GetDepth()`, `GetDimensions()`

### SplineParameterContract
- **Purpose:** Spline-curve based placement (roads, paths, rivers)
- **Parameters:** splinePos/t (0-1), height/h, thickness/width/w
- **Usage:** `SplineFollowStrategy`, curve-based generation
- **Methods:** `GetSplinePosition()`, `GetHeight()`, `GetThickness()`, `GetNormalizedSplinePosition()`

## Future Contracts

Here are examples of contracts you might add:

```csharp
// For trees/vegetation with branching angles
public class BranchParameterContract : IParameterContract
{
    // Parameters: branchAngle, branchLength, branchThickness
}

// For grid-based tile systems
public class GridParameterContract : IParameterContract
{
    // Parameters: gridX, gridY, tileType
}

// For radial structures (circular buildings, arenas)
public class RadialParameterContract : IParameterContract
{
    // Parameters: angle, radius, height
}
```

Each contract defines a **different way of thinking about space**, enabling completely different structure types without modifying existing code.
