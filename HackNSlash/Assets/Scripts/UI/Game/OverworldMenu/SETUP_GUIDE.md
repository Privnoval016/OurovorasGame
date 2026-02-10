# Menu UI System - Setup Guide

## Table of Contents
1. [Quick Start](#quick-start)
2. [Scene Setup](#scene-setup)
3. [Tab Setup](#tab-setup)
4. [Component Configuration](#component-configuration)
5. [Navigation Parameters](#navigation-parameters)
6. [Common Issues](#common-issues)

---

## Quick Start

### Prerequisites
- Unity Input System package installed
- PrimeTween package installed (https://github.com/KyryloKuzyk/PrimeTween)
- FMOD for Unity installed
- EventBus system in place (Extensions/EventBus)
- AudioSystem configured

### Minimal Setup (5 Steps)

1. **Create Canvas**
   - Add Canvas to scene (Screen Space - Overlay)
   - Add EventSystem to scene (should auto-create)

2. **Add OverworldMenuUI**
   - Create empty GameObject named "OverworldMenuUI"
   - Add `OverworldMenuUI` component
   - Assign data providers (see Data Providers section)

3. **Create Tab Buttons**
   - Create horizontal container for tab buttons
   - Add TabGroup component to container
   - Add TabButton components to child buttons (one per tab)

4. **Create Tab Content**
   - Create container for each tab's content
   - Add appropriate tab component (CharacterStatsTab, EquipmentTab, etc.)
   - Assign content panels to TabButtons

5. **Add MenuNavigationConfigurator**
   - Add to OverworldMenuUI GameObject or tab container
   - Set Auto Configure On Start = true
   - Navigation auto-configures at runtime!

---

## Scene Setup

### Canvas Configuration

```
Canvas (Screen Space - Overlay)
├── OverworldMenuUI (GameObject)
│   ├── OverworldMenuUI (Component)
│   ├── MenuNavigationConfigurator (Component)
│   └── [Data Provider components]
│
├── TabButtons (GameObject with TabGroup)
│   ├── TabButton1 (TabButton component)
│   ├── TabButton2 (TabButton component)
│   ├── ... (8 tabs total)
│   └── TabButton8 (TabButton component)
│
└── TabContent (GameObject)
    ├── CharacterStatsPanel (TabSelection, CharacterStatsTab)
    ├── EquipmentPanel (TabSelection, EquipmentTab)
    ├── ... (8 panels total)
    └── SettingsPanel (TabSelection, SettingsTab)
```

### EventSystem
- Automatically created when you add a Canvas
- Ensure only ONE EventSystem exists in scene
- Navigation Mode: Automatic (default)

---

## Tab Setup

### TabButton Configuration

Each TabButton needs these assignments in Inspector:

#### Required
- **Tab Group**: Auto-found from parent if not assigned
- **Content Panel**: The TabSelection component this button controls (e.g., CharacterStatsTab)

#### Optional (Auto-configured)
- **Previous Tab Button**: Used for navigation - leave empty, set by MenuNavigationConfigurator
- **Next Tab Button**: Used for navigation - leave empty, set by MenuNavigationConfigurator
- **First Selectable In Content**: First button in tab - leave empty, auto-found

#### Visual Settings (Customize as Needed)
- **Select Duration**: 0.2s (how fast tab scales up when selected)
- **Deselect Duration**: 0.15s (how fast tab scales down when deselected)
- **Selected Scale**: 1.1 (scale multiplier when selected, 1.1 = 10% larger)
- **Select Ease**: OutBack (easing for select animation - adds overshoot)
- **Deselect Ease**: OutQuad (easing for deselect animation - smooth)

#### Color Settings
- **Normal Color**: White (tab color when not selected)
- **Highlighted Color**: Light gray #C8C8C8 (unused since no mouse hover)
- **Selected Color**: Yellow (tab color when selected)
- **Pressed Color**: Gray #808080 (unused since tabs aren't clickable)

**Note**: Tab buttons are **non-interactive** - they only respond to bumper input (LB/RB) through TabGroup, not direct selection/clicking.

### TabSelection Configuration

Each tab content panel needs:

#### Base Settings
- Add appropriate Tab component (e.g., `CharacterStatsTab`)
- Inherits from `TabSelection` base class
- Add CanvasGroup component (for fade animations)

#### CanvasGroup Settings (IMPORTANT)
When tab is inactive:
- **Alpha**: 0 (invisible)
- **Interactable**: false (can't interact)
- **Block Raycasts**: false (doesn't block input)

These are automatically set by TabSelection.OnTabSelect/OnTabDeselect

### Tab Components

#### Tab 1: CharacterStatsTab
- Displays player name, level, stats, equipped items
- Requires: IPlayerDataProvider
- Components:
  - PlayerStatsDisplay (shows stats)
  - EquippedItemsDisplay (shows equipped items)
  - RenderTextureDisplay (3D player model preview)

#### Tab 2: EquipmentTab
- Equipment and passive selection
- Requires: IEquipmentDataProvider
- Components:
  - ItemSlotUI x6 (3 equipment + 3 passive slots)
  - SlotGridNavigator (auto-configures grid navigation)
  - ScrollMenu (for item selection)
  - RenderTextureDisplay (3D player model preview)

**Special Setup**: Equipment slot containers may use DrillDownNavigator if you want to view detailed unlock info.

#### Tab 3: ElementProgressTab
- Element leveling and attack assignment
- Requires: IElementProgressDataProvider
- Components:
  - Element selector buttons (Fire, Water, Earth, Air, Lightning)
  - ProgressLevelDisplay x10 (level progression bars)
  - Attack assignment buttons x3 (A/X/Y button assignments)
  - ScrollMenu (for attack selection)
  - RenderTextureDisplay (3D player model preview)

#### Tab 4: SkillTreeTab
- Skill tree visualization and unlocking
- Requires: ISkillTreeDataProvider
- Components:
  - SkillNodeUI components (generated at runtime)
  - Skill description display
  - VideoDisplay (optional skill preview videos)
  - RenderTextureDisplay (3D player model in background)

**Special**: Skill nodes use ISelectHandler/ISubmitHandler for controller navigation.

#### Tab 5: InventoryTab
- Item browsing and usage
- Requires: IInventoryDataProvider
- Components:
  - Item category buttons (All, Consumables, Materials, etc.)
  - ScrollMenu (for item selection)
  - Item description display
  - RenderTextureDisplay (3D player model preview)

#### Tab 6: MissionsTab
- Quest tracking
- Requires: IQuestDataProvider
- Components:
  - Quest type buttons (Main, Side)
  - ScrollMenu (for quest selection)
  - Quest description display
  - RenderTextureDisplay (3D player model in background)

#### Tab 7: CompendiumTab
- Lore and world information
- Requires: ICompendiumDataProvider
- Components:
  - Category buttons
  - ScrollMenu (for entry selection)
  - Entry description display

#### Tab 8: SettingsTab
- Game settings (audio, video, controls)
- Requires: None (uses PlayerPrefs and direct FMOD bus access)
- Components:
  - Audio sliders (Master, Music, SFX, UI)
  - Video settings dropdowns
  - Key remapping buttons
  - Save/Load buttons

---

## Component Configuration

### ScrollMenu Setup

Used for item/quest/attack selection with carousel scrolling.

#### Inspector Settings

**Scroll Settings**:
- **Axis**: Vertical or Horizontal (direction of scrolling)
- **Cycle Mode**: 
  - `CircularStop`: Wraps only if items ≥ panels, else stops at edges
  - `CircularPure`: Always wraps (true carousel)
  - `Restart`: Stops at edges, jumps to start when going past end
  - `Stop`: Stops at first/last item, no wrapping
- **Focus Center Panel**: true (scales up center panel)
- **Center Panel Index**: Usually 2 (middle panel is index 2 in 5-panel setup)

**Timing**:
- **Scroll Cooldown**: 0.11s (prevents rapid scrolling)
- **Scroll Duration**: 0.1s (animation speed - fast and responsive)

**Visuals**:
- **Normal Scale**: (1, 1, 1) (scale of non-focused panels)
- **Focused Scale**: (1.15, 1.15, 1.15) (scale of center panel)
- **Scale Duration**: 0.08s (how fast focus animation plays)

**UI**:
- **Scroll Item Container**: Parent Transform containing all ScrollUIPanel children

#### Usage in Code

```csharp
// Activate with items
scrollMenu.Activate(
    items: itemList,
    initialIndex: 0,
    getInfoFunc: item => new ItemUIInfo { /* ... */ },
    authority: this // implements IScrollMenuAuthority
);

// Implement IScrollMenuAuthority
public void SubscribeToScroll(ScrollMenu menu)
{
    InputManager.Instance.onScroll += menu.OnScrollPerformed;
}

public void UnsubscribeFromScroll(ScrollMenu menu)
{
    InputManager.Instance.onScroll -= menu.OnScrollPerformed;
}
```

**Important**: ScrollMenu requires Update() to run for cooldown timer - make sure GameObject is active!

### SlotGridNavigator Setup

Auto-configures grid navigation for button grids (like equipment slots).

#### Inspector Settings

**Grid Configuration**:
- **Columns**: 3 (number of columns in grid, e.g., 3x2 = 6 slots)
- **Rows**: 2 (number of rows in grid)

**External Navigation**:
- **Select On Up From Top**: What to select when pressing up from top row (e.g., tab button)
- **Select On Down From Bottom**: What to select when pressing down from bottom row (e.g., next section)

**Auto-Configuration**:
- **Auto Configure On Start**: true (recommended - runs automatically)

#### How It Works

1. Finds all ItemSlotUI children
2. Calculates grid position for each (row/col)
3. Sets up explicit navigation:
   - Left: Previous column
   - Right: Next column
   - Up: Previous row or external element
   - Down: Next row or external element
4. Adds/configures Button components automatically

**No manual navigation setup required!**

### DrillDownNavigator Setup

Enables "drill-down" navigation for nested content (e.g., viewing individual unlocks in equipment container).

#### Inspector Settings

**References**:
- **Drill Down Container**: Container to drill into (null = this GameObject)
- **First Child Selectable**: First button to select when drilling in
- **Selection Indicator**: Visual feedback GameObject (enabled when selected)

**Settings**:
- **Auto Find First Child**: true (automatically finds first Selectable in children)
- **Play Audio**: true (plays audio feedback on drill in/out)

#### How It Works

1. User navigates to container → Container selected (shows indicator)
2. User presses A (Submit) → DrillDown()
   - Disables parent navigation
   - Selects first child element
   - User can now navigate children
3. User presses B (Cancel) → DrillUp()
   - Re-enables parent navigation
   - Returns to parent container selection

**Perfect for**: Equipment unlock grids, nested menus, detail views

### SharedRenderTextureController Setup

Animates a single render texture camera between tabs (saves performance).

#### Inspector Settings

**Render Texture**:
- **Render Texture Camera**: Camera rendering 3D player model
- **Render Texture Display**: RawImage showing the render texture

**Animation**:
- **Animation Duration**: 0.4s (smooth slide between tabs)
- **Animation Ease**: OutCubic (smooth, natural motion)

**Tab Configurations** (8 entries, one per tab):
- **Tab Index**: 0-7 (which tab this config is for)
- **Target Position**: Local position for this tab (e.g., (200, -100, 0) for right side)
- **Target Scale**: Local scale for this tab (e.g., (1.5, 1.5, 1) for larger display)
- **Is Active**: true if render texture should be visible in this tab

#### Example Configuration

```
Tab 0 (Character Stats): Position (0, 0, 0), Scale (2, 2, 1), Active = true (center, large)
Tab 1 (Equipment): Position (300, 0, 0), Scale (1.5, 1.5, 1), Active = true (right side)
Tab 2 (Element Progress): Position (300, 0, 0), Scale (1.5, 1.5, 1), Active = true (right side)
Tab 3 (Skill Tree): Position (0, 0, 0), Scale (1, 1, 1), Active = true (background)
Tab 4 (Inventory): Position (300, 0, 0), Scale (1.5, 1.5, 1), Active = true (right side)
Tab 5 (Missions): Position (-200, 0, 0), Scale (1, 1, 1), Active = true (background, left)
Tab 6 (Compendium): Active = false (not shown)
Tab 7 (Settings): Active = false (not shown)
```

**Performance Benefit**: Only ONE render texture camera needed instead of 8!

### MenuNavigationConfigurator Setup

Automatically configures ALL navigation for entire menu system.

#### Inspector Settings

**Auto-Configuration**:
- **Auto Configure On Start**: true (HIGHLY recommended - runs automatically)
- **Auto Select First Tab**: true (selects first tab when menu opens)
- **Initial Tab Index**: 0 (which tab to select first, 0-based)

#### What It Configures

1. **Tab Button Chain**:
   - Horizontal navigation between tabs (wraps around)
   - Down navigation from tabs to content
   
2. **Tab Content**:
   - Up navigation from first element back to tab
   - Finds and configures all SlotGridNavigators
   - Finds all DrillDownNavigators

3. **Initial Selection**:
   - Selects initial tab
   - Sets EventSystem.current.selectedGameObject

**Result**: Entire menu is navigable with zero manual navigation setup!

---

## Navigation Parameters

### Understanding Navigation Modes

Unity's Navigation system has several modes:

- **None**: No automatic navigation (use for tabs - bumper-only)
- **Horizontal**: Automatic left/right navigation
- **Vertical**: Automatic up/down navigation
- **Automatic**: Unity auto-detects (can be unpredictable)
- **Explicit**: Manual control (RECOMMENDED for precise control)

**This system uses Explicit mode for all content navigation** - fully configured in code by MenuNavigationConfigurator.

### Button Parameters

When MenuNavigationConfigurator adds/configures Buttons:

#### Navigation (Selectable.navigation)
- **Mode**: Navigation.Mode.Explicit
- **Select On Up**: Explicit Selectable to go to when pressing up
- **Select On Down**: Explicit Selectable to go to when pressing down
- **Select On Left**: Explicit Selectable to go to when pressing left
- **Select On Right**: Explicit Selectable to go to when pressing right

#### Transition (Selectable.transition)
- **Transition**: Selectable.Transition.ColorTint
- **Target Graphic**: Button's Image component

#### Colors (ColorBlock)
- **Normal Color**: (1, 1, 1, 1) white
- **Highlighted Color**: (0.78, 0.78, 0.78, 1) light gray
- **Pressed Color**: (0.5, 0.5, 0.5, 1) gray
- **Selected Color**: (1, 1, 0, 1) yellow (when selected by EventSystem)
- **Disabled Color**: (0.5, 0.5, 0.5, 0.5) transparent gray
- **Color Multiplier**: 1
- **Fade Duration**: 0.1s

### Default Button (First Selection)

**What is it?** The first GameObject selected when a tab opens.

**How it's set**: 
- MenuNavigationConfigurator finds first Selectable in tab content
- Calls `EventSystem.current.SetSelectedGameObject(button.gameObject)`

**Why it matters**: 
- EventSystem needs a selected GameObject for controller navigation
- Without it, controller input does nothing
- Automatically set for you!

### Select On Up/Down/Left/Right

**What they do**: Define what button to select when pressing directional inputs.

**How they're set**:
- SlotGridNavigator: Calculates based on grid position
- MenuNavigationConfigurator: Links tabs to content and vice versa
- DrillDownNavigator: Temporarily disables parent navigation

**Example**: 
```
Button at grid position (0, 0):
- Select On Right → Button at (1, 0)
- Select On Down → Button at (0, 1)
- Select On Up → Tab button (external)
- Select On Left → null (edge of grid)
```

---

## Common Issues

### "Controller input doesn't work"

**Cause**: EventSystem has no selected GameObject
**Solution**: 
- Check MenuNavigationConfigurator.autoSelectFirstTab = true
- Verify Initial Tab Index is valid (0-7)
- Check console for "MenuNavigationConfigurator: Configured X tabs" message

### "Can't navigate to some buttons"

**Cause**: Navigation chain broken (Select On Up/Down/Left/Right not set)
**Solution**:
- Ensure MenuNavigationConfigurator.autoConfigureOnStart = true
- For grids, check SlotGridNavigator columns/rows match actual layout
- Check console for navigation configuration logs

### "Tabs don't switch with bumpers"

**Cause**: TabGroup not receiving input events
**Solution**:
- Check InputManager is enabled and in scene
- Verify InputManager.onTabLeft and onTabRight are working
- Check TabGroup.tabActive = true
- Ensure TabGroup has reference to all TabButtons

### "ScrollMenu doesn't scroll"

**Causes**: 
1. ScrollMenu GameObject is inactive (cooldown timer needs Update())
2. Not subscribed to scroll input
3. ScrollMenu not activated

**Solutions**:
1. Ensure GameObject with ScrollMenu is active
2. Implement IScrollMenuAuthority and call SubscribeToScroll()
3. Call scrollMenu.Activate() with data before using
4. Check InputManager.onScroll is hooked up

### "DrillDownNavigator doesn't drill"

**Cause**: No first child selectable found
**Solution**:
- Check Auto Find First Child = true
- Verify children have Button/Selectable components
- Manually assign First Child Selectable if needed
- Check children are active and interactable

### "Render texture doesn't show"

**Causes**:
1. Camera disabled or culling mask wrong
2. RenderTexture not assigned to camera
3. RawImage not assigned to SharedRenderTextureController

**Solutions**:
1. Check camera is enabled and rendering correct layers
2. Assign RenderTexture to camera's Target Texture
3. Assign RawImage displaying render texture
4. Verify tab configuration IsActive = true for that tab

### "Animations don't play"

**Cause**: Time.timeScale = 0 but not using unscaled time
**Solution**: All PrimeTween calls already use `useUnscaledTime: true` - if you add new animations, ensure this parameter is set!

### "Audio doesn't play"

**Causes**:
1. EventBus not registered in AudioSystem
2. FMOD events not assigned
3. AudioSystem not in scene

**Solutions**:
1. Check AudioSystem has EventBinding<PlayUIAudioEvent> registered
2. Assign FMOD EventReferences in AudioSystem Inspector
3. Ensure AudioSystem persists across scenes or is in menu scene

### "Navigation goes to wrong element"

**Cause**: Multiple navigation systems competing (auto + explicit)
**Solution**:
- Ensure ALL buttons use Navigation.Mode.Explicit
- Never mix Automatic with Explicit
- Run MenuNavigationConfigurator.ConfigureAllNavigation() to reset

### "TabButton visual state incorrect"

**Cause**: Tab button interactable set to true (should be false)
**Solution**: TabButton.Awake() sets button.interactable = false - tabs are visual only, bumpers control switching

---

## Testing Checklist

After setup, test these interactions:

### Tab Navigation
- [ ] LB switches to previous tab (wraps from first to last)
- [ ] RB switches to next tab (wraps from last to first)
- [ ] Selected tab scales up and changes color
- [ ] Deselected tab scales down and returns to normal color
- [ ] Tab switch plays audio

### Content Navigation
- [ ] D-Pad/Stick moves between buttons in tab
- [ ] Navigation wraps correctly at edges (if configured)
- [ ] Button highlights when selected
- [ ] First element auto-selected when tab opens
- [ ] Navigation plays audio

### Actions
- [ ] A button selects/confirms (OnSubmit)
- [ ] B button cancels/backs out (OnCancel)
- [ ] Actions play audio
- [ ] Actions trigger expected behavior

### ScrollMenu
- [ ] Up/Down scrolls through items
- [ ] Center panel scales up
- [ ] Scrolling is smooth and responsive
- [ ] Can't scroll too fast (cooldown working)
- [ ] Scroll plays audio

### DrillDownNavigator
- [ ] Container shows selection indicator when selected
- [ ] A drills into children
- [ ] Can navigate children with D-Pad
- [ ] B returns to parent
- [ ] Plays audio on drill/undrill

### Render Texture
- [ ] Visible in correct tabs
- [ ] Smoothly animates to new position when switching tabs
- [ ] Correct scale in each tab
- [ ] Hidden in tabs where IsActive = false

### Audio
- [ ] All button navigation plays sound
- [ ] All selections play sound
- [ ] Tab switches play unique sound
- [ ] No audio errors in console

---

## Best Practices

### When Adding New Tabs
1. Create tab content panel (GameObject)
2. Add appropriate Tab component (extends TabSelection)
3. Add CanvasGroup (alpha 0, non-interactable)
4. Create TabButton for it
5. Assign content panel reference to TabButton
6. MenuNavigationConfigurator handles the rest!

### When Adding New Components
1. Use ISelectHandler/IDeselectHandler for selection feedback
2. Use ISubmitHandler for A button, ICancelHandler for B button
3. Call UIAudio methods for feedback
4. Use PrimeTween with useUnscaledTime: true for animations
5. Expose settings in Inspector with [SerializeField] and [Tooltip]

### When Debugging Navigation
1. Check console for MenuNavigationConfigurator logs
2. Use Unity's Navigation Visualizer (Window > Analysis > Navigation)
3. Add Debug.Log in OnSelect/OnDeselect to track selection
4. Verify EventSystem.current.currentSelectedGameObject

### Performance Tips
- Use SharedRenderTextureController for shared render textures (1 camera instead of 8)
- Use ScrollMenu panel recycling (5 panels for infinite items)
- Disable inactive tab content (CanvasGroup.interactable = false)
- Use object pooling for dynamically created UI elements

---

## Summary

This menu system is designed for **minimal setup, maximum automation**:

1. Set up basic hierarchy (canvas, tabs, content)
2. Assign content panels to tab buttons
3. Add MenuNavigationConfigurator
4. Let the system auto-configure everything else!

All navigation, audio, and animations are handled automatically. Just assign data providers, configure grid dimensions, and you're ready to go!

