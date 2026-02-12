# Skill Tree System - Setup and Architecture Guide

## Overview

The Skill Tree system allows players to unlock and activate abilities in a visual tree structure. The system follows MVVM architecture with clear separation between data, UI, and business logic.

## Architecture

### Core Components

1. **SkillTreeNode (Serializable Class)**
   - Represents a single node in the tree
   - Stores: ID, name, description, icon, video, cost, parents, attacks, UI position
   - **NOT a ScriptableObject** - nodes are stored within the PlayerSkillTree ScriptableObject

2. **PlayerSkillTree (ScriptableObject)**
   - Defines the entire skill tree structure
   - Contains all nodes and their relationships
   - Edited via custom inspector and Skill Tree Editor window

3. **PlayerSkillTreeData (Serializable Runtime Data)**
   - Tracks player's progress through the skill tree
   - Separates **unlocked** nodes (permanently available) from **activated** nodes (currently equipped)
   - Managed by RuntimePlayerStatus

4. **SkillTreeDataProvider (MonoBehaviour)**
   - Interfaces between UI and game data
   - Provides node information, unlock/activation methods
   - Attached to SkillTreeTab GameObject

5. **SkillTreeTab (UI Component)**
   - Main UI controller for the skill tree tab
   - Handles navigation, focus, animations
   - Shows node info and video demonstrations

6. **SkillTreeNodeUI (Visual Component)**
   - Visual representation of a single node
   - Updates colors/icons based on lock/unlock/activation state

### Design Patterns Used

- **MVVM**: UI (SkillTreeTab) ↔ ViewModel (SkillTreeDataProvider) ↔ Model (PlayerSkillTreeData)
- **Observer**: Nodes update visuals when state changes
- **Factory**: NodeUI prefabs instantiated for each node
- **Strategy**: Navigation uses direction-based node selection

## Setup Instructions

### 1. Create PlayerSkillTree Asset

1. In Project window: `Right-click → Create → Inventory → SkillTree`
2. Name it (e.g., "MainSkillTree")
3. Select the asset
4. Click "Open Skill Tree Editor" button in inspector

### 2. Design Skill Tree Layout (Editor Window)

1. **Tools → Skill Tree Editor**
2. Drag your PlayerSkillTree asset into the toolbar field
3. **Adding Nodes:**
   - Click "Add Node" button
   - Drag nodes to position them in the canvas (0-1 relative space)
   - **Drag only happens when clicking ON the node visual** - clicking in the inspector won't move the node
4. **Grid Snapping:**
   - Enable "Snap" toggle in toolbar
   - Set X and Y snap values (e.g., 0.05 for 5% grid)
   - Nodes will snap to grid when dragging
5. **Connecting Nodes:**
   - Right-click parent node → "Connect From This Node"
   - Right-click child node → "Connect To This Node"
   - Yellow line appears while connecting
   - **Navigation only works between connected nodes** (one-step movement)
6. **Setting Start Node:**
   - Right-click desired start node → "Set as Start Node"
7. **Editing Node Properties:**
   - Select a node (left-click)
   - Use inspector panel on right to edit:
     - Node ID (unique identifier)
     - Name and Description
     - Icon and Video
     - Cost
     - Parent connections (can also remove from here)
   - **Editing text fields won't move the node** - only dragging the node visual does
8. **Canvas Navigation:**
   - Middle mouse drag: Pan canvas
   - Scroll wheel: Zoom in/out
   - "Center View" button: Reset to origin

### 3. Configure Node Details (Inspector)

1. Select your PlayerSkillTree asset
2. Expand "Nodes" foldout
3. For each node, configure:
   - **Node ID**: Unique identifier (auto-generated GUID)
   - **Name**: Display name
   - **Description**: Shown when hovering
   - **Icon**: Sprite for the node
   - **Demo Video**: VideoClip showing what the node unlocks
   - **Cost**: Skill points needed to unlock
   - **UI Position**: Set via Skill Tree Editor (read-only here)
   - **Parent Node IDs**: Prerequisites for this node
   - **Unlocked Attacks**: Attack[] assigned when node is activated
   - **Displayed Attack Index**: Which attack to show in video

### 4. Setup RuntimePlayerStatus

Your `RuntimePlayerStatus` should have:

```csharp
public PlayerSkillTreeData skillTreeData;
```

Ensure `skillTreeData` is initialized with:
- `skillTree` = your PlayerSkillTree asset
- `availableSkillPoints` = starting amount (e.g., 10)
- `unlockedNodeIds` = empty list (or pre-unlocked nodes)
- `activatedNodeIds` = empty list

### 5. Setup UI Hierarchy

Create the following hierarchy in your Overworld Menu:

```
SkillTreeTab (GameObject)
├── SkillTreeDataProvider (Component: SkillTreeDataProvider)
├── TreeView (RectTransform - moves to keep focus centered)
│   └── NodeContainer (RectTransform - holds all nodes)
│       └── (Node prefabs instantiated here at runtime)
├── InfoPanel (Bottom area)
│   ├── NodeIcon (Image)
│   ├── NodeNameText (TextMeshProUGUI)
│   ├── NodeDescriptionText (TextMeshProUGUI)
│   ├── NodeCostText (TextMeshProUGUI)
│   └── SkillPointsText (TextMeshProUGUI)
├── VideoPlayerOverlay (GameObject)
│   └── VideoPlayer (Video Player component)
└── CharacterModelDisplay (RenderTextureDisplay - background)
```

### 6. Create Node Prefab

1. Create GameObject: "SkillTreeNode"
2. Add components:
   - **SkillTreeNodeUI** script
   - **RectTransform** (UI element)
   - **CanvasGroup** (for fading)
3. Create children:
   - **Background** (Image) - main node visual
   - **Icon** (Image) - node icon
   - **Border** (Image) - highlight when activated
   - **NameText** (TextMeshProUGUI) - node name
   - **HoldProgressRing** (Image) - radial fill for hold actions
     - Set Image Type: Filled
     - Set Fill Method: Radial 360
     - Set Fill Origin: Top
     - Start with fillAmount = 0
     - Initially disabled
4. Assign references in SkillTreeNodeUI inspector:
   - Node Background → Background Image
   - Node Icon → Icon Image
   - Node Border → Border Image
   - Node Name Text → NameText
   - Hold Progress Ring → HoldProgressRing Image
5. Configure colors:
   - Locked Color: Gray (0.3, 0.3, 0.3)
   - Unlocked Color: Blue (0.5, 0.7, 1.0)
   - Activated Color: Green (0.2, 1.0, 0.3)
   - Can Unlock Color: Yellow (1.0, 0.8, 0.3)
   - Progress Ring Color: White (1.0, 1.0, 1.0)
6. Save as prefab in Assets/Prefabs/UI/

### 7. Configure SkillTreeTab Component

1. Select SkillTreeTab GameObject
2. Assign references:
   - **Skill Tree Data Provider**: SkillTreeDataProvider component
   - **Tree Container**: TreeView RectTransform
   - **Node Container**: NodeContainer RectTransform
   - **Node UI Prefab**: Your SkillTreeNode prefab
   - **Node Icon/Name/Description/Cost Text**: InfoPanel children
   - **Skill Points Text**: Display for available points
   - **Video Player**: VideoPlayer component
   - **Video Player Overlay**: GameObject containing video player
   - **Character Model Display**: RenderTextureDisplay (background)
3. Configure animation settings:
   - **Focus Animation Duration**: 0.3s
   - **Focus Animation Ease**: OutCubic
   - **Node Scale Normal**: 1.0
   - **Node Scale Focused**: 1.2

### 8. Configure SkillTreeDataProvider

1. Select SkillTreeDataProvider component
2. Assign **Player Controller**: Drag your PlayerController GameObject

### 9. Setup NodeContainer Size

The NodeContainer should be large enough to hold all nodes:
- Recommended size: 2000x2000 (adjust based on tree size)
- Anchor: Center
- Pivot: (0.5, 0.5)

## Usage

### Player Workflow

1. **Opening Skill Tree Tab**:
   - Player navigates to Tab 4
   - Starts at the start node (centered and focused)

2. **Navigation**:
   - Use D-pad/Left stick to navigate between nodes
   - **One input = one node movement** (only to connected nodes)
   - System finds nearest **connected** node (parent or child) in that direction
   - View smoothly animates to center the new node
   - Won't skip over intermediate nodes

3. **Unlocking Nodes**:
   - Navigate to a locked node with "can unlock" status (yellow)
   - Hold A button for 0.5 seconds
   - **Radial progress ring appears on the node itself** showing hold progress
   - Spends skill points, node becomes unlocked (blue)
   - Node's attacks are now available

4. **Activating/Deactivating Nodes**:
   - Navigate to an unlocked node
   - Node is automatically activated (green) when unlocked
   - Hold B button for 0.5 seconds to deactivate (toggle off)
   - **Radial progress ring appears on the node itself** showing hold progress
   - Can reactivate anytime by holding A button (no cost)

5. **Viewing Node Info**:
   - Bottom panel shows:
     - Icon
     - Name
     - Description
     - Cost (or "UNLOCKED"/"ACTIVATED" status)
   - Video demonstration plays if available

### Developer Workflow

1. **Adding New Nodes**:
   - Open Skill Tree Editor
   - Click "Add Node"
   - Position node
   - Connect to parents
   - Edit properties in inspector

2. **Modifying Node Properties**:
   - Select PlayerSkillTree asset
   - Edit node in inspector
   - Or use Skill Tree Editor for visual editing

3. **Testing**:
   - Set initial skill points in RuntimePlayerStatus
   - Play game and navigate to Skill Tree tab
   - Verify navigation, unlock, and activation work correctly

## Key Design Decisions

### Why Serializable Class Instead of ScriptableObject?

**SkillTreeNode is a serializable class** (not ScriptableObject) because:
- Nodes are tightly coupled to a specific tree
- All editing happens in one place (PlayerSkillTree)
- No need for separate asset files cluttering the project
- Easier to duplicate/modify entire trees

### Unlocked vs Activated

**Two-state system**:
- **Unlocked**: Player paid the cost, node is permanently available
- **Activated**: Node is currently equipped and providing benefits

This allows players to experiment with different builds without re-paying costs.

### UI Position Storage

Nodes store their **UI position** (0-1 relative space) directly:
- Makes layout data persistent
- Easy to translate to screen coordinates
- Skill Tree Editor sets these positions visually

### Direction-Based Navigation

Instead of free-form navigation to any node:
- System only considers **connected nodes** (parents and children)
- Finds nearest connected node in given direction
- **One input = one node movement**
- More intuitive for controller input
- Prevents skipping over intermediate nodes
- Reduces setup complexity

## Troubleshooting

### Nodes Not Appearing

- Check NodeContainer size (should be large enough)
- Verify Node Prefab is assigned
- Check node positions are within 0-1 range

### Navigation Not Working

- Ensure Input Manager is properly configured
- Check that nodes are close enough to detect (increase NodeContainer size)
- Verify direction threshold (0.5 alignment minimum)

### Can't Unlock Node

- Check available skill points
- Verify parent nodes are unlocked
- Check node requirements in PlayerSkillTree
- **Hold A button for full 0.5 seconds** - watch the progress ring

### Hold Actions Not Working

- Verify Input Manager is properly configured
- Check that Select and Back actions are mapped
- Ensure holdProgressRing is assigned in each node's SkillTreeNodeUI component
- Make sure node is in correct state (locked but can unlock, or activated)
- Watch for the radial progress ring on the node itself

### Video Not Playing

- Ensure VideoClip is assigned to node
- Check VideoPlayer component is on overlay
- Verify videoPlayerOverlay GameObject is assigned

## Extension Points

### Adding Custom Node Types

1. Add fields to `SkillTreeNode` class
2. Update `SkillTreeDataProvider.ConvertToDisplayData()`
3. Add UI elements in `SkillTreeNodeUI`

### Custom Navigation Logic

Override `GetNearestNodeInDirection()` in `PlayerSkillTree`

### Visual Enhancements

- Add particle effects in `SkillTreeNodeUI`
- Implement connection line drawing in `SkillTreeTab.DrawConnections()`
- Add hold progress ring around focused node

### Saving/Loading

`PlayerSkillTreeData` is serializable and stored in `RuntimePlayerStatus`, which should handle save/load automatically via your existing save system.

## Summary

This skill tree system is:
- **Modular**: Each component has clear responsibilities
- **Data-Driven**: Easy to create and modify skill trees
- **User-Friendly**: Visual editor for layout
- **Extensible**: Easy to add new features
- **Controller-First**: Designed for gamepad navigation

All node data is centralized in the PlayerSkillTree asset, editable via custom inspector and visual editor, with runtime progress tracked separately in PlayerSkillTreeData.

