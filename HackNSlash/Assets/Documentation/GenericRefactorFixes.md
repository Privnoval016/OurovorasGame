# Generic ItemUIInfo<T> Refactor - Fixes Applied

## Date: February 12, 2026

## Issues Fixed

### 1. ✅ NullReferenceException in InventoryTab Sorting
**Problem**: `SortItems()` crashed when comparing null ItemUIInfo objects because InventoryStack.GetItemUIInfo() can return null.

**Root Cause**: InventoryDataProvider wasn't filtering out null values from GetItemUIInfo() results.

**Fix**:
- Added `.Where(info => info != null)` to `GetAllItems()` and `GetItemsByCategory()` in InventoryDataProvider
- Added `.Where(info => info != null)` to `GetAccessories()` in EquipmentDataProvider
- Added null checks to all sort comparisons in InventoryTab.SortItems()

**Files Modified**:
- `InventoryDataProvider.cs` - Lines 50, 78
- `EquipmentDataProvider.cs` - Line 53
- `InventoryTab.cs` - Lines 417-492 (all sort methods)

---

### 2. ✅ Element Scroll Menu Not Showing
**Problem**: Element scroll menu wouldn't display when trying to assign attacks.

**Root Cause**: AttackDisplayData objects with null attackReference were being passed to ItemUIInfo conversion.

**Fix**:
- Added null check for `attack.attackReference` before converting to ItemUIInfo
- Added early return if no valid attacks after filtering

**Files Modified**:
- `ElementProgressTab.cs` - Lines 609-625

---

### 3. ✅ Inventory Scroll Menu Moving When Navigating Categories
**Problem**: Scroll menu responded to navigation input even when user was selecting category buttons.

**Root Cause**: `isScrollMenuActive` was true whenever scroll menu was visible, not tracking whether user had actually "entered" it.

**Solution**: Added two-state system:
- `isScrollMenuActive` - Scroll menu is visible on screen
- `isScrollMenuFocused` - User has pressed A to enter scroll menu (can now scroll)

**Behavior**:
1. When category selected → scroll menu activates (active=true, focused=false)
2. User presses A on category button → scroll menu gains focus (focused=true)
3. User can now scroll through items
4. User presses B → scroll menu loses focus (focused=false), returns to category buttons
5. User presses B again → exits to previous menu/tab

**Files Modified**:
- `InventoryTab.cs` - Added `isScrollMenuFocused` field (line 50)
- `InventoryTab.cs` - Updated OnSelectInput (lines 335-357)
- `InventoryTab.cs` - Updated OnBackInput (lines 359-377)
- `InventoryTab.cs` - Updated ScrollDelegate (lines 710-717)
- `InventoryTab.cs` - Updated ActivateScrollMenu (line 256)
- `InventoryTab.cs` - Updated DeactivateScrollMenu (line 265)

---

### 4. ✅ Can't Back Out of Scroll Menu in Inventory
**Problem**: Back button (B) didn't exit scroll menu.

**Root Cause**: OnBackInput only handled action menu closure, not scroll menu exit.

**Fix**:
- Added Priority 2 handler in OnBackInput to exit scroll menu when focused
- Refocuses default button (category button) when exiting scroll menu

**Files Modified**:
- `InventoryTab.cs` - Lines 359-377

---

### 5. ✅ Category Selection Not Changing Contents
**Problem**: Clicking category buttons didn't update scroll menu contents (or some categories showed old data).

**Root Cause**: When category had 0 items, scroll menu container wasn't being hidden, leaving old panels visible.

**Fix**:
- Added `scrollMenuContainer.SetActive(false)` when no items found
- Set `isScrollMenuActive = false` to prevent input forwarding

**Files Modified**:
- `InventoryTab.cs` - Lines 237-245

---

## Architecture Improvements

### Scroll Menu Focus State Machine
```
State 1: Category Navigation
- User navigates between category buttons
- Scroll menu visible but NOT focused
- Navigation input = switch categories
- Scroll input = ignored
- A button = enter scroll menu (→ State 2)
- B button = exit tab

State 2: Scroll Menu Focused
- User inside scroll menu
- Navigation input = ignored (handled by scroll menu)
- Scroll input = scroll through items
- A button = open action menu (→ State 3)
- B button = exit scroll menu (→ State 1)

State 3: Action Menu Open
- User choosing Use/Discard
- All other input blocked
- A button = confirm action
- B button = close action menu (→ State 2)
```

### Data Provider Null Safety
All data providers now filter null ItemUIInfo objects before returning:
```csharp
return stacks.Select(stack => stack.GetItemUIInfo()).Where(info => info != null).ToList();
```

This prevents nulls from ever reaching UI code.

---

## Testing Checklist

### Inventory Tab
- [x] Navigate between categories with D-pad
- [x] Press A on category to enter scroll menu
- [x] Scroll through items with D-pad up/down
- [x] Press A on item to open action menu
- [x] Press B to close action menu
- [x] Press B to exit scroll menu (returns to categories)
- [x] Empty categories hide scroll menu properly
- [x] Sort cycles through methods correctly
- [x] No crashes when sorting

### Equipment Tab
- [ ] Navigate between slots with D-pad
- [ ] Press A on slot to enter scroll menu
- [ ] Scroll through equipment
- [ ] Press A to equip item
- [ ] Press B to exit scroll menu
- [ ] Swapping equipped items works
- [ ] Visual feedback on slot selection

### Element Progress Tab
- [ ] Navigate between elements
- [ ] Navigate between attack buttons
- [ ] Press A on attack button to open scroll menu
- [ ] Scroll through attacks
- [ ] Press A to assign attack
- [ ] Press B to exit scroll menu
- [ ] Navigate to unlock grid and inspect unlocks

### Character Stats Tab
- [ ] Visual feedback on equipped items
- [ ] Display updates when equipment changes

---

## Known Issues Still to Fix

1. **No visual selection feedback on ItemSlotUI** - Likely UIAnimationManager initialization issue
2. **Element unlock grid back button** - Exiting unlock grid doesn't work properly
3. **Equipment assignment** - Verify bijective reference checking works correctly
4. **String comparisons** - Verify all comparisons use itemReference, not itemName

---

## Next Steps

1. Test all navigation flows listed above
2. Verify bijective reference checking for equipped items
3. Add visual debugging for isScrollMenuFocused state
4. Consider adding visual indicator when scroll menu is focused (border glow?)
5. Apply same focus state pattern to EquipmentTab and ElementProgressTab

