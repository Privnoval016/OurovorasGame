# Controller Navigation Setup Guide

## Overview
This guide explains how to make your menu UI fully navigable with a controller (Xbox/PlayStation/etc.). Unity's Event System handles controller navigation automatically when properly configured.

## How Unity Controller Navigation Works

### The Event System
Unity's `EventSystem` translates controller input into navigation events:
- **D-Pad/Left Stick**: `Move` events (up/down/left/right)
- **A Button (Xbox) / X (PlayStation)**: `Submit` event (select/confirm)
- **B Button (Xbox) / Circle (PlayStation)**: `Cancel` event (back/cancel)

### Selectable Components
Any UI element with a `Selectable` component (Button, Toggle, Slider, etc.) can be navigated:
- Event System tracks which Selectable is currently "selected"
- Visual feedback shows selected state (color change, highlight, etc.)
- Controller input moves between Selectables based on Navigation settings

## Step-by-Step Setup

### 1. Configure Tab Buttons for Controller Navigation

On each `TabButton` GameObject:

1. **Add Button Component** (if not already present)
2. **Configure Button Navigation**:
   ```
   Navigation: Explicit or Horizontal
   - Select On Left: Previous tab button
   - Select On Right: Next tab button
   - Select On Down: First element in tab content
   ```
3. **Configure Visual Transition**:
   ```
   Transition: Color Tint
   - Normal Color: White
   - Highlighted Color: Light Gray (controller hovering)
   - Selected Color: Yellow (tab is active)
   - Pressed Color: Dark Gray
   ```

**Example**:
```
Tab1Button (Character Stats)
├── Navigation:
│   ├── Mode: Explicit
│   ├── Select On Left: Tab8Button
│   ├── Select On Right: Tab2Button
│   └── Select On Down: StatsPanel/FirstButton
└── Colors:
    ├── Normal: #FFFFFF
    ├── Highlighted: #C8C8C8  
    ├── Selected: #FFFF00
    └── Pressed: #808080
```

### 2. Configure Slot Buttons (Equipment, Items)

On each `ItemSlotUI`:

1. **Ensure Button component exists**
2. **Configure Navigation**:
   ```
   Navigation: Explicit or Automatic
   - For grid layout: Set up/down/left/right neighbors
   - For list layout: Set up/down only
   ```
3. **Connect to Slot Selection**:
   - Use Button's OnClick event
   - Call the tab's slot selection method (e.g., `EquipmentMenuUI.OnAccessorySlotSelected(int)`)

**Example** (Equipment Tab - 3x2 Grid):
```
AccessorySlot1 (Top-Left)
├── Navigation:
│   ├── Select On Right: AccessorySlot2
│   ├── Select On Down: PassiveSlot1
│   └── Select On Up: TabButton (to switch tabs)

AccessorySlot2 (Top-Middle)  
├── Navigation:
│   ├── Select On Left: AccessorySlot1
│   ├── Select On Right: AccessorySlot3
│   └── Select On Down: PassiveSlot2
```

### 3. Configure Category/Filter Buttons

For buttons that filter content (Inventory categories, Quest types, etc.):

1. **Setup Button Navigation**:
   ```
   Navigation: Vertical (for vertical list)
   - Automatic navigation works well here
   ```
2. **Connect OnClick event** to filtering method
3. **Add audio** via UIAudioManager:
   ```csharp
   public void OnCategoryButtonClicked(string category)
   {
       UIAudioManager.PlaySelect();
       SelectCategory(category);
   }
   ```

### 4. Setup ScrollMenu Controller Input

The ScrollMenu already handles controller input through InputManager. No additional setup needed!

**How it works**:
1. When a slot is selected, `ActivateScrollMenu()` is called
2. ScrollMenu subscribes to `InputManager.onScroll` event
3. Controller Right Stick Y-axis is mapped to scroll in Input Actions
4. User moves stick up/down to scroll through items
5. Press A to select item, B to cancel

**Input Actions Setup** (should already exist in your UI action map):
```
Scroll (Vector2):
- Binding: Right Stick
- Processors: Stick Deadzone (0.2)

Navigate (Vector2):
- Binding: D-Pad
- Binding: Left Stick

Submit (Button):
- Binding: A Button (Xbox) / Cross (PS)

Cancel (Button):  
- Binding: B Button (Xbox) / Circle (PS)
```

### 5. Skill Tree Controller Navigation

The skill tree needs special handling since it's a 2D navigable space:

**Current Implementation** (Keyboard):
- WASD moves the tree viewport
- Shows selected node info

**Controller Enhancement**:
```csharp
// In SkillTreeTab.HandleNavigation()
private void HandleNavigation()
{
    Vector2 input = Vector2.zero;
    
    // Support both keyboard and controller
    if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        input.y = 1f;
    // ... etc for other directions
    
    // ADDED: Controller support via InputManager
    if (InputManager.Instance != null)
    {
        Vector2 controllerInput = InputManager.Instance.Movement; // Left stick
        if (controllerInput.magnitude > 0.3f) // Deadzone
            input = controllerInput;
    }
    
    if (input != Vector2.zero)
    {
        Vector2 movement = input * navigationSpeed * Time.deltaTime;
        skillTreeContainer.anchoredPosition += movement;
    }
}
```

### 6. First Selected Element

When a tab opens, ensure something is selected automatically:

```csharp
// In TabSelection.OnTabSelect()
public override void OnTabSelect()
{
    base.OnTabSelect();
    
    // Set first selected element for controller
    if (defaultSelectedButton != null && EventSystem.current != null)
    {
        EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
    }
}
```

## Controller-Specific UI Patterns

### Button Prompts
Show context-sensitive button prompts based on current selection:

```csharp
[SerializeField] private GameObject buttonPromptsPanel;
[SerializeField] private TextMeshProUGUI promptText;

private void UpdateButtonPrompts()
{
    if (isScrollMenuActive)
    {
        promptText.text = "[LS] Navigate  [A] Select  [B] Back";
    }
    else
    {
        promptText.text = "[D-Pad] Navigate  [A] Select  [LB/RB] Switch Tab";
    }
}
```

### Tab Switching with Bumpers
Already implemented in `TabGroup.cs`:
- LB (onTabLeft) switches to previous tab
- RB (onTabRight) switches to next tab
- Works automatically with InputManager

### Audio Feedback
Add audio to all controller interactions:

```csharp
// In TabButton.Select()
public void Select()
{
    isSelected = true;
    UIAudioManager.PlayTabSwitch();
    OnTabSelect();
}

// In ItemSlotUI.Select()
private void Select()
{
    isSelected = true;
    UIAudioManager.PlayHover();
    UpdateVisuals();
    onSlotSelected?.Invoke(slotIndex);
}
```

## Testing Checklist

Test with controller connected:

- [ ] Can navigate between tabs with LB/RB
- [ ] Can navigate between buttons with D-Pad/Left Stick
- [ ] Press A selects/confirms
- [ ] Press B cancels/goes back
- [ ] Visual feedback shows current selection (highlight/border)
- [ ] Audio plays for all interactions
- [ ] Scroll menus work with Right Stick
- [ ] Skill tree navigates with Left Stick
- [ ] First element auto-selected when tab opens
- [ ] Can navigate through all tabs without getting stuck

## Common Issues & Solutions

### "Controller not responding"
- Check Input Actions are enabled
- Verify controller is detected (`InputSystem.devices`)
- Ensure Event System has Input System UI Input Module

### "Selection jumps around weirdly"
- Review Navigation settings on Buttons
- Use Explicit navigation for complex layouts
- Verify no circular references

### "Can't exit scroll menu with controller"
- Ensure Cancel button (B) is wired to `DeactivateScrollMenu()`
- Check `IScrollMenuAuthority.UnsubscribeFromScroll()` is called

### "No visual feedback on selection"
- Check Button Transition is set to Color Tint
- Verify Highlighted/Selected colors are different from Normal
- Ensure Target Graphic is assigned

## Performance Tips

1. **Disable inactive tabs**: Only the active tab should have Selectables enabled
2. **Limit Automatic navigation**: Use Explicit for large menus
3. **Cache EventSystem reference**: Don't call `EventSystem.current` every frame

## Advanced: Custom Controller Skins

Support different controller types (Xbox, PlayStation, Switch):

```csharp
public enum ControllerType { Xbox, PlayStation, Switch, Keyboard }

public static ControllerType DetectController()
{
    var gamepad = Gamepad.current;
    if (gamepad == null) return ControllerType.Keyboard;
    
    // Check device name
    if (gamepad.name.Contains("Xbox")) return ControllerType.Xbox;
    if (gamepad.name.Contains("DualShock") || gamepad.name.Contains("PS"))
        return ControllerType.PlayStation;
    if (gamepad.name.Contains("Switch")) return ControllerType.Switch;
    
    return ControllerType.Xbox; // Default
}
```

Then show appropriate button icons (A vs X, etc.) in your button prompts.

---

## Summary

Your menu UI is now fully controller-compatible:
1. ✅ Tab navigation with bumpers (LB/RB)
2. ✅ Button/slot navigation with D-Pad/Left Stick  
3. ✅ Scroll menus with Right Stick
4. ✅ Select with A, Cancel with B
5. ✅ FMOD audio feedback
6. ✅ Visual selection indicators

The key is proper Navigation setup on all Selectable components and using Unity's Event System correctly. The rest is handled automatically!

