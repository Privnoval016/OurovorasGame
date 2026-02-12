# Equipment Swap & Element Attack Display Fixes

## Date: February 12, 2026

## Issues Fixed

### 1. ✅ Equipment Item Swap Not Working
**Problem**: When swapping equipped items, `ConvertToItemUIInfo()` was creating ItemUIInfo objects with null `itemReference` and null `guid`, causing the swap to fail.

**Root Cause**: The old implementation created a new ItemUIInfo from scratch using only EquippedItemDisplayData, which doesn't contain the InventoryStack reference or GUID.

**Solution**: Refactored `ConvertToItemUIInfo()` to fetch the actual ItemUIInfo from inventory by searching through accessories/passives lists by name.

**Code Change**:
```csharp
// OLD (broken):
return new ItemUIInfo<InventoryStack>
{
    itemReference = null, // WARNING: No reference!
    guid = null, // WARNING: No GUID!
    itemName = data.itemName,
    // ...
};

// NEW (working):
// Find actual ItemUIInfo from inventory
var allAccessories = equipmentDataProvider.GetAccessories();
var matchingItem = allAccessories.Find(item => item != null && item.itemName == data.itemName);
if (matchingItem != null)
    return matchingItem; // Returns ItemUIInfo with proper reference and GUID
```

**Benefits**:
- ✅ Proper InventoryStack reference for equipping
- ✅ Valid GUID for comparison operations
- ✅ All ItemUIInfo fields populated correctly
- ✅ Swap operation now succeeds

---

### 2. ✅ Element Attack Scroll Menu Not Showing
**Problem**: Attack scroll menu panels weren't appearing, likely due to missing GUIDs in ItemUIInfo objects.

**Root Cause**: When converting `AttackDisplayData` to `ItemUIInfo<AttacksByWeapon>`, the GUID field was not being copied over.

**Solution**: 
1. Ensured GUID is copied from AttackDisplayData to ItemUIInfo
2. Added null/empty GUID checks with detailed logging
3. Updated cached attacks to ensure GUID is always present

**Code Changes**:

**ElementProgressTab.cs**:
```csharp
// Added GUID to ItemUIInfo creation
attackInfoList.Add(new ItemUIInfo<AttacksByWeapon>
{
    guid = attack.guid, // CRITICAL: Copy GUID from AttackDisplayData
    itemReference = attack.attackReference,
    itemName = attack.attackName,
    // ...
});

// Added validation
if (string.IsNullOrEmpty(attack.guid))
{
    Debug.LogWarning($"Skipping attack '{attack.attackName}' with empty GUID");
    continue;
}
```

**ElementProgressDataProvider.cs**:
```csharp
// Ensure cached attacks have GUID
if (attackDisplayDataCache.ContainsKey(cacheKey))
{
    var cached = attackDisplayDataCache[cacheKey];
    // Ensure GUID is set (in case cache was created before GUID system)
    if (string.IsNullOrEmpty(cached.guid))
    {
        cached.guid = GenerateAttackGuid(attack);
    }
    return cached;
}
```

**Benefits**:
- ✅ Attack GUIDs always present
- ✅ Detailed logging shows filtering results
- ✅ Backward compatible with cached attacks
- ✅ Attack scroll menu now displays

---

## Debug Logging Added

Added comprehensive logging to ElementProgressTab to diagnose attack filtering:
- Total available attacks from data provider
- Each attack's validation status (null check, reference check, GUID check)
- Final count of valid attacks after filtering
- Clear warnings for each type of failure

**Example Output**:
```
ElementProgressTab: Got 5 available attacks for Fire
ElementProgressTab: Adding attack 'Fireball' with GUID 'attack_12345'
ElementProgressTab: Adding attack 'Flame Strike' with GUID 'attack_67890'
ElementProgressTab: Created 2 valid attack ItemUIInfo objects
```

---

## Files Modified

1. **EquipmentTab.cs**
   - `ConvertToItemUIInfo()` - Now fetches from inventory instead of creating new

2. **ElementProgressTab.cs**
   - `ActivateScrollMenu()` - Copies GUID, adds validation and logging

3. **ElementProgressDataProvider.cs**
   - `ConvertToDisplayData()` - Ensures cached attacks have GUID

---

## Testing Checklist

### Equipment Swap
- [x] Equip item to slot A
- [x] Equip different item to slot B
- [x] Select item from scroll menu that's already in slot B
- [x] Verify items swap correctly
- [x] Check both items maintain their data

### Element Attacks
- [x] Navigate to element progress tab
- [x] Select an attack button
- [x] Verify scroll menu appears with attacks
- [x] Check console for attack count logs
- [x] Verify all attacks have GUIDs
- [x] Can scroll through attacks
- [x] Can assign attack to button

---

## Known Limitations

1. **ConvertToItemUIInfo uses name matching**
   - Relies on unique item names
   - If two items have same name, may return wrong one
   - **Future**: Store GUID in EquippedItemDisplayData for exact matching

2. **Attack GUID uses hash code**
   - Hash codes can change between runs in some cases
   - Generally stable for same session
   - **Future**: Use stable ID from attack data

---

## Architecture Notes

### Equipment Swap Flow
1. User selects item already equipped in another slot
2. `OnItemConfirmed()` detects existing slot via `FindAccessorySlotWithItem()`
3. `SwapItems()` called with both slot indices
4. `ConvertToItemUIInfo()` fetches actual ItemUIInfo from inventory
5. Both items equipped via data provider (swap complete)
6. Slots refreshed to show new items

### Attack Display Flow
1. `GetAvailableAttacks()` returns `List<AttackDisplayData>` with GUIDs
2. Each AttackDisplayData converted to `ItemUIInfo<AttacksByWeapon>`
3. GUID copied from AttackDisplayData to ItemUIInfo
4. Validation ensures no null references or missing GUIDs
5. Valid attacks passed to ScrollMenu.Activate()
6. Panels display attacks with proper identification

---

## Success Criteria

✅ Equipment items can be swapped between slots
✅ ItemUIInfo has valid itemReference and guid after conversion
✅ Element attack scroll menu displays attacks
✅ All attacks have valid GUIDs
✅ Detailed logging helps diagnose issues
✅ No compiler errors, only naming warnings

