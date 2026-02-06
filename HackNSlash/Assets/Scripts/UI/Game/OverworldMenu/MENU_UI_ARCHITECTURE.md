# Menu UI System Architecture

## Overview

This is a complete, modular menu UI system for your action RPG game, built following MVVM (Model-View-ViewModel) architecture. The system is designed to be completely decoupled from game logic, allowing the UI to be removed without affecting gameplay.

## Design Principles

### 1. **MVVM Architecture**
- **Model**: Game backend (PlayerInventory, PlayerStats, ElementLoadout, etc.)
- **ViewModel**: Data Providers (PlayerDataProvider, EquipmentDataProvider, etc.)
- **View**: UI Components (Tabs, ScrollMenus, DisplayComponents)

### 2. **Modularity**
- All reusable UI components are in `Extensions.UI` namespace
- Can be copied to other projects without dependencies
- Input handling is separated and assignable externally

### 3. **Data-Driven**
- All parameters exposed in Unity Inspector
- Uses DTOs (Data Transfer Objects) to pass data
- No hard-coded values

### 4. **Interface-Based Communication**
- UI queries data through interfaces (IPlayerDataProvider, IEquipmentDataProvider, etc.)
- Easy to mock for testing
- Clean separation of concerns

## File Structure

```
Assets/
├── Extensions/
│   └── UI/
│       ├── IMenuDataProvider.cs          # Base data provider interface
│       ├── IMenuDataProviders.cs         # All specific data provider interfaces
│       ├── IInputReceiver.cs             # Input handling interfaces
│       ├── MenuDisplayData.cs            # DTO for player stats and equipped items
│       ├── MenuDisplayDataExtended.cs    # DTOs for attacks, quests, compendium
│       ├── UIDisplayConfigs.cs           # Config for render textures and videos
│       ├── RenderTextureDisplay.cs       # Component for 3D model display
│       ├── VideoDisplay.cs               # Component for video playback
│       ├── ItemSlotUI.cs                 # Reusable item slot component
│       ├── PlayerStatsDisplay.cs         # Reusable stats display component
│       ├── ScrollMenu.cs                 # Enhanced scrolling menu (fixed)
│       ├── ScrollUIPanel.cs              # Panel for scroll menu items
│       ├── TabButton.cs                  # Existing tab button
│       ├── TabGroup.cs                   # Existing tab group manager
│       └── TabSelection.cs               # Existing tab content base class
│
└── Scripts/
    └── UI/
        └── Game/
            └── OverworldMenu/
                ├── OverworldMenuUI.cs    # Main menu controller
                ├── EquipmentMenuUI.cs    # Tab 2 (rewritten)
                ├── Tabs/
                │   ├── CharacterStatsTab.cs      # Tab 1: Character/Stats
                │   ├── ElementProgressTab.cs     # Tab 3: Element Progress
                │   ├── SkillTreeTab.cs           # Tab 4: Skill Tree
                │   ├── InventoryTab.cs           # Tab 5: Inventory
                │   ├── MissionsTab.cs            # Tab 6: Quests
                │   ├── CompendiumTab.cs          # Tab 7: Compendium
                │   └── SettingsTab.cs            # Tab 8: Settings
                └── DataProviders/
                    ├── PlayerDataProvider.cs              # Bridges Tab 1
                    ├── EquipmentDataProvider.cs           # Bridges Tab 2
                    ├── ElementProgressDataProvider.cs     # Bridges Tab 3
                    ├── SkillTreeDataProvider.cs           # Bridges Tab 4
                    ├── InventoryDataProvider.cs           # Bridges Tab 5
                    ├── QuestDataProvider.cs               # Bridges Tab 6 (placeholder)
                    └── CompendiumDataProvider.cs          # Bridges Tab 7 (placeholder)
```

## Tab Descriptions

### Tab 1: Character Stats
- **Location**: `CharacterStatsTab.cs`
- **Purpose**: Display player level, stats, and equipped items
- **Components**: PlayerStatsDisplay, ItemSlotUI[], RenderTextureDisplay
- **Data Provider**: PlayerDataProvider
- **Features**: Auto-refreshing stats display, 3D character model

### Tab 2: Equipment Selection
- **Location**: `EquipmentMenuUI.cs`
- **Purpose**: Manage equipped accessories and passive skills
- **Components**: ItemSlotUI[] for slots, ScrollMenu for selection, RenderTextureDisplay
- **Data Provider**: EquipmentDataProvider
- **Features**: Click slot to open scroll menu, compare current vs new items

### Tab 3: Element Progress
- **Location**: `ElementProgressTab.cs`
- **Purpose**: View element levels and assign attacks to buttons
- **Components**: Element buttons, ProgressLevelDisplay[], AttackAssignmentButton[], ScrollMenu
- **Data Provider**: ElementProgressDataProvider
- **Features**: 5 elements, 10 level progression, attack assignment with video preview

### Tab 4: Skill Tree
- **Location**: `SkillTreeTab.cs`
- **Purpose**: Navigate and unlock skills/abilities
- **Components**: SkillNodeUI[], VideoDisplay, skill info panel, RenderTextureDisplay
- **Data Provider**: SkillTreeDataProvider
- **Features**: Navigable tree, center-focused selection, hold to unlock, video demos

### Tab 5: Inventory
- **Location**: `InventoryTab.cs`
- **Purpose**: View all collected items by category
- **Components**: Category buttons, ScrollMenu, item details panel, RenderTextureDisplay
- **Data Provider**: InventoryDataProvider
- **Features**: Category filtering, item descriptions, quantity display

### Tab 6: Missions
- **Location**: `MissionsTab.cs`
- **Purpose**: View and track quests
- **Components**: Quest type toggle, ScrollMenu, quest details panel, RenderTextureDisplay
- **Data Provider**: QuestDataProvider (placeholder)
- **Features**: Main/side quest separation, progress tracking, completion status

### Tab 7: Compendium
- **Location**: `CompendiumTab.cs`
- **Purpose**: View discovered lore, enemies, locations
- **Components**: Category buttons, ScrollMenu, entry details panel
- **Data Provider**: CompendiumDataProvider (placeholder)
- **Features**: Multiple categories, discovery system, detailed descriptions

### Tab 8: Settings
- **Location**: `SettingsTab.cs`
- **Purpose**: Configure game settings
- **Components**: Audio sliders, video dropdowns, gameplay toggles, key remapping
- **Features**: Save/load settings, reset to defaults, full audio/video/gameplay control

## Key Components

### Data Providers
Each data provider implements an interface and acts as a bridge:
```csharp
public interface IPlayerDataProvider
{
    PlayerStatsDisplayData GetPlayerStats();
    EquippedItemDisplayData[] GetEquippedItems();
}

public class PlayerDataProvider : MonoBehaviour, IPlayerDataProvider
{
    // Retrieves data from PlayerController, PlayerStats, PlayerInventory
    // Converts to DTOs for UI consumption
}
```

### DTOs (Data Transfer Objects)
Clean data containers with no logic:
```csharp
[System.Serializable]
public class PlayerStatsDisplayData
{
    public int level;
    public float maxHealth;
    public float currentHealth;
    // ... etc
}
```

### Reusable UI Components
All in `Extensions.UI` namespace, project-agnostic:
- **ScrollMenu**: Enhanced with proper cycle modes and XML documentation
- **ItemSlotUI**: Click/hover selection, empty state handling
- **PlayerStatsDisplay**: Formatted stat display with bars
- **RenderTextureDisplay**: 3D model preview management
- **VideoDisplay**: Video playback for ability demos

## Unity Setup Instructions

### 1. Create GameObject Hierarchy
```
OverworldMenuUI (OverworldMenuUI.cs)
├── EventSystem
├── TabGroup (TabGroup.cs)
│   ├── TabButton1 (TabButton.cs) → Links to CharacterStatsTab
│   ├── TabButton2 (TabButton.cs) → Links to EquipmentTab
│   ├── ... (8 tabs total)
│
├── TabContents
│   ├── Tab1_Character (CharacterStatsTab.cs)
│   ├── Tab2_Equipment (EquipmentMenuUI.cs)
│   ├── Tab3_ElementProgress (ElementProgressTab.cs)
│   ├── Tab4_SkillTree (SkillTreeTab.cs)
│   ├── Tab5_Inventory (InventoryTab.cs)
│   ├── Tab6_Missions (MissionsTab.cs)
│   ├── Tab7_Compendium (CompendiumTab.cs)
│   └── Tab8_Settings (SettingsTab.cs)
│
└── DataProviders
    ├── PlayerDataProvider
    ├── EquipmentDataProvider
    ├── ElementProgressDataProvider
    ├── SkillTreeDataProvider
    ├── InventoryDataProvider
    ├── QuestDataProvider
    └── CompendiumDataProvider
```

### 2. Configure Each Tab
For each tab:
1. Assign the corresponding data provider MonoBehaviour reference
2. Set up UI component references (buttons, text fields, images, etc.)
3. Configure RenderTextureDisplay with camera and render texture
4. Configure ScrollMenu with panels and settings

### 3. Create Render Textures
For 3D character model displays:
1. Create RenderTexture assets
2. Create cameras targeting those render textures
3. Set camera culling masks to specific layers for character model
4. Assign to RenderTextureDisplay components

### 4. Input Setup
The system automatically uses InputManager events:
- `onScroll` for ScrollMenu navigation
- `onTabLeft`/`onTabRight` for tab switching
- Standard Unity Event System for button clicks

## Implementation Status

### ✅ Fully Implemented
- All 8 tab UI components
- All 7 data provider implementations
- MVVM architecture
- Modular UI components
- ScrollMenu enhancements
- Settings system with save/load

### ⚠️ Placeholder/TODO
- **Passive Skill System**: Not yet implemented in backend
  - EquipmentDataProvider returns empty for passives
  - Skill tree integration needed
  
- **Quest System**: Not yet implemented
  - QuestDataProvider uses placeholder data
  - Hook up when quest backend is ready
  
- **Compendium System**: Not yet implemented
  - CompendiumDataProvider uses placeholder data
  - Hook up when lore/discovery system is ready
  
- **Element Progression**: Partially implemented
  - Level system needs backend implementation
  - Benefits tracking needs to be added
  
- **Key Remapping**: Settings tab has placeholder
  - Requires input system rebinding implementation

### 🔨 To Complete by User
1. Create Unity UI layouts for each tab
2. Assign references in Inspector
3. Create render texture assets
4. Configure cameras for character model display
5. Create video clips for ability demonstrations
6. Implement missing backend systems (quests, compendium, progression)
7. Populate real data in placeholder providers

## Usage Example

### Opening the Menu
```csharp
var menu = Services.Get<OverworldMenuUI>();
menu.OpenMenu();
```

### Updating Player Stats (handled automatically)
```csharp
// PlayerDataProvider queries PlayerStats every refresh interval
// No manual updates needed - it's automatic!
```

### Equipping an Item
```csharp
// UI calls through interface:
equipmentDataProvider.EquipAccessory(slotIndex: 0, itemIndex: 5);

// This updates the backend (PlayerInventory.CurrentLoadout)
// UI refreshes automatically on next frame
```

## Extension Points

### Adding a New Tab
1. Create tab class inheriting from `TabSelection`
2. Create data provider interface in `IMenuDataProviders.cs`
3. Implement data provider class
4. Connect in `OverworldMenuUI.InitializeDataProviders()`
5. Add TabButton to TabGroup

### Adding a New Data Source
1. Define interface in `IMenuDataProviders.cs`
2. Create DTO in `MenuDisplayData.cs` or `MenuDisplayDataExtended.cs`
3. Implement provider in `DataProviders/` folder
4. Register as service in Awake()
5. Inject into tab via `SetDataProvider()`

### Creating Reusable Component
1. Place in `Extensions.UI` namespace
2. No dependencies on game-specific classes
3. Use interfaces for external communication
4. Expose all parameters in Inspector
5. Document with XML comments

## Best Practices

1. **Never access game logic directly from UI**
   - Always go through data provider interfaces
   
2. **Use DTOs for all data transfer**
   - No passing ScriptableObjects or MonoBehaviours to UI
   
3. **Keep UI components stateless**
   - All state in data providers or backend
   
4. **Document everything**
   - Use XML comments (/** */) for all public members
   
5. **Test modularity**
   - Should be able to copy Extensions.UI to another project

## Known Issues & Limitations

1. **ScrollMenu**: Fixed debug logging, improved cycle modes
2. **Tab switching**: May need slight delay for activation
3. **RenderTexture**: Requires proper layer setup for character model
4. **Video playback**: Requires VideoClip assets

## Performance Considerations

- **Auto-refresh**: CharacterStatsTab refreshes every 0.5s (configurable)
- **Scroll menu**: Uses object pooling with linked list
- **Render textures**: Cameras disabled when tabs not active
- **Event subscriptions**: Properly cleaned up in OnTabDeselect

## Testing Checklist

- [ ] All tabs can be switched between
- [ ] Data providers return valid data
- [ ] Scroll menus navigate correctly
- [ ] Equipment can be equipped/unequipped
- [ ] Skill tree nodes can be unlocked
- [ ] Settings save and load correctly
- [ ] Character model displays properly
- [ ] No memory leaks (check camera/video deactivation)
- [ ] Input works with both keyboard and controller

---

**Note**: This system is production-ready for UI implementation. Backend systems (quests, compendium, progression) need to be implemented separately and then connected through the data providers.

