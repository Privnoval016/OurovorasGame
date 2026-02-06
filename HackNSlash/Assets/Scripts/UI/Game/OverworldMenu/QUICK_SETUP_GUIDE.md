# Quick Setup Guide for Menu UI

## Prerequisites
- Unity 2021.3 or later
- TextMeshPro package installed
- Input System package (already in project)
- Video Player package

## Step-by-Step Setup

### 1. Create Main Menu GameObject

1. Create empty GameObject: `OverworldMenuUI`
2. Add component: `OverworldMenuUI.cs`
3. Set as child of Canvas

### 2. Create Tab Group

1. Under OverworldMenuUI, create empty: `TabGroup`
2. Add component: `TabGroup.cs`
3. Create 8 child objects: `Tab1Button` through `Tab8Button`
4. On each button:
   - Add `TabButton.cs` component
   - Add `Button` component
   - Add child `Text (TMP)` for label

### 3. Create Tab Content Containers

Under OverworldMenuUI, create empty: `TabContents`

For each tab, create container with appropriate script:
```
TabContents/
├── Tab1_Character (add CharacterStatsTab.cs)
├── Tab2_Equipment (add EquipmentMenuUI.cs)
├── Tab3_ElementProgress (add ElementProgressTab.cs)
├── Tab4_SkillTree (add SkillTreeTab.cs)
├── Tab5_Inventory (add InventoryTab.cs)
├── Tab6_Missions (add MissionsTab.cs)
├── Tab7_Compendium (add CompendiumTab.cs)
└── Tab8_Settings (add SettingsTab.cs)
```

### 4. Link Tabs to Buttons

For each TabButton:
1. Set `tabGroup` reference to TabGroup
2. Set `contentPanel` reference to corresponding tab content

Example:
- Tab1Button → contentPanel = Tab1_Character

### 5. Create Data Providers

Under OverworldMenuUI, create empty: `DataProviders`

Add these components:
1. `PlayerDataProvider.cs`
2. `EquipmentDataProvider.cs`
3. `ElementProgressDataProvider.cs`
4. `SkillTreeDataProvider.cs`
5. `InventoryDataProvider.cs`
6. `QuestDataProvider.cs`
7. `CompendiumDataProvider.cs`

### 6. Link Data Providers to OverworldMenuUI

On OverworldMenuUI component:
- Drag each data provider to corresponding field
- Assign tabGroup reference
- Assign eventSystem reference

### 7. Configure Tab 1 (Character Stats)

Create UI hierarchy under Tab1_Character:
```
Tab1_Character/
├── StatsPanel/
│   ├── LevelText (TMP)
│   ├── ExperienceBar (Slider)
│   ├── HealthBar (Slider)
│   ├── ChargeBar (Slider)
│   ├── StrengthText (TMP)
│   └── DefenseText (TMP)
├── EquippedItemsPanel/
│   ├── Slot1 (add ItemSlotUI.cs)
│   ├── Slot2 (add ItemSlotUI.cs)
│   └── ... (6 slots total)
└── CharacterModelDisplay (add RenderTextureDisplay.cs)
```

On CharacterStatsTab:
1. Assign `PlayerDataProvider` to `playerDataProviderObject`
2. Create `PlayerStatsDisplay` GameObject with component
3. Assign all UI references

### 8. Configure Tab 2 (Equipment)

Create UI hierarchy under Tab2_Equipment:
```
Tab2_Equipment/
├── SlotsPanel/
│   ├── AccessorySlots/
│   │   ├── Slot1 (ItemSlotUI)
│   │   ├── Slot2 (ItemSlotUI)
│   │   └── Slot3 (ItemSlotUI)
│   └── PassiveSlots/
│       ├── Slot1 (ItemSlotUI)
│       ├── Slot2 (ItemSlotUI)
│       └── Slot3 (ItemSlotUI)
├── ScrollMenuContainer/
│   └── ScrollMenu (add ScrollMenu.cs)
│       └── Panels (5-7 ScrollUIPanel children)
├── InfoPanel/
│   ├── CurrentItem/ (name, desc, icon)
│   └── SelectedItem/ (name, desc, icon)
└── CharacterModelDisplay
```

On EquipmentMenuUI:
1. Assign `EquipmentDataProvider` to `equipmentDataProviderObject`
2. Assign all slot references
3. Assign scrollMenu reference
4. Connect slot callbacks via Inspector events

### 9. Setup Render Textures (for 3D Models)

For each tab with character model:

1. **Create RenderTexture**:
   - Assets → Create → Render Texture
   - Name: `CharacterModelRT_Tab1`, etc.
   - Size: 512x512 or 1024x1024

2. **Create Camera**:
   - Create Camera in scene: `CharacterCamera_Tab1`
   - Set Target Texture to RenderTexture
   - Set Culling Mask to custom layer (e.g., "UI3D")
   - Set Clear Flags to Solid Color
   - Disable camera initially

3. **Setup Character Model**:
   - Duplicate player model
   - Move to position in front of camera
   - Set layer to "UI3D"
   - Parent to camera

4. **Configure RenderTextureDisplay**:
   - Assign camera reference
   - Assign render texture
   - Set culling mask

### 10. Setup ScrollMenu

For each ScrollMenu:

1. **Create Panel Prefab**: `ScrollUIPanel_Item`
   - Add component inheriting from `ScrollUIPanel`
   - Add UI elements (icon, name, description)
   - Implement `Refresh()` method

2. **Configure ScrollMenu**:
   ```
   Axis: Vertical (for up/down) or Horizontal
   Cycle Mode: CircularStop (recommended)
   Focus Center Panel: true
   Center Panel Index: 2 (middle panel)
   Scroll Cooldown: 0.11s
   Scroll Duration: 0.1s
   ```

3. **Create 5-7 Panel Instances**:
   - Instantiate panel prefab as children
   - Arrange in layout (Vertical/Horizontal Layout Group)

### 11. Connect Input

Input is automatically connected via InputManager:
- Scroll: `InputManager.onScroll`
- Tab Switch: `InputManager.onTabLeft/Right`
- Selection: Unity Event System

No additional setup needed if InputManager exists!

### 12. Settings Tab Configuration

On Tab8_Settings:

1. **Audio Mixer**:
   - Create Audio Mixer with groups: Master, Music, SFX
   - Add exposed parameters (right-click → Expose)
   - Assign mixer to SettingsTab

2. **UI References**:
   - Assign all sliders, dropdowns, toggles
   - Assign text fields for display

3. **Save/Load**:
   - Automatically uses PlayerPrefs
   - No additional setup needed

## Testing Checklist

After setup, test each:

- [ ] Menu opens/closes
- [ ] Tab switching works (LB/RB or Q/E)
- [ ] Character model displays in Tab 1
- [ ] Stats update in real-time
- [ ] Equipment slots show equipped items
- [ ] Clicking slot opens scroll menu
- [ ] Scroll menu navigation works
- [ ] Settings save and load
- [ ] All tabs are accessible

## Common Issues

### "Data provider not found"
- Ensure data provider GameObject has component
- Check reference is assigned in tab script

### "Scroll menu doesn't work"
- Verify IScrollMenuAuthority implementation
- Check input subscription in SubscribeToScroll()
- Ensure panels have correct component

### "Character model doesn't show"
- Check camera is enabled when tab active
- Verify render texture is assigned to RawImage
- Check culling mask matches model layer

### "Tabs don't switch"
- Verify TabGroup has tabActive = true when menu open
- Check TabButton references are correct
- Ensure InputManager callbacks are registered

## Performance Tips

1. **Disable cameras when tabs inactive**: Done automatically
2. **Use object pooling for scroll menus**: Built into ScrollMenu
3. **Limit auto-refresh rate**: Set refreshInterval on CharacterStatsTab
4. **Use lower resolution render textures**: 512x512 is usually enough

## Next Steps

After basic setup:

1. Implement backend systems (quests, compendium)
2. Replace placeholder data in providers
3. Add real attack/skill data to skill tree
4. Create video clips for ability demonstrations
5. Design UI layouts and styling
6. Add animations and transitions
7. Implement key remapping system

## Need Help?

Refer to:
- `MENU_UI_ARCHITECTURE.md` for detailed architecture
- XML comments in code for method documentation
- Example implementations in existing tabs

