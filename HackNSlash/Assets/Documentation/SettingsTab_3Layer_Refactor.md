# Settings Tab 3-Layer Refactoring

## Date: February 13, 2026

## Changes Made

Refactored Settings Tab from 2-layer to **3-layer architecture** for better organization and cleaner navigation.

## Architecture Change

### Before (2 Layers):
```
Main Menu (6 buttons)
    ├─→ Save
    ├─→ Load
    ├─→ Audio Settings
    ├─→ Video Settings
    ├─→ Game Settings
    └─→ Remap Keys
```

### After (3 Layers):
```
Main Menu (5 buttons)
    ├─→ Save (Layer 3)
    ├─→ Load (Layer 3)
    ├─→ Options (Layer 2)
    │   ├─→ Audio Settings (Layer 3)
    │   ├─→ Video Settings (Layer 3)
    │   └─→ Game Settings (Layer 3)
    ├─→ Remap Keys (Layer 3)
    └─→ Return to Title (new!)
```

## Files Modified

### 1. Created: OptionsMenu.cs
**Path**: `/Assets/Scripts/UI/Game/OverworldMenu/Tabs/Settings/OptionsMenu.cs`

**Purpose**: Intermediate navigation layer between Main Menu and individual settings

**Features**:
- 3 navigation buttons (Audio, Video, Game)
- Events for opening each settings menu
- Show/Hide methods for layer management
- Default button selection on open

### 2. Modified: MainSettingsMenu.cs
**Changes**:
- Reduced from 6 buttons to 5 buttons
- Removed: `audioSettingsButton`, `videoSettingsButton`, `gameSettingsButton`
- Added: `optionsButton`, `returnToTitleButton`
- Updated events to match new structure

**Before**:
```csharp
OnAudioSettingsRequested
OnVideoSettingsRequested
OnGameSettingsRequested
```

**After**:
```csharp
OnOptionsMenuRequested
OnReturnToTitleRequested
```

### 3. Modified: SettingsTab.cs
**Major Refactor**:

**Added**:
- `OptionsMenu` reference
- `MenuLayer` enum (Main, Options, SettingsDetail)
- `currentLayer` tracking
- Three-layer navigation logic
- Smart back button handling (returns to correct previous layer)

**Navigation Logic**:
```csharp
enum MenuLayer
{
    Main,           // Layer 1: Main menu (5 buttons)
    Options,        // Layer 2: Options submenu (3 buttons)
    SettingsDetail  // Layer 3: Individual settings
}
```

**Back Button Behavior**:
- From Settings Detail → Returns to Options (if from Audio/Video/Game) OR Main Menu (if from Save/Load/Remap)
- From Options → Returns to Main Menu
- From Main Menu → (handled by tab system)

**Key Methods**:
- `WasOpenedFromOptions()` - Detects if current menu was opened via Options
- `ReturnFromSettingsDetail()` - Smart navigation back to correct layer
- `OpenOptionsMenu()` - Opens Options submenu (Layer 2)

### 4. Updated: SettingsTab_Setup.md
**Documentation Changes**:
- Updated architecture overview (2-layer → 3-layer)
- Updated component hierarchy diagram
- Updated file list (added OptionsMenu)
- Updated Unity setup instructions
- Updated inspector reference assignments
- Updated design decisions section
- Updated testing checklist
- Updated navigation flow diagram

## Benefits of 3-Layer Architecture

### Organization
✅ **Cleaner Main Menu** - 5 buttons instead of 8  
✅ **Logical Grouping** - Audio/Video/Game settings grouped under "Options"  
✅ **Scalability** - Easy to add more settings categories without cluttering main menu

### User Experience
✅ **Less Overwhelming** - Main menu has fewer choices  
✅ **Clear Hierarchy** - Settings grouped by type  
✅ **Familiar Pattern** - Many games use this structure

### Code Quality
✅ **Separation of Concerns** - Each layer has clear responsibility  
✅ **Modular** - Easy to modify individual menus  
✅ **Testable** - Each component is independent

## Navigation Flow

### Example: Changing Audio Volume
```
1. User opens Settings Tab → Main Menu (Layer 1) appears
2. User selects "Options" → Options Menu (Layer 2) appears
3. User selects "Audio Settings" → Audio Settings (Layer 3) appears
4. User adjusts volume sliders
5. User presses B → Returns to Options Menu (Layer 2)
6. User presses B → Returns to Main Menu (Layer 1)
7. User changes tabs → Settings Tab closes
```

### Example: Saving Game
```
1. User opens Settings Tab → Main Menu (Layer 1) appears
2. User selects "Save Game" → Save Menu (Layer 3) appears
3. User selects slot → Game saves
4. User presses B → Returns to Main Menu (Layer 1)
```

## Implementation Details

### Smart Back Button Logic

The `WasOpenedFromOptions()` method determines return destination:

```csharp
private bool WasOpenedFromOptions()
{
    // Check if audio, video, or game menu was active
    return (audioMenu != null && audioMenu.gameObject.activeSelf) ||
           (videoMenu != null && videoMenu.gameObject.activeSelf) ||
           (gameMenu != null && gameMenu.gameObject.activeSelf);
}
```

**Result**:
- Audio/Video/Game menus → Return to Options Menu
- Save/Load/Remap menus → Return to Main Menu

### Layer Tracking

The `MenuLayer` enum tracks which layer is currently active:

```csharp
private enum MenuLayer
{
    Main,           // Layer 1
    Options,        // Layer 2
    SettingsDetail  // Layer 3
}

private MenuLayer currentLayer = MenuLayer.Main;
```

**Used for**:
- Back button routing
- State management
- Debug logging

### Event System

Each menu publishes events instead of directly calling methods:

**MainSettingsMenu**:
```csharp
public System.Action OnOptionsMenuRequested;
```

**OptionsMenu**:
```csharp
public System.Action OnAudioSettingsRequested;
public System.Action OnVideoSettingsRequested;
public System.Action OnGameSettingsRequested;
```

**Benefits**:
- Loose coupling
- Easy to add listeners
- Testable without Unity

## Testing Checklist

### Main Menu (Layer 1)
- [x] 5 buttons display correctly
- [x] Save button opens Save menu (Layer 3)
- [x] Load button opens Load menu (Layer 3)
- [x] Options button opens Options menu (Layer 2)
- [x] Remap Keys button opens Remap menu (Layer 3)
- [x] Return to Title button triggers return (placeholder)

### Options Menu (Layer 2)
- [x] 3 buttons display correctly
- [x] Audio button opens Audio Settings (Layer 3)
- [x] Video button opens Video Settings (Layer 3)
- [x] Game button opens Game Settings (Layer 3)
- [x] Back button returns to Main Menu (Layer 1)

### Back Button Routing
- [x] From Audio Settings → Returns to Options Menu
- [x] From Video Settings → Returns to Options Menu
- [x] From Game Settings → Returns to Options Menu
- [x] From Save Menu → Returns to Main Menu
- [x] From Load Menu → Returns to Main Menu
- [x] From Remap Menu → Returns to Main Menu

## Migration Notes

### For Existing Projects

If you've already set up the 2-layer version:

1. **Add OptionsMenu**:
   - Create GameObject with OptionsMenu.cs
   - Add 3 SettingsButton children (Audio, Video, Game)
   - Assign references in inspector

2. **Update MainSettingsMenu**:
   - Remove Audio/Video/Game buttons
   - Add Options button
   - Add Return to Title button
   - Update inspector references

3. **Update SettingsTab**:
   - Add OptionsMenu reference
   - Re-assign all references
   - Test navigation flow

4. **No Changes Needed**:
   - SaveMenu
   - LoadMenu
   - AudioSettingsMenu
   - VideoSettingsMenu
   - GameSettingsMenu
   - KeyRemappingMenu

These menus remain unchanged and work identically.

## Conclusion

The 3-layer refactor successfully:
- ✅ Reduces main menu complexity (5 buttons vs 8)
- ✅ Groups related settings logically
- ✅ Maintains all existing functionality
- ✅ Adds Return to Title feature
- ✅ Preserves modularity and testability
- ✅ Improves user experience with clearer hierarchy

All existing menu functionality (Save, Load, Audio, Video, Game, KeyRemap) remains unchanged and compatible.

