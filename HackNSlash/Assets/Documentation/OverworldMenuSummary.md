# Overworld Menu UI - Architecture & Systems

## Overview

This document provides a comprehensive overview of the Overworld Menu UI system architecture, including all tabs, data flow, stat management, and key features.

## Architecture Principles

### MVVM Pattern
- **Model**: RuntimePlayerStatus, InventoryInfo, SkillTreeData, etc.
- **ViewModel**: DataProviders (Equipment, Inventory, Element, SkillTree, etc.)
- **View**: Tab components and UI elements

### Key Design Decisions
1. **No Global Services for DataProviders**: DataProviders are local to OverworldMenuUI
2. **EventBus for Decoupling**: Stats changes, UI events use EventBus instead of singletons
3. **Modular Components**: UI components (ScrollMenu, ItemActionMenu, etc.) work independently
4. **Controller-First**: All navigation designed for gamepad, no mouse support
5. **PrimeTween Animations**: All transitions use unscaled time for smooth menu feel

## System Components

### Core Menu System

#### OverworldMenuUI (Main Controller)
- **Location**: `Assets/Scripts/UI/Game/OverworldMenu/OverworldMenuUI.cs`
- **Responsibility**: Central hub for all menu tabs
- **Features**:
  - Tab management via TabGroup
  - DataProvider initialization and connection
  - Ensures all tabs start closed on menu open
  - Resets to Tab 1 when reopening

#### TabGroup & TabSelection
- **Location**: `Assets/Extensions/UI/TabGroup.cs`, `TabSelection.cs`
- **Responsibility**: Tab navigation and content management
- **Features**:
  - Horizontal tab swiping animations
  - Auto-selection of first element for controller
  - Pause input during tab transitions

### Tab 1: Character Stats
- Displays player stats, level, equipped items
- Render texture of player model
- Mostly informational

### Tab 2: Equipment Selection
**Features**:
- Left: Equipment slots (3 accessories, 3 passives)
- Center: Scroll menu for item selection
- Right: Player render texture
- **Equipped Indicator**: Shows when item is equipped elsewhere
- **Swap Logic**: Swaps items if already equipped

**Key Files**:
- `EquipmentTab.cs`
- `EquipmentDataProvider.cs`
- `ItemSlotUI.cs` (with equipped indicator)

### Tab 3: Element Progress
**Features**:
- Left: 5 element selection buttons
- Top Center: 10 level progress indicators
- Bottom Center: 3 attack assignment buttons (X, Y, A)
- Right: Player render texture with video overlay
- **Unlock Grid**: 3x3 grid navigation for viewing level bonuses
- **Attack Swapping**: Prevents double assignment

**Navigation Layers**:
1. Element Selection
2. Attack Buttons + Unlock Grid Button
3. Unlock Grid (when entered)
4. Scroll Menu (for attack reassignment)

**Key Files**:
- `ElementProgressTab.cs`
- `ElementProgressDataProvider.cs`
- `AttackButtonSlotUI.cs`
- `UnlockSlotUI.cs`

### Tab 4: Skill Tree
**Features**:
- Visual node-based skill tree
- Direction-based navigation (connected nodes only)
- Hold A (0.5s) to unlock nodes
- Hold B (0.5s) to deactivate nodes
- **Unlocked vs Activated**: Two-state system for flexibility
- Center-focused view with smooth camera movement
- **Radial Progress Ring**: Shows hold progress on each node

**Editor Tools**:
- Visual Skill Tree Editor window
- Drag & drop node placement
- Visual connection drawing
- Custom inspector for node properties

**Key Files**:
- `SkillTreeTab.cs`
- `SkillTreeNodeUI.cs` (with hold progress ring)
- `SkillTreeDataProvider.cs`
- `SkillTreeEditorWindow.cs` (Editor)
- `PlayerSkillTreeEditor.cs` (Custom Inspector)

**Documentation**: See `SkillTreeSetup.md`

### Tab 5: Inventory
**Features**:
- Left: Category filter buttons
- Center: Scrolling item list
- Right: Player render texture
- **Item Action Menu**: Use/Discard with controller navigation
- **Equipped Indicator**: Shows when item is equipped
- **Safe Discard**: Auto-unequips before discarding
- **Sort System**: 9 sort methods (Name, Rarity, Quantity, Date, Category)

**Sort Methods**:
- Name (A-Z, Z-A)
- Rarity (High-Low, Low-High)
- Quantity (High-Low, Low-High)
- Date Obtained (Newest, Oldest) - Placeholder
- Category

**Input**:
- D-pad: Navigate/scroll
- A: Select item → Open action menu
- B: Close action menu
- **Sort Button**: Cycle through sort methods
- Sort display shows current method

**Key Files**:
- `InventoryTab.cs`
- `InventoryDataProvider.cs`
- `ItemActionMenu.cs` (modular component)
- `InventorySortMethod.cs`

**Documentation**: See `InventoryTabSetup.md`

### Tab 6: Missions
- Scroll menu of quests (main/side)
- Description on left
- Player render texture background

### Tab 7: Compendium
- Scroll menu of lore/important items
- Category selection on side

### Tab 8: Settings
- Audio/video settings
- Game saving
- Key remapping

## Data Flow Architecture

### DataProviders (Local to Menu)
All DataProviders are **MonoBehaviour components** attached to OverworldMenuUI hierarchy:
- `PlayerDataProvider`
- `EquipmentDataProvider`
- `ElementProgressDataProvider`
- `SkillTreeDataProvider`
- `InventoryDataProvider`
- `QuestDataProvider`
- `CompendiumDataProvider`

**Connection Flow**:
```
OverworldMenuUI.Awake()
  ↓
FindComponentsInChildren<DataProvider>()
  ↓
ConnectDataProvidersToTabs()
  ↓
Tab.SetDataProvider(provider)
```

**Data Access**:
```
DataProvider → RuntimePlayerStatus → Game Data
              ↓
         IDataProvider Interface
              ↓
         Tab (View)
```

## Stat Management System

### Stat Refresh in PlayerStats
**Purpose**: Automatically refreshes player stats when equipment/skills/elements change

**Location**: `Assets/Scripts/Player/Control/PlayerStats.cs`

**How It Works**:
1. Subscribes to `PlayerStatsChangedEvent` via EventBus in `OnEnable()`
2. When event raised, calls `RefreshStats()`
3. Recreates `EvaluatedStats` with fresh mediators
4. Reapplies all modifiers from:
   - Equipped accessories via `ApplyEquipmentModifiers()`
   - Element unlock levels via `ApplyElementUnlockModifiers()`
   - Activated skill tree nodes via `ApplySkillTreeModifiers()` (future)

**Event Sources**:
- Equipment changes (equip/unequip)
- Element progress level ups
- Skill tree node activation/deactivation
- Level ups

**Usage**:
```csharp
// When equipment changes
EventBus<PlayerStatsChangedEvent>.Raise(
    new PlayerStatsChangedEvent("Equipment - Equip Accessory")
);

// PlayerStats automatically refreshes via OnStatsChanged()
```

**Integration**:
- No separate MonoBehaviour component needed
- Directly integrated into PlayerStats
- Uses existing `InitializeStats()` pattern
- Reuses `Accessory.InitializeEffects()` and `ElementUnlock.Initialize()`

### PlayerStatsChangedEvent
**Location**: `Assets/Scripts/Events/PlayerStatsChangedEvent.cs`

**Fields**:
- `Source`: String describing what changed (for debugging)
- `ChangedStat`: Optional specific stat (null = refresh all)

**When to Raise**:
- Equipping/unequipping items
- Unlocking element levels
- Activating/deactivating skill nodes
- Leveling up
- Any stat modifier change

## Modular UI Components

### ScrollMenu
**Location**: `Assets/Extensions/UI/ScrollMenu.cs`
- **Purpose**: Reusable scrolling list for any data
- **Features**: Stop/Loop cycle modes, smooth animations, buffer panels
- **Usage**: Inventory items, attack selection, quest list

### ItemActionMenu
**Location**: `Assets/Extensions/UI/ItemActionMenu.cs`
- **Purpose**: Generic action menu for inventory items
- **Features**: Use/Discard options, controller navigation, events
- **Usage**: Can be used in any inventory system

### ItemSlotUI
**Location**: `Assets/Extensions/UI/ItemSlotUI.cs`
- **Purpose**: Displays equipment/item slots
- **Features**: Empty state, selection colors, equipped indicator
- **Usage**: Equipment tab, element tab

### SlotGridNavigator
**Location**: `Assets/Extensions/UI/SlotGridNavigator.cs`
- **Purpose**: Grid navigation for slot arrays
- **Features**: Automatic neighbor detection, wraparound
- **Usage**: Equipment slots, unlock grids

### RenderTextureDisplay
**Purpose**: Shared player model renderer across tabs
**Features**: Activate/deactivate per tab, shared resource

## Input Management

### Input Actions (UI Map)
- **Navigate**: D-pad/Left stick - Move selection
- **Select**: A button - Confirm selection
- **Back**: B button - Cancel/return
- **TabLeft**: LB - Previous tab
- **TabRight**: RB - Next tab
- **Sort**: Y button - Cycle sort method (Inventory)
- **Scroll**: D-pad Up/Down - Scroll menu

### Input Flow
```
InputManager (Global)
     ↓
Tab-specific input handlers
     ↓
Component actions (ScrollMenu, ItemActionMenu, etc.)
```

### No Mouse Support
- All UI designed for controller
- EventSystem uses Selectable navigation
- No IPointerEnter/IPointerExit handlers

## Audio Integration

### UIAudio Static Class
**Methods**:
- `PlayHover()` - Selection changed
- `PlaySelect()` - Item selected
- `PlayBack()` - Cancel/return
- `PlayItemEquip()` - Equipment changed
- `PlayError()` - Invalid action

**Integration**: Uses AudioSystem, routes through FMOD

## Animation System

### PrimeTween Integration
**All animations use `useUnscaledTime: true`** (menu doesn't pause game time)

**Animation Types**:
- Tab swiping (horizontal slide)
- Button selection (scale punch)
- Scroll menu transitions (smooth movement)
- Node focus (camera movement)
- Hold progress (radial fill)

**Example**:
```csharp
Tween.Custom(startPos, endPos, duration,
    onValueChange: pos => transform.position = pos,
    ease: Ease.OutCubic,
    useUnscaledTime: true
);
```

## Best Practices

### Adding New Tabs
1. Create Tab class extending `TabSelection`
2. Create DataProvider implementing `IXXXDataProvider`
3. Add DataProvider to OverworldMenuUI hierarchy
4. Connect in `ConnectDataProvidersToTabs()`
5. Add tab button to TabGroup

### Adding New DataProvider Methods
1. Update interface in `IMenuDataProviders.cs`
2. Implement in concrete DataProvider
3. Call from Tab component

### Raising Stat Change Events
```csharp
using Extensions.EventBus;

// After changing equipment/skills/elements
EventBus<PlayerStatsChangedEvent>.Raise(
    new PlayerStatsChangedEvent("YourSystem - What Changed")
);
```

### Creating Modular Components
- Place in `Assets/Extensions/UI/`
- No game-specific dependencies
- Use events for communication
- Expose all parameters in inspector

## Troubleshooting

### Tabs Don't Open
- Check all tabs are deactivated in `OpenMenu()`
- Verify TabGroup is assigned
- Ensure EventSystem exists

### Controller Navigation Broken
- Check Selectable navigation is set up
- Verify InputManager actions are assigned
- Ensure EventSystem has selected GameObject

### Stats Not Updating
- Verify PlayerStatModifierManager is on PlayerController
- Check EventBus event is raised after changes
- Ensure accessories have `InnateStatChange[]` defined

### Sort Not Working
- Check onSort is subscribed in InventoryTab
- Verify Sort input action exists in InputManager
- Ensure sortMethodText is assigned

### Hold Actions Not Working (Skill Tree)
- Check holdProgressRing is assigned to each SkillTreeNodeUI
- Verify Input actions started/canceled callbacks
- Ensure Update() is called for timer

## Performance Considerations

### Stat Recalculation
- Only happens when `PlayerStatsChangedEvent` raised
- Don't raise event every frame
- Batch changes when possible

### Scroll Menu
- Uses object pooling for panels
- Only visible items are updated
- Buffer panels for smooth scrolling

### Render Textures
- Shared across tabs (deactivate when not in use)
- Single instance, toggled per tab

## Future Extensions

### Inventory Sort
- Date tracking system (add `dateObtained` to InventoryItem)
- Custom sort comparers
- Save/load sort preferences

### Skill Tree
- Stat modifiers on nodes (add `InnateStatChange[]` to SkillTreeNode)
- Multiple skill trees
- Skill tree presets/loadouts

### Equipment
- Set bonuses (when multiple items equipped)
- Equipment durability
- Enhancement/upgrading system

## File Structure
```
Assets/
├── Scripts/
│   ├── UI/Game/OverworldMenu/
│   │   ├── OverworldMenuUI.cs
│   │   ├── Tabs/
│   │   │   ├── CharacterStatsTab.cs
│   │   │   ├── EquipmentTab.cs
│   │   │   ├── ElementProgressTab.cs
│   │   │   ├── SkillTreeTab.cs
│   │   │   └── InventoryTab.cs
│   │   └── DataProviders/
│   │       ├── PlayerDataProvider.cs
│   │       ├── EquipmentDataProvider.cs
│   │       ├── ElementProgressDataProvider.cs
│   │       ├── SkillTreeDataProvider.cs
│   │       └── InventoryDataProvider.cs
│   ├── Player/Control/
│   │   └── PlayerStats.cs (includes stat refresh logic)
│   ├── Events/
│   │   └── PlayerStatsChangedEvent.cs
│   └── Editor/
│       ├── SkillTreeEditorWindow.cs
│       └── PlayerSkillTreeEditor.cs
├── Extensions/UI/
│   ├── TabGroup.cs
│   ├── TabSelection.cs
│   ├── ScrollMenu.cs
│   ├── ItemActionMenu.cs
│   ├── ItemSlotUI.cs
│   ├── SlotGridNavigator.cs
│   ├── IMenuDataProviders.cs
│   ├── MenuDisplayData.cs (includes ItemUIInfo)
│   ├── MenuDisplayDataExtended.cs
│   └── InventorySortMethod.cs
└── Documentation/
    ├── OverworldMenuSummary.md (this file)
    ├── SkillTreeSetup.md
    └── InventoryTabSetup.md
```

## Summary

The Overworld Menu UI system is a comprehensive, modular, and scalable menu framework that:
- Uses MVVM architecture for clean separation
- Supports full controller navigation
- Provides smooth animations and visual feedback
- Automatically manages stat recalculation
- Offers reusable components for future features
- Maintains data integrity with proper event systems

All systems are designed to work independently while communicating through well-defined interfaces and events.

