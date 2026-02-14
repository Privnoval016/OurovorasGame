# Menu UI System - Unity Editor Setup Guide

## Important Philosophy

**This system is designed for 100% controller/keyboard navigation - NO MOUSE REQUIRED.**

Most setup is handled **automatically in code** to minimize manual configuration errors:
- ✅ Button listeners added at runtime
- ✅ Navigation chains configured programmatically
- ✅ First selected elements handled automatically
- ✅ Grid navigation calculated by code

You only need to:
1. Create the GameObject hierarchy
2. Assign tab content panels
3. Set grid dimensions (columns/rows)
4. Let the system do the rest

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Initial Setup](#initial-setup)
3. [GameObject Hierarchy](#gameobject-hierarchy)
4. [Tab Configuration](#tab-configuration)
5. [Audio Integration](#audio-integration)
6. [Controller Navigation Setup](#controller-navigation-setup-automated)
7. [Render Texture Setup](#render-texture-setup)
8. [ScrollMenu Configuration](#scrollmenu-configuration)
9. [Data Provider Setup](#data-provider-setup)
10. [Parameter Reference](#parameter-reference)
11. [Testing Checklist](#testing-checklist)

---

## Prerequisites

Ensure you have these packages installed:
- ✅ Unity Input System
- ✅ TextMeshPro
- ✅ FMOD Unity Integration
- ✅ PrimeTween (https://github.com/KyryloKuzyk/PrimeTween)

---

## Initial Setup

### 1. Create Main Menu GameObject

1. In your scene, create an empty GameObject: **`OverworldMenuUI`**
2. Set as child of your Canvas
3. Add component: **`OverworldMenuUI.cs`**
4. Add component: **`MenuNavigationConfigurator.cs`** (handles all navigation automatically)
5. Set RectTransform to fill the canvas

### 2. Create EventSystem

**CRITICAL STEP - This is what makes controller navigation work!**

If you don't have one:
1. GameObject → UI → Event System
2. **Remove** the default **Standalone Input Module** component
3. Add **`UIInputConfigurator`** component to the EventSystem GameObject
   - This will automatically add and configure `InputSystemUIInputModule`
   - This bridges Unity Input System to EventSystem navigation
   - **Without this, D-Pad/A/B buttons won't work!**

The UIInputConfigurator does the following automatically:
- Removes old input modules (StandaloneInputModule, etc.)
- Adds InputSystemUIInputModule if not present
- Connects Menu/Navigate to EventSystem move
- Connects Menu/Select to EventSystem submit (A button)
- Connects Menu/Deselect to EventSystem cancel (B button)
- Enables the Menu action map

**Verification**: 
- Check console for "UIInputConfigurator: ✅ Configured InputSystemUIInputModule" message
- EventSystem should have both EventSystem and InputSystemUIInputModule components
- No StandaloneInputModule should be present

---

## GameObject Hierarchy

Create this hierarchy under `OverworldMenuUI`:

```
OverworldMenuUI (OverworldMenuUI.cs)
├── EventSystem (reference)
│
├── TabGroup (TabGroup.cs)
│   ├── TabButton1_Character (TabButton.cs + Button)
│   ├── TabButton2_Equipment (TabButton.cs + Button)
│   ├── TabButton3_Elements (TabButton.cs + Button)
│   ├── TabButton4_SkillTree (TabButton.cs + Button)
│   ├── TabButton5_Inventory (TabButton.cs + Button)
│   ├── TabButton6_Missions (TabButton.cs + Button)
│   ├── TabButton7_Compendium (TabButton.cs + Button)
│   └── TabButton8_Settings (TabButton.cs + Button)
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
├── DataProviders
│   ├── PlayerDataProvider (PlayerDataProvider.cs)
│   ├── EquipmentDataProvider (EquipmentDataProvider.cs)
│   ├── ElementProgressDataProvider (ElementProgressDataProvider.cs)
│   ├── SkillTreeDataProvider (SkillTreeDataProvider.cs)
│   ├── InventoryDataProvider (InventoryDataProvider.cs)
│   ├── QuestDataProvider (QuestDataProvider.cs)
│   └── CompendiumDataProvider (CompendiumDataProvider.cs)
│
└── SharedRenderTexture (SharedRenderTextureController.cs, RawImage)
    └── RenderTexturePositions
        ├── Tab1_CharacterPos (Empty RectTransform)
        ├── Tab2_EquipmentPos (Empty RectTransform)
        └── ... (position markers for each tab that needs the render texture)
```

---

## Tab Configuration

### For Each Tab Button (Minimal Manual Setup):

1. **Add Components** (automatically handled):
   - `TabButton.cs` - Add this component
   - `Button` (Unity UI) - Added automatically by TabButton
   - `Image` - For visual feedback

2. **Configure TabButton** (only 2 assignments required):
   - **Content Panel**: Drag corresponding tab content (e.g., Tab1_Character) - **REQUIRED**
   - **Tab Group**: Auto-found from parent, but can assign manually

3. **Navigation** (automatically configured by MenuNavigationConfigurator):
   - ✅ Button click listeners added in code
   - ✅ Horizontal navigation (left/right) configured in code
   - ✅ Down navigation to tab content configured in code
   - ✅ Visual transitions (colors) set in code

4. **Add Text Label**:
   - Child GameObject with `TextMeshProUGUI`
   - Set text: "Character", "Equipment", etc.

### For Each Tab Content:

1. **Add Base Components**:
   - Corresponding tab script (e.g., `CharacterStatsTab.cs`)
   - `CanvasGroup` (added automatically by TabSelection)

2. **Configure Tab Script**:
   - **Data Provider Object**: Drag corresponding data provider - **REQUIRED**
   - All other UI element references specific to that tab

### For Slot Grids (Equipment, Inventory, etc.):

1. **Add SlotGridNavigator** to the parent container:
   - `SlotGridNavigator.cs` component on parent of ItemSlotUI children
   
2. **Configure Grid Dimensions**:
   - **Columns**: Number of columns in grid (e.g., 3 for a 3x2 grid)
   - **Rows**: Number of rows in grid (e.g., 2 for a 3x2 grid)
   - **Select On Up From Top**: Tab button or other element above grid (optional)
   - **Select On Down From Bottom**: Element below grid (optional)
   
3. **That's it!** Navigation is configured automatically in code

### MenuNavigationConfigurator Parameters:

Configured on `OverworldMenuUI` GameObject:

- **Auto Configure On Start** (default: true)
  - When enabled, all navigation is configured automatically when the scene starts
  - Disable only if you need manual control over when configuration happens
  
- **Auto Select First Tab** (default: true)
  - When enabled, automatically selects and focuses the first tab when menu opens
  - Required for controller navigation to work immediately
  
- **Initial Tab Index** (default: 0)
  - Which tab to select when menu opens (0 = first tab, 1 = second tab, etc.)
  - Usually keep at 0 to start on the Character Stats tab

---

## Tab-Specific Setup

### Tab 1: Character Stats

**UI Structure**:
```
Tab1_Character
├── StatsPanel (Left side)
│   └── PlayerStatsDisplay (PlayerStatsDisplay.cs)
│       ├── LevelText (TMP)
│       ├── ExperienceBar (Slider)
│       ├── HealthBar (Slider)
│       ├── HealthText (TMP)
│       ├── ChargeBar (Slider)
│       ├── ChargeText (TMP)
│       ├── StrengthText (TMP)
│       └── DefenseText (TMP)
├── EquippedItemsPanel (Bottom left)
│   ├── Slot1 (ItemSlotUI.cs + Button)
│   ├── Slot2 (ItemSlotUI.cs + Button)
│   └── ... (6 slots total: 3 accessories + 3 passives)
└── CharacterModelDisplay (RawImage - shared)
```

**CharacterStatsTab Configuration**:
- Assign PlayerDataProvider
- Assign PlayerStatsDisplay
- Drag all 6 ItemSlotUI into `equippedItemSlots` array
- Set `autoRefresh` to true
- Set `refreshInterval` to 0.5

### Tab 2: Equipment Selection

**UI Structure**:
```
Tab2_Equipment
├── SlotsPanel (Left side)
│   ├── AccessorySlots
│   │   ├── Slot1 (ItemSlotUI.cs + Button)
│   │   ├── Slot2 (ItemSlotUI.cs + Button)
│   │   └── Slot3 (ItemSlotUI.cs + Button)
│   └── PassiveSlots
│       ├── Slot1 (ItemSlotUI.cs + Button)
│       ├── Slot2 (ItemSlotUI.cs + Button)
│       └── Slot3 (ItemSlotUI.cs + Button)
├── ScrollMenuContainer (Center)
│   └── EquipmentScrollMenu (ScrollMenu.cs)
│       └── Panels (5-7 ItemScrollPanel prefabs)
├── InfoPanel (Center/Right)
│   ├── CurrentItem
│   │   ├── NameText (TMP)
│   │   ├── DescriptionText (TMP)
│   │   └── Icon (Image)
│   └── SelectedItem
│       ├── NameText (TMP)
│       ├── DescriptionText (TMP)
│       └── Icon (Image)
└── CharacterModelDisplay (RawImage - shared)
```

**EquipmentMenuUI Configuration**:
- Assign EquipmentDataProvider
- Drag all accessory slots into `accessorySlots` array
- Drag all passive slots into `passiveSlots` array
- Assign `equipmentScrollMenu`
- Assign `scrollMenuContainer` GameObject
- **CRITICAL**: Add `SelectableContainer.cs` component to `scrollMenuContainer`
  - This makes the scroll menu receive B button (Cancel) input to close
  - Without this, you can't exit the scroll menu with B button!
- Assign info panel UI references
- Assign `defaultButton`

**ItemSlotUI Setup**:
- Each slot automatically calls `OnAccessorySlotSelected(int)` or `OnPassiveSlotSelected(int)` on Submit (A button)
- Slots are automatically disabled when scroll menu opens (can't navigate back to them)
- Slots are automatically re-enabled when scroll menu closes
- Highlights automatically clear when switching tabs

### Tab 3: Element Progress

**UI Structure**:
```
Tab3_ElementProgress
├── ElementButtonsPanel (Left)
│   ├── FireButton (Button)
│   ├── IceButton (Button)
│   ├── LightningButton (Button)
│   ├── EarthButton (Button)
│   └── WindButton (Button)
├── ProgressPanel (Top Center)
│   ├── ElementNameText (TMP)
│   ├── ProgressBar (Slider)
│   ├── ProgressText (TMP)
│   └── LevelDisplays (10x ProgressLevelDisplay.cs)
├── AttackAssignmentPanel (Bottom Center)
│   ├── AttackButton_X (AttackAssignmentButton.cs + Button)
│   ├── AttackButton_Y (AttackAssignmentButton.cs + Button)
│   └── AttackButton_A (AttackAssignmentButton.cs + Button)
├── ScrollMenuContainer (Right, overlaid)
│   └── AttackScrollMenu (ScrollMenu.cs)
│       └── Panels (5-7 ItemScrollPanel prefabs)
└── CharacterModelDisplay (RawImage - shared, behind panels)
```

**ElementProgressTab Configuration**:
- Assign ElementProgressDataProvider
- Drag element buttons into `elementButtons` array
- Assign progress display references
- Assign attack assignment buttons
- Assign `attackScrollMenu` and `scrollMenuContainer`
- Optional: Assign `attackVideoDisplay` for ability demos

### Tab 4: Skill Tree

**UI Structure**:
```
Tab4_SkillTree
├── SkillTreeContainer (RectTransform - scrollable)
│   └── Nodes (Instantiated SkillNodeUI prefabs)
├── SkillInfoPanel (Bottom)
│   ├── SkillNameText (TMP)
│   ├── SkillDescriptionText (TMP)
│   ├── SkillIcon (Image)
│   ├── UnlockButton (Button)
│   ├── UnlockCostText (TMP)
│   └── LockedIndicator (GameObject)
├── VideoDisplay (Optional, for ability demos)
└── CharacterModelDisplay (RawImage - shared, behind tree)
```

**SkillTreeTab Configuration**:
- Assign SkillTreeDataProvider
- Assign `skillTreeContainer` (parent for nodes)
- Assign `skillNodePrefab` (SkillNodeUI prefab)
- Assign skill info panel references
- Set `navigationSpeed` (e.g., 500)

**SkillNodeUI Prefab**:
- Create prefab with `SkillNodeUI.cs`
- Add UI: icon, background, locked overlay, selected indicator
- Use Event Trigger: `Pointer Click` → calls parent's `OnNodeClicked(int)`

### Tab 5: Inventory

**UI Structure**:
```
Tab5_Inventory
├── CategoryButtonsPanel (Left)
│   ├── AllButton (Button)
│   ├── AccessoriesButton (Button)
│   ├── ConsumablesButton (Button)
│   └── ... (category buttons)
├── ItemScrollMenuContainer (Center)
│   └── ItemScrollMenu (ScrollMenu.cs)
│       └── Panels (5-7 ItemScrollPanel prefabs)
├── ItemDetailsPanel (Right)
│   ├── ItemNameText (TMP)
│   ├── ItemDescriptionText (TMP)
│   ├── ItemIcon (Image)
│   └── ItemQuantityText (TMP)
└── CharacterModelDisplay (RawImage - shared)
```

**InventoryTab Configuration**:
- Assign InventoryDataProvider
- Drag category buttons into `categoryButtons` array
- Drag category button texts into `categoryButtonTexts` array
- Assign `itemScrollMenu` and `scrollMenuContainer`
- Assign item details panel references

### Tab 6: Missions

**UI Structure**:
```
Tab6_Missions
├── QuestTypePanel (Top)
│   ├── MainQuestsButton (Button)
│   └── SideQuestsButton (Button)
├── QuestScrollMenuContainer (Right)
│   └── QuestScrollMenu (ScrollMenu.cs)
│       └── Panels (5-7 ItemScrollPanel prefabs)
├── QuestDetailsPanel (Left)
│   ├── QuestNameText (TMP)
│   ├── QuestDescriptionText (TMP)
│   ├── QuestProgressBar (Slider)
│   ├── QuestProgressText (TMP)
│   └── CompletedIndicator (GameObject)
└── CharacterModelDisplay (RawImage - shared, behind details)
```

**MissionsTab Configuration**:
- Assign QuestDataProvider
- Assign quest type buttons
- Assign `questScrollMenu` and `scrollMenuContainer`
- Assign quest details panel references

### Tab 7: Compendium

**UI Structure**:
```
Tab7_Compendium
├── CategoryButtonsPanel (Left)
│   ├── AllButton (Button)
│   ├── LoreButton (Button)
│   ├── EnemiesButton (Button)
│   └── ... (category buttons)
├── EntryScrollMenuContainer (Center)
│   └── EntryScrollMenu (ScrollMenu.cs)
│       └── Panels (5-7 ItemScrollPanel prefabs)
└── EntryDetailsPanel (Right)
    ├── EntryNameText (TMP)
    ├── EntryDescriptionText (TMP)
    ├── EntryIcon (Image)
    └── UndiscoveredIndicator (GameObject)
```

**CompendiumTab Configuration**:
- Assign CompendiumDataProvider
- Drag category buttons and texts
- Assign `entryScrollMenu` and container
- Assign entry details panel references

### Tab 8: Settings

**UI Structure**:
```
Tab8_Settings
├── AudioSettings
│   ├── MasterVolumeSlider (Slider)
│   ├── MasterVolumeText (TMP)
│   ├── MusicVolumeSlider (Slider)
│   ├── MusicVolumeText (TMP)
│   ├── SFXVolumeSlider (Slider)
│   └── SFXVolumeText (TMP)
├── VideoSettings
│   ├── ResolutionDropdown (TMP_Dropdown)
│   ├── QualityDropdown (TMP_Dropdown)
│   ├── FullscreenToggle (Toggle)
│   ├── VSyncToggle (Toggle)
│   └── BrightnessSlider (Slider)
├── GameplaySettings
│   ├── CameraSensitivitySlider (Slider)
│   ├── CameraSensitivityText (TMP)
│   └── InvertYAxisToggle (Toggle)
└── SaveLoadButtons
    ├── SaveSettingsButton (Button)
    ├── LoadSettingsButton (Button)
    ├── ResetToDefaultButton (Button)
    └── SaveStatusText (TMP)
```

**SettingsTab Configuration**:
- Assign all slider, dropdown, toggle references
- Assign save/load button references
- No data provider needed (uses PlayerPrefs and FMOD buses)

---

## Audio Integration

### 1. Setup AudioSystemUIIntegration

**Option A: Add to Existing AudioSystem**:
```csharp
// In your AudioSystem.cs, add:
[SerializeField] private EventReference uiNavigationEvent;
[SerializeField] private EventReference uiSelectEvent;
[SerializeField] private EventReference uiBackEvent;
[SerializeField] private EventReference uiTabSwitchEvent;
[SerializeField] private EventReference uiErrorEvent;
[SerializeField] private EventReference uiHoverEvent;
[SerializeField] private EventReference uiUnlockEvent;

private EventBinding<PlayUIAudioEvent> uiAudioBinding;

private void Awake()
{
    // ... existing code ...
    
    uiAudioBinding = new EventBinding<PlayUIAudioEvent>(OnPlayUIAudio);
    EventBus<PlayUIAudioEvent>.Register(uiAudioBinding);
}

private void OnDestroy()
{
    if (uiAudioBinding != null)
        EventBus<PlayUIAudioEvent>.Deregister(uiAudioBinding);
}

private void OnPlayUIAudio(PlayUIAudioEvent evt)
{
    // See AudioSystemUIIntegration.cs for implementation
}
```

**Option B: Use Separate Component**:
1. Add `AudioSystemUIIntegration.cs` component to your AudioSystem GameObject
2. Assign FMOD event references in Inspector

### 2. Create FMOD Events

In FMOD Studio, create these events:
- `event:/UI/Navigation` - Short blip (50-100ms)
- `event:/UI/Select` - Satisfying click (100-150ms)
- `event:/UI/Back` - Softer sound (80-120ms)
- `event:/UI/TabSwitch` - Distinct whoosh (150-200ms)
- `event:/UI/Error` - Harsh/dissonant (100-150ms)
- `event:/UI/Hover` - Very subtle (30-50ms)
- `event:/UI/Unlock` - Rewarding chime (200-300ms)

**Tips**:
- Keep UI sounds short
- Navigation should be quiet
- Select should feel satisfying
- Error should be noticeably different
- Consider parameter variations

### 3. Verify FMOD Bus Structure

Ensure these buses exist:
```
bus:/          (Master)
├── bus:/Music (Music)
├── bus:/SFX   (Sound Effects)
└── bus:/UI    (UI Sounds)
```

---

## Controller Navigation Setup

### 1. Configure Input Actions

In your UI Input Actions asset, ensure these exist:

**Navigate (Vector2)**:
- Binding: D-Pad (Gamepad)
- Binding: Left Stick (Gamepad)
- Binding: WASD (Keyboard)
- Processors: Stick Deadzone (0.2)

**Scroll (Vector2)**:
- Binding: Right Stick (Gamepad)
- Processors: Stick Deadzone (0.2)

**Submit (Button)**:
- Binding: A Button (Gamepad)
- Binding: Enter (Keyboard)

**Cancel (Button)**:
- Binding: B Button (Gamepad)
- Binding: Escape (Keyboard)

**TabLeft (Button)**:
- Binding: LB (Gamepad)
- Binding: Q (Keyboard)

**TabRight (Button)**:
- Binding: RB (Gamepad)
- Binding: E (Keyboard)

### 2. Configure Button Navigation

For each Button/Selectable:

**Tab Buttons** (Horizontal navigation):
```
Navigation: Explicit
- Select On Left: Previous tab button
- Select On Right: Next tab button
- Select On Down: First element in tab content
- Select On Up: (none)
```

**Item Slots** (Grid navigation):
```
Navigation: Explicit
- Select On Left: Slot to the left
- Select On Right: Slot to the right
- Select On Up: Slot above
- Select On Down: Slot below
```

**Category Buttons** (Vertical navigation):
```
Navigation: Vertical (Automatic works well)
```

### 3. Set First Selected Elements

For each tab, in the tab script:
```csharp
[SerializeField] private Button defaultButton;

public override void OnTabSelect()
{
    base.OnTabSelect();
    
    if (defaultButton != null && EventSystem.current != null)
    {
        EventSystem.current.SetSelectedGameObject(defaultButton.gameObject);
    }
}
```

---

## Render Texture Setup

### Shared Render Texture (Recommended)

1. **Create Render Texture Asset**:
   - Assets → Create → Render Texture
   - Name: `CharacterModelRT`
   - Size: 1024x1024 (or 512x512 for performance)

2. **Create Camera**:
   - GameObject → Camera
   - Name: `CharacterModelCamera`
   - Settings:
     - Target Texture: CharacterModelRT
     - Culling Mask: Create layer "UI3D" and set to that
     - Clear Flags: Solid Color
     - Background: Transparent
     - Depth: 1 (above main camera)
   - Initially disable the camera

3. **Setup Character Model**:
   - Duplicate your player character model
   - Position in front of camera
   - Set layer to "UI3D"
   - Add to camera as child (or separate object)

4. **Configure SharedRenderTextureController**:
   - On the SharedRenderTexture GameObject:
     - Add `RawImage` component
     - Add `SharedRenderTextureController.cs` component
   - Assign:
     - Render Camera: CharacterModelCamera
     - Render Texture: CharacterModelRT
     - Target Image: RawImage on same object
     - Culling Mask: UI3D layer
   - Animation Settings:
     - Transition Duration: 0.4
     - Transition Ease: OutCubic
     - Scale Overshoot: 1.05

5. **Create Position Markers**:
   - Under `RenderTexturePositions`, create empty GameObjects:
     - `Tab1_CharacterPos`
     - `Tab2_EquipmentPos`
     - `Tab3_ElementPos`
     - etc.
   - Position each RectTransform where you want the render texture in that tab
   - Drag all into SharedRenderTextureController's `tabPositions` array **in order**

6. **Call from Tabs**:
```csharp
[SerializeField] private SharedRenderTextureController sharedRenderTexture;

public override void OnTabSelect()
{
    base.OnTabSelect();
    
    // Move render texture to this tab's position
    sharedRenderTexture.MoveToTab(0); // Your tab index
}
```

### Per-Tab Render Texture (Alternative)

If you prefer separate render textures per tab:

1. Create separate RenderTexture assets for each tab
2. Create separate cameras for each
3. Use `RenderTextureDisplay.cs` component on each tab
4. Enable/disable cameras in OnTabSelect/OnTabDeselect

---

## ScrollMenu Configuration

### 1. Create Panel Prefab

**ItemScrollPanel Prefab**:
1. Create GameObject: `ItemScrollPanel`
2. Add component: `ItemScrollPanel.cs` (or your custom panel)
3. Add RectTransform with desired size (e.g., 200x200)
4. Add child UI elements:
   - Icon (Image)
   - NameText (TextMeshProUGUI)
   - DescriptionText (TextMeshProUGUI)
   - AmountText (TextMeshProUGUI)
   - BorderImage (Image)
   - EmptyIndicator (GameObject)
5. Assign all references in ItemScrollPanel component
6. Save as prefab

### 2. Configure ScrollMenu

1. **On ScrollMenu GameObject**:
   - Add `ScrollMenu.cs` component
   - Create child: `ScrollItemContainer` (RectTransform)

2. **Instantiate Panels**:
   - Drag ItemScrollPanel prefab as child 5-7 times
   - Arrange with Vertical or Horizontal Layout Group
   - Or manually position them

3. **ScrollMenu Settings**:
   - **Axis**: Vertical (for up/down) or Horizontal (for left/right)
   - **Cycle Mode**: CircularStop (recommended)
   - **Focus Center Panel**: true
   - **Center Panel Index**: 2 (middle of 5 panels)
   - **Scroll Cooldown**: 0.11
   - **Scroll Duration**: 0.1
   - **Scroll Ease**: OutQuad
   - **Panel Spacing**: Distance between panels (e.g., 220)

4. **Assign Panels**:
   - Drag all panel instances into `panelQueue` array
   - Or leave empty and it will auto-find children

### 3. Connect to Tab

In your tab script:
```csharp
[SerializeField] private ScrollMenu scrollMenu;

public void OnSlotSelected(int slotIndex)
{
    var items = dataProvider.GetItems();
    
    scrollMenu.Activate(
        items: items,
        initialIndex: 0,
        getInfoFunc: item => item, // If already ItemUIInfo
        authority: this // Tab implements IScrollMenuAuthority
    );
}
```

### 4. Implement IScrollMenuAuthority

```csharp
public class MyTab : TabSelection, IScrollMenuAuthority
{
    private void ScrollDelegate(InputAction.CallbackContext context)
    {
        if (!isScrollMenuActive) return;
        scrollMenu.OnScrollPerformed(context.ReadValue<Vector2>());
    }
    
    public void SubscribeToScroll(ScrollMenu menu)
    {
        InputManager.Instance.onScroll += ScrollDelegate;
    }
    
    public void UnsubscribeFromScroll(ScrollMenu menu)
    {
        InputManager.Instance.onScroll -= ScrollDelegate;
    }
}
```

---

## Data Provider Setup

### Fix Remaining Data Providers

Four data providers still need the IService removal:

**For each of these files**:
- `SkillTreeDataProvider.cs`
- `InventoryDataProvider.cs`
- `QuestDataProvider.cs`
- `CompendiumDataProvider.cs`

**Remove**:
```csharp
public class XxxDataProvider : MonoBehaviour, IXxxDataProvider, IService
{
    private void Awake()
    {
        Services.Register<XxxDataProvider>(this);
    }
    // ...
}
```

**Replace with**:
```csharp
public class XxxDataProvider : MonoBehaviour, IXxxDataProvider
{
    private void Start()
    {
        try
        {
            var playerController = Services.Get<PlayerController>();
            // Use playerController to get data
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"XxxDataProvider: Could not get PlayerController: {e.Message}");
        }
    }
    // ...
}
```

### Connect Data Providers

On `OverworldMenuUI` GameObject:
1. Expand `DataProviders` section in Inspector
2. Drag each data provider GameObject to corresponding field:
   - Player Data Provider → PlayerDataProvider GameObject
   - Equipment Data Provider → EquipmentDataProvider GameObject
   - etc.

The OverworldMenuUI will automatically call `SetDataProvider()` on each tab during `Awake()`.

---

## Parameter Reference

### MenuNavigationConfigurator Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Auto Configure On Start | bool | true | Automatically configures all navigation when scene starts. Disable only if you need manual control. |
| Auto Select First Tab | bool | true | Automatically selects the first tab when menu opens. Required for immediate controller input. |
| Initial Tab Index | int | 0 | Which tab to select on start (0-based). 0 = Character, 1 = Equipment, etc. |

### TabButton Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **References** ||||
| Tab Group | TabGroup | null | The TabGroup managing all tabs. Auto-found from parent if not assigned. |
| Content Panel | TabSelection | **Required** | The content panel this tab controls. Must be assigned manually. |
| **Animation Settings** ||||
| Select Duration | float | 0.2 | Duration of scale-up animation when tab is selected (seconds). |
| Deselect Duration | float | 0.15 | Duration of scale-down animation when tab is deselected (seconds). |
| Selected Scale | float | 1.1 | Target scale when selected (1.1 = 10% larger). |
| Select Ease | Ease | OutBack | Easing curve for select animation. OutBack adds overshoot for snappy feel. |
| Deselect Ease | Ease | OutQuad | Easing curve for deselect animation. OutQuad is smooth and quick. |
| **Visual Settings** ||||
| Normal Color | Color | White | Color when tab is not selected. |
| Highlighted Color | Color | #C8C8C8 | Color when controller is hovering over tab (not selected yet). |
| Selected Color | Color | Yellow | Color when this is the active/selected tab. |
| Pressed Color | Color | #808080 | Color when tab is being pressed (A button down). |

### TabSelection Parameters (Base Class for All Tabs)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Animation Settings** ||||
| Fade In Duration | float | 0.25 | Duration of fade-in animation when tab is selected (seconds). |
| Fade Out Duration | float | 0.15 | Duration of fade-out animation when tab is deselected (seconds). |
| Fade In Ease | Ease | OutQuad | Easing curve for fade-in. OutQuad is smooth. |
| Fade Out Ease | Ease | InQuad | Easing curve for fade-out. InQuad is quick. |

### SlotGridNavigator Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Columns | int | 3 | Number of columns in the slot grid. For 3x2 grid, set to 3. |
| Rows | int | 2 | Number of rows in the slot grid. For 3x2 grid, set to 2. |
| Select On Up From Top | Selectable | null | Where to navigate when pressing up from top row (usually tab button). Optional. |
| Select On Down From Bottom | Selectable | null | Where to navigate when pressing down from bottom row. Optional. |
| Auto Configure On Start | bool | true | Automatically configure navigation on Start. Recommended to keep true. |

### ItemSlotUI Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **UI References** ||||
| Icon Image | Image | **Required** | Image component showing the item icon. |
| Background Image | Image | null | Image component for the slot background. Optional. |
| Border Image | Image | null | Image component for selection border/highlight. Optional but recommended. |
| Name Text | TMP_Text | null | Text field displaying item name. Optional. |
| Empty Indicator | GameObject | null | GameObject shown when slot is empty (e.g., "Empty Slot" text). Optional. |
| **Visual Settings** ||||
| Normal Border Color | Color | White | Border color when slot is not selected. |
| Selected Border Color | Color | Yellow | Border color when slot is selected (controller focus). |
| Empty Color | Color | Gray | Color tint when slot is empty. |
| **Events** ||||
| On Slot Selected | UnityEvent<int> | - | Called when slot is selected. Passes slot index as parameter. |
| On Slot Deselected | UnityEvent | - | Called when slot loses selection. |

### SharedRenderTextureController Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Render Texture Configuration** ||||
| Render Camera | Camera | **Required** | Camera that renders to the render texture. |
| Render Texture | RenderTexture | **Required** | The render texture asset. |
| Target Image | RawImage | **Required** | RawImage that displays the render texture. |
| Culling Mask | LayerMask | Everything | What layers the camera renders (usually "UI3D" layer). |
| **Animation Settings** ||||
| Transition Duration | float | 0.4 | Duration of movement animation between tabs (seconds). |
| Transition Ease | Ease | OutCubic | Easing curve for movement. OutCubic is smooth. |
| Scale Overshoot | float | 1.05 | Temporary scale increase during transition for snappy feel. |
| **Position Presets** ||||
| Tab Positions | RectTransform[] | **Required** | Array of position markers (one per tab). Must be in tab order. |

### ScrollMenu Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Scroll Settings** ||||
| Axis | ScrollAxis | Vertical | Direction of scrolling (Vertical = up/down, Horizontal = left/right). |
| Cycle Mode | CycleMode | CircularStop | How scrolling wraps: CircularStop (stops if few items), CircularPure (always wraps), Restart (jumps), Stop (hard stop). |
| Focus Center Panel | bool | true | Whether to scale up and highlight the center panel. Recommended true. |
| Center Panel Index | int | 2 | Which panel is the center (usually 2 for 5 panels). |
| Scroll Cooldown | float | 0.11 | Minimum time between scroll inputs (prevents spam). |
| Scroll Duration | float | 0.1 | Duration of scroll animation (seconds). |
| Scroll Ease | Ease | OutQuad | Easing curve for scroll animation. |
| Panel Spacing | float | 220 | Distance between panels in pixels. |

### CharacterStatsTab Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Player Data Provider Object | MonoBehaviour | **Required** | PlayerDataProvider component. Must implement IPlayerDataProvider. |
| Player Stats Display | PlayerStatsDisplay | **Required** | Component that displays player stats. |
| Equipped Item Slots | ItemSlotUI[] | **Required** | Array of 6 ItemSlotUI (3 accessories + 3 passives). |
| Auto Refresh | bool | true | Automatically refresh stats periodically. Recommended true for live updates. |
| Refresh Interval | float | 0.5 | How often to refresh stats (seconds). Balance between responsiveness and performance. |

### EquipmentMenuUI Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Equipment Data Provider Object | MonoBehaviour | **Required** | EquipmentDataProvider component. |
| Accessory Slots | ItemSlotUI[] | **Required** | Array of 3 accessory slots. |
| Passive Slots | ItemSlotUI[] | **Required** | Array of 3 passive skill slots. |
| Equipment Scroll Menu | ScrollMenu | **Required** | ScrollMenu for selecting equipment. |
| Scroll Menu Container | GameObject | **Required** | Parent GameObject of scroll menu (for show/hide). |
| Default Button | Button | **Required** | First button to select when tab opens (usually first accessory slot). |
| Current Item Display | UI References | **Required** | UI elements showing currently equipped item. |
| Selected Item Display | UI References | **Required** | UI elements showing item being previewed in scroll menu. |

### SettingsTab Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Audio Settings** ||||
| Master Volume Slider | Slider | **Required** | Slider controlling FMOD Master bus volume. |
| Master Volume Text | TMP_Text | **Required** | Text displaying Master volume percentage. |
| Music Volume Slider | Slider | **Required** | Slider controlling FMOD Music bus volume. |
| Music Volume Text | TMP_Text | **Required** | Text displaying Music volume percentage. |
| SFX Volume Slider | Slider | **Required** | Slider controlling FMOD SFX bus volume. |
| SFX Volume Text | TMP_Text | **Required** | Text displaying SFX volume percentage. |
| **Video Settings** ||||
| Resolution Dropdown | TMP_Dropdown | **Required** | Dropdown for screen resolution selection. |
| Quality Dropdown | TMP_Dropdown | **Required** | Dropdown for graphics quality presets. |
| Fullscreen Toggle | Toggle | **Required** | Toggle for fullscreen mode. |
| VSync Toggle | Toggle | **Required** | Toggle for vertical sync. |
| Brightness Slider | Slider | null | Slider for brightness adjustment. Optional. |
| **Gameplay Settings** ||||
| Camera Sensitivity Slider | Slider | null | Slider for camera sensitivity. Optional. |
| Camera Sensitivity Text | TMP_Text | null | Text displaying sensitivity value. Optional. |
| Invert Y Axis Toggle | Toggle | null | Toggle for inverted Y-axis camera. Optional. |
| **Save/Load** ||||
| Save Settings Button | Button | **Required** | Button to save settings to PlayerPrefs. |
| Load Settings Button | Button | **Required** | Button to load settings from PlayerPrefs. |
| Reset To Default Button | Button | **Required** | Button to reset all settings to defaults. |
| Save Status Text | TMP_Text | **Required** | Text showing "Settings Saved!" feedback with animation. |

### Common Parameters Across All Tabs

| Parameter | Description |
|-----------|-------------|
| **Data Provider Object** | MonoBehaviour implementing the data provider interface for that tab. Must be assigned. Connection between UI and game backend. |
| **Default Button** | First Selectable to focus when tab opens. Auto-selected by EventSystem. Required for controller navigation. |
| **Render Texture Display** | SharedRenderTextureController or RenderTextureDisplay for 3D character model preview. Optional but recommended for visual polish. |

---

## Testing Checklist

### Basic Functionality
- [ ] Menu opens/closes correctly
- [ ] All 8 tabs can be switched between
- [ ] LB/RB switches tabs (controller)
- [ ] Q/E switches tabs (keyboard)

### Tab Content
- [ ] Tab 1: Player stats display correctly
- [ ] Tab 2: Equipment slots show equipped items
- [ ] Tab 3: Element buttons switch element displays
- [ ] Tab 4: Skill tree is navigable
- [ ] Tab 5: Inventory items show up
- [ ] Tab 6: Quests display in scrollmenu
- [ ] Tab 7: Compendium entries show
- [ ] Tab 8: Settings save and load

### Controller Navigation
- [ ] D-Pad/Left Stick navigates between buttons
- [ ] Right Stick scrolls through items in scroll menus
- [ ] A button selects/confirms
- [ ] B button cancels/backs out
- [ ] Visual feedback shows selected element (yellow highlight)
- [ ] First element auto-selected when tab opens

### Audio
- [ ] Tab switching plays sound (LB/RB)
- [ ] Button hover plays sound (controller moving over button)
- [ ] Button select plays sound (A button)
- [ ] Scroll menu navigation plays sound
- [ ] Settings volume sliders control FMOD buses
- [ ] All sounds play through AudioSystem (check EventBus)

### Animations
- [ ] Tabs fade in smoothly when selected (0.25s)
- [ ] Tabs fade out quickly when deselected (0.15s)
- [ ] Tab buttons scale up when selected
- [ ] Item slots pulse when selected
- [ ] Render texture moves smoothly between tabs (if using shared)
- [ ] Save status animates in settings tab
- [ ] All animations work when `Time.timeScale = 0`

### Scroll Menus
- [ ] Right Stick scrolls through items
- [ ] Center item is highlighted
- [ ] Item details update when scrolling
- [ ] Panels recycle correctly (test with 50+ items)
- [ ] Scroll cooldown prevents spam

### Data Flow
- [ ] Player stats update in real-time
- [ ] Equipped items display correctly
- [ ] Equipment changes reflect in backend
- [ ] No errors in console when switching tabs

### Edge Cases
- [ ] Empty inventory/quest lists handle gracefully
- [ ] Invalid slot selections show error feedback
- [ ] Navigation doesn't get stuck in any menu
- [ ] Can exit any scroll menu with B button

---

## Common Issues & Solutions

### "Controller not responding" / "D-Pad/A/B buttons don't work"

**This is the #1 issue!** EventSystem needs to be properly configured.

**Checklist**:
1. ✅ EventSystem exists in scene (check Hierarchy)
2. ✅ EventSystem has `UIInputConfigurator` component
3. ✅ UIInputConfigurator has run (check console for "✅ Configured" message)
4. ✅ EventSystem has `InputSystemUIInputModule` component (added by UIInputConfigurator)
5. ✅ NO `StandaloneInputModule` on EventSystem (will conflict!)
6. ✅ Menu action map is enabled (InputManager does this)
7. ✅ Controller is detected: In Unity console, type `UnityEngine.InputSystem.Gamepad.current` - should show your controller

**Manual Fix** (if UIInputConfigurator doesn't work):
1. Select EventSystem GameObject
2. Remove Standalone Input Module
3. Add Component → Input System UI Input Module
4. Manually assign:
   - Move → Menu/Navigate
   - Submit → Menu/Select
   - Cancel → Menu/Deselect
5. Leave mouse actions null (we're controller-only)

**Still not working?**
- Check Input Actions asset has Menu action map
- Verify Navigate, Select, Deselect actions exist and have bindings
- Check Menu action map is enabled in InputManager
- Try pressing Play, then check EventSystem's InputSystemUIInputModule in Inspector to see if actions are assigned

### "Tab switching works but content navigation doesn't"

**This means**:
- ✅ InputManager events are working (TabLeft/TabRight are manual subscriptions)
- ❌ EventSystem is NOT configured properly

**Solution**: Follow the "Controller not responding" fix above. The EventSystem needs UIInputConfigurator.

### "Selection jumps around weirdly"
- ✅ Review Navigation settings on Buttons
- ✅ Use Explicit navigation for complex layouts
- ✅ Verify no circular references

### "Can't see render texture"
- ✅ Check camera is enabled when tab is active
- ✅ Verify render texture is assigned to RawImage
- ✅ Check culling mask matches model layer
- ✅ Ensure camera depth is > 0

### "Scroll menu doesn't work"
- ✅ Verify IScrollMenuAuthority is implemented
- ✅ Check input subscription in SubscribeToScroll()
- ✅ Ensure Right Stick is mapped in Input Actions
- ✅ Verify panels have ScrollUIPanel component

### "Can still navigate slots when scroll menu is open"
**Problem**: Slots remain interactable when scroll menu is active
**Solution**: 
- ✅ Ensure ActivateScrollMenu() calls DisableSlotNavigation()
- ✅ Ensure DeactivateScrollMenu() calls EnableSlotNavigation()
- ✅ ItemSlotUI should only invoke callback on Submit (A button), not Select (hover)

### "Can't close scroll menu with B button"
**Problem**: B button doesn't close scroll menu
**Solution**:
- ✅ Add `SelectableContainer.cs` component to scroll menu container GameObject
- ✅ Tab must implement ICancelHandler interface
- ✅ ActivateScrollMenu() must select the scroll menu container: `eventSystem.SetSelectedGameObject(scrollMenuContainer)`

### "Slot highlights don't clear when switching tabs"
**Problem**: Yellow highlight stays on slots after switching tabs
**Solution**:
- ✅ Tab's OnTabDeselect() must call ClearAllSlotSelections()
- ✅ Tab's OnTabDeselect() must clear EventSystem: `eventSystem.SetSelectedGameObject(null)`
- ✅ ItemSlotUI must have ForceDeselect() method that resets visual state

### "No audio playing"
- ✅ Check AudioSystemUIIntegration is listening to EventBus
- ✅ Verify FMOD events are assigned in Inspector
- ✅ Test FMOD events directly in FMOD Studio
- ✅ Check FMOD banks are loaded

### "Animations not working when menu open"
- ✅ Ensure all Tween calls use `useUnscaledTime: true`
- ✅ Verify PrimeTween package is installed correctly
- ✅ Check no errors in console blocking animations

### "Data not showing up"
- ✅ Verify data provider is assigned to tab
- ✅ Check data provider has reference to PlayerController
- ✅ Ensure backend data exists (e.g., items in inventory)
- ✅ Look for errors in data provider Start() method

---

## Performance Optimization

### Recommendations
1. **Render Textures**: Use 512x512 instead of 1024x1024 if performance is an issue
2. **Disable Cameras**: Cameras disabled when tabs not active (automatic)
3. **Limit Panels**: Use 5 panels for scroll menus instead of 7
4. **Reduce Refresh Rate**: Increase CharacterStatsTab `refreshInterval` to 1.0s if needed
5. **Object Pooling**: ScrollMenu already uses efficient panel recycling

---

## Final Steps

1. **Save Scene**
2. **Create Prefab** of OverworldMenuUI for reusability
3. **Test with Controller** connected
4. **Test with Keyboard** as fallback
5. **Profile** to ensure no performance issues
6. **Document** any custom modifications for your project

---

*Last Updated: February 9, 2026*
*This setup guide will be updated whenever changes are made to the menu UI system.*



