# GUID-Based Identification System

## Date: February 12, 2026

## Overview

Implemented a GUID-based unique identification system for all menu items to solve reference equality issues when using `ItemUIInfo<InventoryStack>` instead of `ItemUIInfo<InventoryItem>`.

## Problem

When changing from `ItemUIInfo<InventoryItem>` to `ItemUIInfo<InventoryStack>`, reference equality checks broke because:
1. InventoryStack instances are not stable references
2. Multiple ItemUIInfo objects could reference different stack instances for the same item
3. Reference comparison (`a.itemReference == b.itemReference`) became unreliable

## Solution: GUID System

Added a `guid` field to `ItemUIInfo<T>` and `AttackDisplayData` for unique, stable identification.

### GUID Generation

**For Inventory Items (InventoryStack):**
```csharp
private string GenerateGuid()
{
    if (item == null) return Guid.NewGuid().ToString();
    
    // Stable GUID based on item's instance ID and amount
    return $"item_{item.GetInstanceID()}_{amount}";
}
```

**For Attacks (AttacksByWeapon):**
```csharp
private string GenerateAttackGuid(AttacksByWeapon attack)
{
    if (attack == null) return System.Guid.NewGuid().ToString();
    return $"attack_{attack.GetHashCode()}";
}
```

## Files Modified

### Core Data Structures
1. **ItemUIInfo.cs**
   - Added `guid` field
   - Kept `itemReference` for data access but NOT for equality checks
   - Documentation warns to use GUID for comparisons

2. **MenuDisplayDataExtended.cs** (AttackDisplayData)
   - Added `guid` field
   - Used for attack identification in element progress menu

### Data Generation
3. **InventoryStack.cs**
   - `GetItemUIInfo()` generates GUID based on item instance ID
   - Returns `ItemUIInfo<InventoryStack>` (not InventoryItem)

4. **ElementProgressDataProvider.cs**
   - `ConvertToDisplayData()` generates attack GUID
   - Added `GenerateAttackGuid()` helper method

### Data Providers
5. **InventoryDataProvider.cs**
   - Returns `List<ItemUIInfo<InventoryStack>>`
   - Already had null filtering

6. **EquipmentDataProvider.cs**
   - Returns `List<ItemUIInfo<InventoryStack>>`
   - `EquipAccessory()` uses `item.itemReference.item` to get actual item

### Interfaces
7. **IMenuDataProviders.cs** (IInventoryDataProvider)
   - Updated to `List<ItemUIInfo<InventoryStack>>`

8. **IEquipmentDataProvider.cs** (standalone)
   - Updated to `ItemUIInfo<InventoryStack>`
   - Both Get methods and Equip methods

### UI Panels
9. **ItemScrollPanel.cs**
   - Changed to `ItemUIInfo<InventoryStack>`
   - Casts from object in Refresh()

### Tab Controllers
10. **InventoryTab.cs**
    - All methods use `ItemUIInfo<InventoryStack>`
    - `IsItemEquipped()` uses GUID comparison
    - `UnequipItem()` takes GUID string parameter
    - `GetSelectedItem<InventoryStack>()` call
    - Callback: `obj as ItemUIInfo<InventoryStack>`

11. **EquipmentTab.cs**
    - Already using `ItemUIInfo<InventoryStack>`
    - `IsItemEquippedInAnySlot()` updated to GUID comparison
    - `GetSelectedItem<InventoryStack>()` call
    - Callback: `obj as ItemUIInfo<InventoryStack>`

12. **ElementProgressTab.cs**
    - `FindButtonWithAttack()` uses GUID comparison
    - Attack scroll menu creates `ItemUIInfo<AttacksByWeapon>`

## Usage Pattern

### Comparing Items
```csharp
// ❌ OLD (broken with InventoryStack):
if (itemA.itemReference == itemB.itemReference)

// ✅ NEW (works with GUID):
if (itemA.guid == itemB.guid)
```

### Checking if Item is Equipped
```csharp
// ❌ OLD:
var match = accessories.Find(a => a.itemReference == selectedItem.itemReference);

// ✅ NEW:
var match = accessories.Find(a => a.guid == selectedItem.guid);
```

### Accessing Item Data
```csharp
// itemReference still used for DATA ACCESS (not comparison):
Accessory accessory = itemUIInfo.itemReference.item as Accessory;
AttacksByWeapon attack = attackDisplayData.attackReference;
```

## Benefits

✅ **Stable Identity**: GUID doesn't change even if references are recreated
✅ **Works with InventoryStack**: No longer relying on stack reference equality
✅ **Consistent**: Same approach for items and attacks
✅ **Debuggable**: GUIDs are readable strings showing type and ID
✅ **No Breaking Changes**: itemReference still available for data access

## GUID Format

- **Inventory Items**: `item_{instanceID}_{amount}`
  - Example: `item_12345_1`
  
- **Attacks**: `attack_{hashCode}`
  - Example: `attack_67890`

## Important Notes

1. **itemReference is NOT for comparison!**
   - Only use for accessing data (`.item`, `.amount`, etc.)
   - NEVER use `==` on itemReference

2. **GUID is generated, not stored**
   - GUIDs are calculated on-the-fly in `GetItemUIInfo()`
   - Same item always generates same GUID (stable)

3. **Null Safety**
   - All comparison methods check `string.IsNullOrEmpty(guid)`
   - Fall back to `Guid.NewGuid()` if item is null

4. **EquippedItemDisplayData doesn't have GUID yet**
   - Still comparing by name as fallback
   - Could be enhanced in future

## Testing Checklist

### Inventory Tab
- ✅ Items display in scroll menu
- ✅ Can navigate categories
- ✅ Can enter scroll menu with A
- ✅ Can scroll through items
- ✅ Equipped indicator shows correctly
- ✅ Can open action menu on item
- ✅ Can discard item (unequips if equipped)
- ✅ Can exit scroll menu with B

### Equipment Tab  
- ✅ Accessories display in scroll menu
- ✅ Can equip item to slot
- ✅ Equipped indicator shows on items
- ✅ Swapping items works correctly
- ✅ Can exit scroll menu with B

### Element Progress Tab
- ✅ Attacks display in scroll menu
- ✅ Can assign attack to button
- ✅ Swapping attacks works
- ✅ GUID comparison prevents duplicate assignments
- ✅ Can exit scroll menu with B

## Future Enhancements

1. **Add GUID to EquippedItemDisplayData**
   - Would eliminate name-based fallback comparisons
   
2. **Persistent GUIDs**
   - Store GUIDs in save data for long-term stability
   - Current GUIDs regenerate on load (but consistently)

3. **GUID-based Save System**
   - Use GUIDs for inventory serialization
   - Easier to track which specific items are equipped

4. **Debugging Tools**
   - GUID inspector to see what items have what GUIDs
   - Validation tool to check for GUID collisions

