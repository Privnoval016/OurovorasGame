# ProceduralGenerator - Inspector UI Guide

## Visual Layout

```
┌─────────────────────────────────────────────────────┐
│     Procedural Grammar Generator                    │
│  Generate procedural 3D structures from grammar     │
│                  rules...                           │
└─────────────────────────────────────────────────────┘

▼ Grammar Configuration ═══════════════════════════════
  ┌─────────────────────────────────────────────────┐
  │ Grammar Asset:        [ComplexBuilding]          │
  │ Axiom Symbol:          Building                  │
  │ Symbols:               13                        │
  │ Rules:                 14                        │
  │ Max Iterations:        [100]                     │
  └─────────────────────────────────────────────────┘

▼ Spatial Strategy ════════════════════════════════════
  ┌─────────────────────────────────────────────────┐
  │ Strategy Type:        [Vertical Stack      ▼]   │
  │                                                  │
  │ Strategy Settings:                               │
  │   ▼ Strategy Configuration                       │
  │     Default Layer Height:  [3.5]                 │
  │     ▼ Config Manager                             │
  │       Configs: Array[10]                         │
  │       [0] FloorMarker: ResetVertical             │
  │       [1] FloorSide:   ChangeSide                │
  │       [2] Roof:        StartSection              │
  │       ...                                        │
  └─────────────────────────────────────────────────┘

▼ Geometry Library ════════════════════════════════════
  ┌─────────────────────────────────────────────────┐
  │ Geometry Assets: Array[4]                        │
  │   [0] WindowGeometry                             │
  │   [1] DoorGeometry                               │
  │   [2] WallGeometry                               │
  │   [3] RoofTileGeometry                           │
  │                                                  │
  │ Total Assets: 4                                  │
  └─────────────────────────────────────────────────┘

▼ Axiom Parameters ════════════════════════════════════
  ┌─────────────────────────────────────────────────┐
  │ ┌─────────────────────────────────────────────┐ │
  │ │ length:        [40.0]                        │ │
  │ └─────────────────────────────────────────────┘ │
  │ ┌─────────────────────────────────────────────┐ │
  │ │ width:         [30.0]                        │ │
  │ └─────────────────────────────────────────────┘ │
  │ ┌─────────────────────────────────────────────┐ │
  │ │ height:        [14.0]                        │ │
  │ └─────────────────────────────────────────────┘ │
  │ ┌─────────────────────────────────────────────┐ │
  │ │ windowSize:    [2.0]                         │ │
  │ └─────────────────────────────────────────────┘ │
  │ ┌─────────────────────────────────────────────┐ │
  │ │ doorWidth:     [1.5]                         │ │
  │ └─────────────────────────────────────────────┘ │
  │ ...                                              │
  └─────────────────────────────────────────────────┘

▶ Advanced Options ════════════════════════════════════

┌─────────────────────────────────────────────────────┐
│              Generation Controls                     │
│                                                      │
│  ┌───────────────────────────────────────────────┐  │
│  │                                                │  │
│  │              GENERATE                          │  │
│  │                                                │  │
│  └───────────────────────────────────────────────┘  │
│                    (Green Button)                    │
│                                                      │
│  ┌───────────────────────────────────────────────┐  │
│  │     Clear Generated Content                    │  │
│  └───────────────────────────────────────────────┘  │
│                    (Red Button)                      │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ ℹ️ Status: Complete! Generated 594 objects          │
└─────────────────────────────────────────────────────┘

▼ Generation Statistics ═══════════════════════════════
  ┌─────────────────────────────────────────────────┐
  │ Grammar:              ComplexBuilding            │
  │ Symbols / Rules:      13 / 14                    │
  │ Derivation Depth:     37                         │
  │ Terminal Nodes:       594                        │
  │ Spatial Nodes:        1187                       │
  │ Generated Objects:    594                        │
  │ Bounds Center:        (39.00, 7.00, 15.00)       │
  │ Bounds Size:          (79.00, 15.00, 31.00)      │
  └─────────────────────────────────────────────────┘
```

## Color Coding

| Element | Color | Purpose |
|---------|-------|---------|
| **GENERATE button** | 🟢 Green | Primary action - safe to use |
| **Clear button** | 🔴 Red | Destructive action - requires confirmation |
| **Info messages** | 🔵 Blue | General information |
| **Warning messages** | 🟡 Yellow | Non-critical issues |
| **Error messages** | 🔴 Red | Critical problems requiring attention |

## Section States

### Collapsed (▶)
- Section header visible
- Content hidden
- Click to expand

### Expanded (▼)
- Section header visible
- Full content shown
- Click to collapse

## Interactive Elements

### Fields
| Type | Appearance | Usage |
|------|------------|-------|
| **Float** | `[40.0]` | Drag or type to edit |
| **Int** | `[100]` | Drag or type to edit |
| **String** | `[text]` | Type to edit |
| **Bool** | `☑` / `☐` | Click to toggle |
| **Vector3** | `(X, Y, Z)` | Expand to edit components |
| **Color** | 🎨 | Click for color picker |
| **Object** | Drag-drop area | Drag asset from Project |
| **Array** | `Size [4]` | Expand to see elements |

### Buttons
- **GENERATE**: Large (40px height), green background
- **Clear**: Medium (30px height), red background  
- Both disabled when appropriate (e.g., Clear when nothing generated)

### Help Boxes
```
┌─────────────────────────────────────────────────────┐
│ ℹ️ Configure your grammar, spatial strategy, and    │
│   geometry assets, then click Generate.             │
└─────────────────────────────────────────────────────┘
```

Types:
- ℹ️ **Info**: Guidance and instructions
- ⚠️ **Warning**: Missing configuration or non-critical issues
- ❌ **Error**: Problems that prevent generation

## Workflow

1. **Assign Grammar** → Axiom parameters auto-populate
2. **Select Strategy** → Strategy settings appear
3. **Add Geometry** → Visual assets configured
4. **Set Parameters** → Initial values for generation
5. **Click GENERATE** → Status updates in real-time
6. **View Stats** → Expand statistics section
7. **Modify & Regenerate** → Iterate on design
8. **Clear** → Remove when done

## Tooltips

Hover over any field for tooltip:
- **Grammar Asset**: "Grammar asset defining generation rules"
- **Max Iterations**: "Maximum number of grammar rule expansions"
- **Strategy Configuration**: "Expand to see strategy-specific parameters"
- **Include Non-Terminals**: "Include non-terminal nodes in hierarchy (for debugging)"
- etc.

## Smart Features

### Auto-Detection
- Axiom symbol name (read-only)
- Symbol and rule counts (read-only)
- Parameter types and names (auto-populated)

### Auto-Update
- Parameter list refreshes when grammar changes
- Preserves existing parameter values when possible
- Strategy settings update on type change

### Validation
- "No grammar asset assigned" warning
- "Add geometry assets" info message
- "No parameters required" info when axiom has no params

## Keyboard Navigation

- **Tab**: Move between fields
- **Enter**: Confirm text input
- **Space**: Toggle checkboxes
- **Arrow Keys**: Adjust numeric values (when focused)

## Best Practices

### Organization
- Keep commonly-used sections expanded
- Collapse Advanced Options when not needed
- Expand Statistics after generation to review

### Workflow
1. Configure once → Save scene
2. Adjust parameters → Generate multiple times
3. Use Clear sparingly (enable Auto Clear instead)

### Performance
- Start with low max iterations
- Add geometry gradually
- Test with simple grammars first

---

**The UI is designed for intuitive, efficient workflow from grammar to generated mesh!**
