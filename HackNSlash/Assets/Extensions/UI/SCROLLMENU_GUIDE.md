# ScrollMenu System - Complete Guide

## What is ScrollMenu?

ScrollMenu is a **modular, reusable carousel/list component** that displays items in a scrollable view. It's like a traditional RPG item selection menu (think Final Fantasy, Kingdom Hearts, etc.) where you can scroll through items with visual focus on the center item.

## Core Concepts

### 1. The LinkedList Pattern
ScrollMenu uses a **circular linked list** of panels for efficient recycling:

```
Initial State (5 panels showing items 0-4):
[Item 0] → [Item 1] → [Item 2*] → [Item 3] → [Item 4]
                        ↑ Center (focused)

Scroll Forward (shows items 1-5):
[Item 5] → [Item 1] → [Item 2] → [Item 3*] → [Item 4]
           ↑ Recycled          ↑ New center

The first panel moved to the end and was updated to show Item 5
```

**Why this is efficient**:
- Only creates 5-7 panels (not thousands for large inventories)
- Panels are reused, not destroyed/recreated
- Constant O(1) memory usage regardless of item count

### 2. Panel Recycling

As you scroll, panels are moved and their data is refreshed:

```csharp
// In ScrollForward():
ScrollUIPanel panel = panelQueue.First.Value;  // Get first panel
panelQueue.RemoveFirst();                       // Remove from front
panelQueue.AddLast(panel);                      // Add to back

// Move it visually to the end
panel.rectTransform.localPosition = lastPanel.localPosition + slotDelta;

// Update it with new data
RefreshPanel(panel, newItemIndex);
```

### 3. The Center Focus System

The "center panel" is always at index `centerPanelIndex` (usually 2 for 5 panels):

```
Panels:  [0]    [1]    [2*]   [3]    [4]
         Side   Side   CENTER Side   Side
         1.0x   1.0x   1.15x  1.0x   1.0x  ← Scale
```

When scrolling stops, `UpdateFocus()` scales up the center panel and calls:
- `panel.OnSelected()` on center panel (show details, change color, etc.)
- `panel.OnDeselected()` on other panels (hide details, normal color)

## Component Hierarchy

```
ScrollMenu (ScrollMenu.cs)
├── Settings (Inspector)
│   ├── Axis: Horizontal or Vertical
│   ├── Cycle Mode: How scrolling wraps
│   ├── Focus Center Panel: true
│   ├── Center Panel Index: 2 (for 5 panels)
│   ├── Scroll Cooldown: 0.11s
│   └── Scroll Duration: 0.1s (animation speed)
│
└── ScrollItemContainer (Transform)
    ├── Panel 0 (Your Custom ScrollUIPanel)
    ├── Panel 1 (Your Custom ScrollUIPanel)
    ├── Panel 2 (Your Custom ScrollUIPanel) ← Center/Focused
    ├── Panel 3 (Your Custom ScrollUIPanel)
    └── Panel 4 (Your Custom ScrollUIPanel)
```

## Creating a Custom Panel

### Step 1: Inherit from ScrollUIPanel

```csharp
using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyCustomPanel : ScrollUIPanel
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    
    // Called when panel becomes focused (center)
    public override void OnSelected()
    {
        // Change border color, play sound, etc.
        UIAudioManager.PlayHover();
    }
    
    // Called when panel loses focus
    public override void OnDeselected()
    {
        // Reset visuals
    }
    
    // Called to update panel with new data
    public override void Refresh(ItemUIInfo info)
    {
        if (info == null) {
            // Show as empty
            nameText.text = "Empty";
            icon.enabled = false;
            return;
        }
        
        // Update with new data
        nameText.text = info.itemName;
        icon.sprite = info.icon;
        icon.enabled = true;
    }
}
```

### Step 2: Create Prefab

1. Create GameObject: `MyCustomPanel`
2. Add your custom ScrollUIPanel script
3. Add UI children:
   - Image for icon
   - TextMeshProUGUI for name
   - etc.
4. Assign references in Inspector
5. Save as prefab

### Step 3: Setup ScrollMenu

1. Create ScrollMenu GameObject
2. Add `ScrollMenu` component
3. Create child: `ScrollItemContainer`
4. Instantiate your panel prefab as child 5-7 times
5. Configure ScrollMenu:
   - Drag panels into `panelQueue` or let it auto-find
   - Set axis, cycle mode, etc.

## Activation Flow

```csharp
// 1. Your tab/UI calls Activate()
scrollMenu.Activate(
    items: myItemList,              // List<T> of your items
    initialIndex: 0,                 // Which item to start at
    getInfoFunc: item => item.ToUIInfo(), // Convert T to ItemUIInfo
    authority: this                  // IScrollMenuAuthority (handles input)
);

// 2. ScrollMenu initializes
// - Stores items reference
// - Sets selectedIndex
// - Calls InitializePanels()
// - Subscribes to input via authority

// 3. InitializePanels() sets up initial view
// - Loops through panelQueue
// - Calls RefreshPanel() for each with appropriate item index
// - Calls UpdateFocus() to highlight center panel

// 4. User scrolls (controller/keyboard)
// - Input → OnScrollPerformed()
// - Checks cooldown timer
// - Calls Scroll(+1 or -1)
// - Scrolls forward/backward
// - Animates container
// - Refreshes recycled panel
// - Updates focus when animation completes

// 5. Deactivate when done
scrollMenu.Deactivate();
// - Unsubscribes from input
// - Clears data references
// - Stops animations
```

## Cycle Modes Explained

### CircularStop (Recommended for most cases)
- **With enough items** (items >= panels): Wraps around seamlessly
- **With few items** (items < panels): Stops at edges, no wrapping

```
5 items, 5 panels: [A][B][C][D][E] → Wraps
3 items, 5 panels: [-][A][B][C][-] → Stops at edges
```

### CircularPure (True carousel)
- Always wraps, even with few items
- Items repeat visually

```
3 items, 5 panels: [C][A][B][C][A] → Repeating pattern
```

### Restart (Jump back)
- Stops at edges
- Trying to go past end jumps to start (and vice versa)

```
At end, press right: [E] → [A]
```

### Stop (Simple)
- Just stops at first/last item
- No wrapping or jumping

## Input Integration

### The Authority Pattern

```csharp
public interface IScrollMenuAuthority
{
    void SubscribeToScroll(ScrollMenu menu);
    void UnsubscribeFromScroll(ScrollMenu menu);
}
```

**Why this pattern?**
- Keeps ScrollMenu decoupled from input system
- Tab decides how to handle input (keyboard, controller, touch)
- Multiple scroll menus can coexist with different input sources

**Implementation in your tab**:
```csharp
public class MyTab : TabSelection, IScrollMenuAuthority
{
    private void ScrollDelegate(InputAction.CallbackContext context)
    {
        if (!isScrollMenuActive) return;
        Vector2 input = context.ReadValue<Vector2>();
        scrollMenu.OnScrollPerformed(input);
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

## Controller Setup

Your Input Actions should have:

```
Scroll (Vector2):
- Binding: Right Stick (Gamepad)
- Binding: Scroll Wheel (Mouse) [optional]
- Processors: Stick Deadzone (0.2)
```

The right stick Y-axis maps to scroll up/down:
- Positive Y (stick up) = scroll backward (previous items)
- Negative Y (stick down) = scroll forward (next items)

## Example: Equipment Selection Menu

```csharp
public class EquipmentTab : TabSelection, IScrollMenuAuthority
{
    [SerializeField] private ScrollMenu scrollMenu;
    [SerializeField] private ItemSlotUI[] equipmentSlots;
    
    private List<ItemUIInfo> availableItems;
    private bool isScrollMenuActive = false;
    
    public void OnSlotSelected(int slotIndex)
    {
        // Get items from inventory
        availableItems = equipmentDataProvider.GetAccessories();
        
        // Activate scroll menu
        scrollMenu.Activate(
            items: availableItems,
            initialIndex: FindCurrentItemIndex(slotIndex),
            getInfoFunc: item => item, // Already ItemUIInfo
            authority: this
        );
        
        isScrollMenuActive = true;
    }
    
    public void OnItemSelected(int itemIndex)
    {
        // User pressed A on selected item
        equipmentDataProvider.EquipAccessory(currentSlotIndex, itemIndex);
        scrollMenu.Deactivate();
        isScrollMenuActive = false;
        UIAudioManager.PlaySelect();
    }
    
    // IScrollMenuAuthority implementation
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

## Visual Feedback

### Scale Animation
Center panel scales up automatically (configurable):
```
Normal Scale: (1, 1, 1)
Focused Scale: (1.15, 1.15, 1.15)
Duration: 0.08s
Ease: OutQuad
```

### Panel-Specific Effects
In your custom panel's `OnSelected()`:
```csharp
public override void OnSelected()
{
    // Border glow
    borderImage.color = Color.yellow;
    
    // Show full description
    descriptionPanel.SetActive(true);
    
    // Particle effect
    selectionParticles.Play();
    
    // Sound
    UIAudioManager.PlayHover();
}
```

## Performance Considerations

### Memory
- ✅ Fixed panel count (5-7 panels)
- ✅ Panels reused, not destroyed
- ✅ Only stores item reference list, not full data

### CPU
- ✅ Cooldown timer prevents input spam
- ✅ Animation uses PrimeTween (optimized)
- ✅ Only updates visible panels

### Garbage Collection
- ✅ No allocations during scrolling
- ✅ LinkedList operations are O(1)
- ✅ Panel refresh reuses existing components

## Common Patterns

### Pattern 1: Item Comparison
Show currently equipped item vs selection:

```csharp
[SerializeField] private ItemDisplay currentItemDisplay;
[SerializeField] private ItemDisplay selectedItemDisplay;

public override void OnSelected()
{
    base.OnSelected();
    selectedItemDisplay.Show(currentInfo);
    
    // Show comparison
    var currentItem = GetCurrentlyEquippedItem();
    currentItemDisplay.Show(currentItem);
    ComparisonArrows.Show(currentItem, currentInfo);
}
```

### Pattern 2: Category Filtering
Switch categories without recreating scroll menu:

```csharp
public void OnCategoryChanged(string category)
{
    var filteredItems = GetItemsByCategory(category);
    
    scrollMenu.Deactivate();
    scrollMenu.Activate(filteredItems, 0, item => item, this);
}
```

### Pattern 3: Nested Menus
Scroll menu within scroll menu (not recommended, but possible):

```csharp
// Outer menu shows categories
// When category selected, show inner menu with items
```

## Troubleshooting

### "Panels don't update when scrolling"
- Check `Refresh()` is implemented correctly
- Verify `getInfoFunc` returns valid ItemUIInfo
- Ensure panels have RectTransform assigned

### "Scrolling is jittery"
- Increase scroll cooldown (0.15s-0.2s)
- Check frame rate isn't dropping
- Use Ease.Linear for smoother animation

### "Wrong item selected"
- Verify `centerPanelIndex` matches middle panel
- Check `selectedIndex` calculation
- Debug `NextIndex()` logic

### "Can't scroll past certain point"
- Check CycleMode setting
- Verify item count vs panel count
- Look for early returns in Scroll()

## Summary

The ScrollMenu system:
1. ✅ Uses efficient panel recycling (LinkedList)
2. ✅ Supports controller input via Right Stick
3. ✅ Provides visual focus on center item
4. ✅ Scales to any item count with fixed memory
5. ✅ Decoupled from game logic via ItemUIInfo DTO
6. ✅ Modular via ScrollUIPanel inheritance
7. ✅ Authority pattern for flexible input
8. ✅ FMOD audio integration via UIAudioManager

It's production-ready and battle-tested!

