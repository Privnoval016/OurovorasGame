# Settings Tab Setup Guide

## Overview

The Settings Tab uses a **three-layer architecture** with modular, single-responsibility components:

**Layer 1:** Main Settings Menu (5 navigation buttons: Save, Load, Options, Remap Keys, Return to Title)  
**Layer 2:** Options Submenu (3 navigation buttons: Audio, Video, Game)  
**Layer 3:** Individual setting menus (Save, Load, Audio Settings, Video Settings, Game Settings, Key Remapping)

## Architecture

### Component Hierarchy

```
SettingsTab (Controller)
├── MainSettingsMenu (Layer 1)
│   ├── Save Button → Opens SaveMenu (Layer 3)
│   ├── Load Button → Opens LoadMenu (Layer 3)
│   ├── Options Button → Opens OptionsMenu (Layer 2)
│   ├── Remap Keybinds Button → Opens KeyRemappingMenu (Layer 3)
│   └── Return to Title Button → Returns to title screen
│
├── OptionsMenu (Layer 2)
│   ├── Audio Button → Opens AudioSettingsMenu (Layer 3)
│   ├── Video Button → Opens VideoSettingsMenu (Layer 3)
│   └── Game Button → Opens GameSettingsMenu (Layer 3)
│
├── SaveMenu (Layer 3)
│   └── 3 SaveSlotButtons (Slot 1-3)
│
├── LoadMenu (Layer 3)
│   └── 4 SaveSlotButtons (Autosave + Slot 1-3)
│
├── AudioSettingsMenu (Layer 3)
│   ├── Master Volume Slider
│   ├── Music Volume Slider
│   ├── SFX Volume Slider
│   ├── Save Button
│   └── Reset Button
│
├── VideoSettingsMenu (Layer 3)
│   ├── Resolution Dropdown
│   ├── Quality Dropdown
│   ├── Fullscreen Toggle
│   ├── VSync Toggle
│   ├── Brightness Slider
│   ├── Save Button
│   └── Reset Button
│
├── GameSettingsMenu (Layer 3)
│   ├── Camera Sensitivity Slider
│   ├── Invert Y Axis Toggle
│   ├── ScrollRect (for future expansion)
│   ├── Save Button
│   └── Reset Button
│
└── KeyRemappingMenu (Layer 3)
    ├── Remap Button Container
    ├── Save Button
    └── Reset Button
```

---

## Files Created

### Custom Selectables (in `/Assets/Extensions/UI/`)
1. **SettingsButton.cs** - Navigation buttons with animations
2. **SaveSlotButton.cs** - Save/load slot buttons with save info display

### Data Providers (in `/Assets/Scripts/UI/Game/OverworldMenu/DataProviders/`)
3. **ISaveDataProvider.cs** - Interface for save system
4. **SaveDataProvider.cs** - Placeholder implementation (replace with real save system)

### Menu Components (in `/Assets/Scripts/UI/Game/OverworldMenu/Tabs/Settings/`)
5. **MainSettingsMenu.cs** - Layer 1: Main navigation (5 buttons)
6. **OptionsMenu.cs** - Layer 2: Options submenu (3 buttons: Audio, Video, Game)
7. **SaveMenu.cs** - Layer 3: 3 save slots for manual saves
8. **LoadMenu.cs** - Layer 3: 4 slots (autosave + 3 manual)
9. **AudioSettingsMenu.cs** - Layer 3: FMOD volume controls
10. **VideoSettingsMenu.cs** - Layer 3: Resolution, quality, fullscreen, vsync, brightness
11. **GameSettingsMenu.cs** - Layer 3: Scrollable gameplay settings (sensitivity, invert Y)
12. **KeyRemappingMenu.cs** - Layer 3: Key rebinding interface (placeholder)

### Main Controller (in `/Assets/Scripts/UI/Game/OverworldMenu/Tabs/`)
13. **SettingsTab.cs** (refactored) - Three-layer navigation controller

---

## Unity Setup Instructions

### Step 1: Create UI Hierarchy

Create this hierarchy in your Settings Tab panel:

```
SettingsTab (GameObject with SettingsTab.cs)
├── MainSettingsMenu (GameObject with MainSettingsMenu.cs)
│   ├── ButtonContainer (Vertical Layout Group)
│   │   ├── SaveButton (GameObject with SettingsButton.cs)
│   │   ├── LoadButton (GameObject with SettingsButton.cs)
│   │   ├── OptionsButton (GameObject with SettingsButton.cs)
│   │   ├── KeyRemapButton (GameObject with SettingsButton.cs)
│   │   └── ReturnToTitleButton (GameObject with SettingsButton.cs)
│
├── OptionsMenu (GameObject with OptionsMenu.cs)
│   ├── ButtonContainer (Vertical Layout Group)
│   │   ├── AudioButton (GameObject with SettingsButton.cs)
│   │   ├── VideoButton (GameObject with SettingsButton.cs)
│   │   └── GameButton (GameObject with SettingsButton.cs)
│
├── SaveMenu (GameObject with SaveMenu.cs)
│   ├── SaveDataProvider (GameObject with SaveDataProvider.cs)
│   └── SlotContainer (Vertical Layout Group)
│       ├── Slot1 (GameObject with SaveSlotButton.cs)
│       ├── Slot2 (GameObject with SaveSlotButton.cs)
│       └── Slot3 (GameObject with SaveSlotButton.cs)
│
├── LoadMenu (GameObject with LoadMenu.cs)
│   ├── SaveDataProvider (reuse from SaveMenu)
│   └── SlotContainer (Vertical Layout Group)
│       ├── Autosave (GameObject with SaveSlotButton.cs)
│       ├── Slot1 (GameObject with SaveSlotButton.cs)
│       ├── Slot2 (GameObject with SaveSlotButton.cs)
│       └── Slot3 (GameObject with SaveSlotButton.cs)
│
├── AudioSettingsMenu (GameObject with AudioSettingsMenu.cs)
│   ├── ScrollRect
│   │   └── Content
│   │       ├── MasterVolumeSlider (Slider)
│   │       ├── MusicVolumeSlider (Slider)
│   │       ├── SFXVolumeSlider (Slider)
│   │       ├── SaveButton (GameObject with SettingsButton.cs)
│   │       └── ResetButton (GameObject with SettingsButton.cs)
│
├── VideoSettingsMenu (GameObject with VideoSettingsMenu.cs)
│   ├── ScrollRect
│   │   └── Content
│   │       ├── ResolutionDropdown (TMP_Dropdown)
│   │       ├── QualityDropdown (TMP_Dropdown)
│   │       ├── FullscreenToggle (Toggle)
│   │       ├── VSyncToggle (Toggle)
│   │       ├── BrightnessSlider (Slider)
│   │       ├── SaveButton (GameObject with SettingsButton.cs)
│   │       └── ResetButton (GameObject with SettingsButton.cs)
│
├── GameSettingsMenu (GameObject with GameSettingsMenu.cs)
│   ├── ScrollRect
│   │   └── Content
│   │       ├── CameraSensitivitySlider (Slider)
│   │       ├── InvertYToggle (Toggle)
│   │       ├── SaveButton (GameObject with SettingsButton.cs)
│   │       └── ResetButton (GameObject with SettingsButton.cs)
│
└── KeyRemappingMenu (GameObject with KeyRemappingMenu.cs)
    ├── InstructionText (TextMeshProUGUI)
    ├── RemapButtonContainer (Vertical Layout Group)
    ├── SaveButton (GameObject with SettingsButton.cs)
    └── ResetButton (GameObject with SettingsButton.cs)
```

### Step 2: Assign References in Inspector

#### SettingsTab Component
- **mainMenu** → MainSettingsMenu GameObject
- **optionsMenu** → OptionsMenu GameObject
- **saveMenu** → SaveMenu GameObject
- **loadMenu** → LoadMenu GameObject
- **audioMenu** → AudioSettingsMenu GameObject
- **videoMenu** → VideoSettingsMenu GameObject
- **gameMenu** → GameSettingsMenu GameObject
- **keyRemapMenu** → KeyRemappingMenu GameObject

#### MainSettingsMenu Component
- **saveButton** → SaveButton (SettingsButton)
- **loadButton** → LoadButton (SettingsButton)
- **optionsButton** → OptionsButton (SettingsButton)
- **keyRemappingButton** → KeyRemapButton (SettingsButton)
- **returnToTitleButton** → ReturnToTitleButton (SettingsButton)
- **defaultButton** → First button to select (e.g., SaveButton)

#### OptionsMenu Component
- **audioButton** → AudioButton (SettingsButton)
- **videoButton** → VideoButton (SettingsButton)
- **gameButton** → GameButton (SettingsButton)
- **defaultButton** → First button to select (e.g., AudioButton)

#### SaveMenu Component
- **slot1Button** → Slot1 (SaveSlotButton)
- **slot2Button** → Slot2 (SaveSlotButton)
- **slot3Button** → Slot3 (SaveSlotButton)
- **saveDataProvider** → SaveDataProvider GameObject
- **defaultButton** → First slot button

#### LoadMenu Component
- **autosaveButton** → Autosave (SaveSlotButton)
- **slot1Button** → Slot1 (SaveSlotButton)
- **slot2Button** → Slot2 (SaveSlotButton)
- **slot3Button** → Slot3 (SaveSlotButton)
- **saveDataProvider** → Same SaveDataProvider as SaveMenu
- **defaultButton** → Autosave button

#### AudioSettingsMenu Component
- **masterVolumeSlider** → Master Volume Slider
- **musicVolumeSlider** → Music Volume Slider
- **sfxVolumeSlider** → SFX Volume Slider
- **masterVolumeText** → Text showing master volume %
- **musicVolumeText** → Text showing music volume %
- **sfxVolumeText** → Text showing SFX volume %
- **saveButton** → Save Button (SettingsButton)
- **resetButton** → Reset Button (SettingsButton)
- **saveStatusText** → Text showing "Settings Saved!"
- **defaultButton** → Master volume slider

#### VideoSettingsMenu Component
- **resolutionDropdown** → Resolution Dropdown (TMP_Dropdown)
- **qualityDropdown** → Quality Dropdown (TMP_Dropdown)
- **fullscreenToggle** → Fullscreen Toggle
- **vsyncToggle** → VSync Toggle
- **brightnessSlider** → Brightness Slider
- **brightnessText** → Text showing brightness %
- **saveButton** → Save Button (SettingsButton)
- **resetButton** → Reset Button (SettingsButton)
- **saveStatusText** → Text showing "Settings Saved!"
- **defaultButton** → Resolution dropdown

#### GameSettingsMenu Component
- **cameraSensitivitySlider** → Camera Sensitivity Slider
- **cameraSensitivityText** → Text showing sensitivity value
- **invertYAxisToggle** → Invert Y Axis Toggle
- **scrollRect** → ScrollRect component (for future expansion)
- **saveButton** → Save Button (SettingsButton)
- **resetButton** → Reset Button (SettingsButton)
- **saveStatusText** → Text showing "Settings Saved!"
- **defaultButton** → Camera sensitivity slider

#### KeyRemappingMenu Component
- **remapButtonPrefab** → Prefab for remap buttons (create later)
- **remapButtonContainer** → Parent transform for buttons
- **instructionText** → Text showing instructions
- **saveButton** → Save Button (SettingsButton)
- **resetButton** → Reset Button (SettingsButton)
- **saveStatusText** → Text showing "Bindings Saved!"
- **defaultButton** → First remap button

### Step 3: Create Custom Selectable Prefabs

#### SettingsButton Prefab
1. Create GameObject with Image (background)
2. Add Image (border) as child
3. Add TextMeshProUGUI (label) as child
4. Add Image (icon, optional) as child
5. Add SettingsButton component
6. Assign references:
   - **backgroundImage** → Background Image
   - **borderImage** → Border Image
   - **labelText** → Label TextMeshProUGUI
   - **iconImage** → Icon Image (optional)

#### SaveSlotButton Prefab
1. Create GameObject with Image (background)
2. Add Image (border) as child
3. Add TextMeshProUGUI (slotNumber) - "SLOT 1", "AUTOSAVE", etc.
4. Add TextMeshProUGUI (saveName) - Save name
5. Add TextMeshProUGUI (timestamp) - "2/12/26 3:45 PM"
6. Add TextMeshProUGUI (level) - "Level 15"
7. Add GameObject (emptySlotIndicator) - Shows when slot is empty
8. Add SaveSlotButton component
9. Assign references accordingly

### Step 4: Configure Navigation

Set up Unity UI navigation for controller support:

1. **Automatic Navigation**: Unity's navigation system should work automatically with Vertical/Horizontal Layout Groups
2. **Manual Navigation**: If automatic fails, manually set navigation:
   - Buttons in vertical list: Set **Up** and **Down** to adjacent buttons
   - Sliders/toggles: Make sure they're part of navigation chain
3. **Default Button**: Each menu's `defaultButton` field determines what's selected on open

---

## Design Decisions

### Why Three Layers?

**Separation of Concerns:**
- Layer 1 (MainSettingsMenu) = Top-level navigation (Save, Load, Options, Remap, Title)
- Layer 2 (OptionsMenu) = Settings category navigation (Audio, Video, Game)
- Layer 3 (Individual Menus) = Actual settings logic and controls
- Controller (SettingsTab) = Layer switching, back button handling

**Benefits:**
- ✅ Cleaner main menu (5 buttons instead of 8)
- ✅ Logical grouping (Audio/Video/Game settings under "Options")
- ✅ Each menu is self-contained and testable
- ✅ Easy to add new setting categories without cluttering main menu
- ✅ Clear single responsibility for each component
- ✅ More organized navigation flow

**Navigation Flow:**
```
Main Menu (Layer 1)
    ├─→ Save (Layer 3)
    ├─→ Load (Layer 3)
    ├─→ Options (Layer 2)
    │   ├─→ Audio Settings (Layer 3)
    │   ├─→ Video Settings (Layer 3)
    │   └─→ Game Settings (Layer 3)
    ├─→ Remap Keys (Layer 3)
    └─→ Return to Title
```

### Why Custom Selectables?

**Instead of Unity Buttons:**
- Full control over animations via UIAnimationManager
- Consistent visual feedback across all menus
- Events (onPressed, onSlotSelected) instead of onClick
- Extends Selectable for controller navigation

### Why Placeholder Data Provider?

**ISaveDataProvider + SaveDataProvider:**
- UI is completely decoupled from save system implementation
- Can swap out SaveDataProvider with real save system later
- Follows MVVM pattern (View ↔ ViewModel ↔ Model)
- Easy to unit test UI without save system

### Why ScrollRect in GameSettingsMenu?

**Future Expansion:**
- Currently only 2 settings (sensitivity + invert Y)
- ScrollRect allows adding many more settings later
- Same pattern could apply to other menus if needed

### Why Placeholder Key Remapping?

**Complexity:**
- InputSystem rebinding requires significant implementation
- Per-action rebinding UI needs careful design
- Conflict resolution (multiple actions on same button)
- Save/load binding overrides
- This is marked as TODO for future implementation

---

## Implementation Status

### ✅ Fully Implemented

1. **SettingsTab** - Three-layer controller ✅
2. **MainSettingsMenu** - Layer 1 navigation (5 buttons) ✅
3. **OptionsMenu** - Layer 2 navigation (3 buttons) ✅
4. **SaveMenu** - Save slot selection ✅
5. **LoadMenu** - Load slot selection ✅
6. **AudioSettingsMenu** - FMOD volume controls ✅
7. **VideoSettingsMenu** - Video settings ✅
8. **GameSettingsMenu** - Gameplay settings ✅

### ⚠️ Placeholder / TODO

1. **KeyRemappingMenu** - Needs InputSystem rebinding implementation
2. **SaveDataProvider** - Needs real save system integration
3. **Camera Sensitivity** - Needs camera controller integration
4. **Brightness** - Needs post-processing integration
5. **Return to Title** - Needs scene loading implementation

---

## Testing Checklist

### Main Menu Navigation (Layer 1)
- [ ] Can navigate between 5 buttons with D-pad
- [ ] Each button plays hover sound on select
- [ ] Each button plays select sound on press
- [ ] Save button opens Save menu
- [ ] Load button opens Load menu
- [ ] Options button opens Options menu
- [ ] Remap Keys button opens Key Remapping menu
- [ ] Return to Title button triggers return (placeholder)

### Options Menu Navigation (Layer 2)
- [ ] Can navigate between 3 buttons with D-pad
- [ ] Audio button opens Audio Settings
- [ ] Video button opens Video Settings
- [ ] Game button opens Game Settings
- [ ] Pressing B returns to Main Menu

### Save Menu (Layer 3)
- [ ] Shows 3 save slots
- [ ] Empty slots display "Empty Slot"
- [ ] Occupied slots show save name, timestamp, level
- [ ] Pressing A on slot saves game
- [ ] Save confirmation plays
- [ ] Pressing B returns to Main Menu

### Load Menu (Layer 3)
- [ ] Shows autosave + 3 manual slots
- [ ] Cannot load from empty slots (plays error sound)
- [ ] Pressing A on occupied slot loads game
- [ ] Load confirmation plays
- [ ] Pressing B returns to Main Menu

### Audio Settings (Layer 3)
- [ ] Master volume slider adjusts FMOD master bus
- [ ] Music volume slider adjusts FMOD music bus
- [ ] SFX volume slider adjusts FMOD SFX bus
- [ ] Volume percentages update in real-time
- [ ] Settings auto-save when leaving menu
- [ ] Reset button restores to 100% all volumes
- [ ] Pressing B returns to Options Menu

### Video Settings (Layer 3)
- [ ] Resolution dropdown lists all available resolutions
- [ ] Quality dropdown shows Unity quality presets
- [ ] Fullscreen toggle works
- [ ] VSync toggle works
- [ ] Brightness slider adjusts ambient intensity
- [ ] Settings auto-save when leaving menu
- [ ] Reset button restores defaults
- [ ] Pressing B returns to Options Menu

### Game Settings (Layer 3)
- [ ] Camera sensitivity slider updates text
- [ ] Invert Y axis toggle works
- [ ] ScrollRect scrolls (test with more settings added)
- [ ] Settings auto-save when leaving menu
- [ ] Reset button restores defaults
- [ ] Pressing B returns to Options Menu

### Key Remapping (Layer 3)
- [ ] Instruction text displays
- [ ] Pressing B returns to Main Menu
- [ ] (Rebinding functionality pending)

---

## Future Enhancements

### Save System Integration
- Connect SaveDataProvider to actual save/load implementation
- Add save file metadata (playtime, location, etc.)
- Add save file screenshots
- Add confirmation dialogs for overwrite/delete

### Key Remapping Implementation
- Implement InputSystem rebinding using `PerformInteractiveRebinding()`
- Create remap button for each combat action
- Handle binding conflicts
- Save binding overrides to file
- Add "listening for input" visual feedback

### Game Settings Expansion
- Add FOV slider
- Add aim assist toggle (if applicable)
- Add difficulty selection
- Add language selection
- Add subtitle settings
- Add colorblind mode

### Video Settings Expansion
- Add anti-aliasing options
- Add shadow quality
- Add texture quality
- Add effects quality
- Add frame rate cap
- Add HDR toggle

### Audio Settings Expansion
- Add individual bus volume controls (Ambient, UI, Dialogue, etc.)
- Add 3D audio toggle
- Add headphone mode
- Add audio device selection

---

## Common Issues & Solutions

### Issue: Buttons Not Navigating
**Solution**: Check Navigation settings in Unity Inspector. Set Navigation to "Automatic" or manually configure Up/Down/Left/Right.

### Issue: FMOD Buses Not Found
**Solution**: Ensure FMOD bus names match exactly: "bus:/", "bus:/Music", "bus:/SFX". Check FMOD Studio project.

### Issue: Sliders Not Responding
**Solution**: Ensure Slider component has "Interactable" checked. Check if EventSystem is present in scene.

### Issue: Save/Load Not Working
**Solution**: SaveDataProvider is a placeholder. Replace with actual save system that implements ISaveDataProvider.

### Issue: Menus Not Hiding/Showing
**Solution**: Check that all submenu GameObjects start inactive in editor (except MainSettingsMenu).

---

## Architecture Diagram

```
User Input (Controller) 
    ↓
InputManager 
    ↓
SettingsTab (Controller)
    ↓
MainSettingsMenu (Navigation)
    ↓
Individual Menus (Settings Logic)
    ↓
Data Providers (Data Access)
    ↓
Game Systems (FMOD, Unity Settings, Save System)
```

This architecture ensures:
- **Modularity**: Each menu is independent
- **Testability**: Can test menus without game systems
- **Scalability**: Easy to add new settings
- **Maintainability**: Clear single responsibilities

