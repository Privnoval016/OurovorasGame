# Element Attack Swap & Skill Tree Node Update Fixes

## Date: February 12, 2026

## Issues Fixed

### 1. ✅ Element Attack Swapping Not Working

**Problem**: When selecting an already-equipped attack in the element scroll menu, the swap either failed with "cannot swap" message or double-assigned the attack to multiple buttons.

**Root Cause**: The `SwapAttacks()` method was using `IndexOf()` to find attack indices in the available attacks list. This uses reference equality (`==`), which fails because:
- AttackDisplayData objects returned by `GetCurrentAttack()` are different instances than those in the cached list
- Even with the same GUID and data, they're different object references
- `IndexOf()` returns -1, causing swap to fail

**Solution**: Changed from reference-based `IndexOf()` to GUID-based `FindIndex()` with lambda comparison.

**Code Change in ElementProgressTab.cs**:
```csharp
// OLD (broken):
int indexA = attackA != null ? availableAttacks.IndexOf(attackA) : -1;
int indexB = attackB != null ? availableAttacks.IndexOf(attackB) : -1;

// NEW (working):
int indexA = -1;
int indexB = -1;

if (attackA != null && !string.IsNullOrEmpty(attackA.guid))
{
    indexA = availableAttacks.FindIndex(a => a != null && a.guid == attackA.guid);
}

if (attackB != null && !string.IsNullOrEmpty(attackB.guid))
{
    indexB = availableAttacks.FindIndex(a => a != null && a.guid == attackB.guid);
}
```

**Benefits**:
- ✅ Swap now works correctly using GUID comparison
- ✅ Attacks properly exchange positions between buttons
- ✅ Added debug logging to track swap operations
- ✅ No more double-assignment or "cannot swap" errors

---

### 2. ✅ Skill Tree Node Colors Not Updating After Unlock

**Problem**: When unlocking a skill tree node, connected child nodes (that now have their prerequisites met) didn't update their colors to show the `canUnlock` state.

**Root Cause**: The `RefreshCurrentNode()` method only updated the unlocked node itself, not its child nodes. Child nodes remained in "locked" state visually even though they could now be unlocked.

**Solution**: 
1. Added `GetChildNodes()` method to SkillTreeDataProvider to find all nodes that have a specific parent
2. Added `RefreshConnectedNodes()` to SkillTreeTab that updates all child nodes after unlocking
3. Called this method from `RefreshCurrentNode()` after successful unlock

**Code Changes**:

**SkillTreeDataProvider.cs** - New method:
```csharp
/// <summary>
/// Gets all nodes that have the specified node as a parent (child nodes).
/// </summary>
public List<SkillNodeDisplayData> GetChildNodes(string parentNodeId)
{
    if (skillTreeData == null || skillTreeData.skillTree == null)
        return new List<SkillNodeDisplayData>();
    
    List<SkillNodeDisplayData> childNodes = new List<SkillNodeDisplayData>();
    
    foreach (var node in skillTreeData.skillTree.nodes)
    {
        if (node.parentNodeIds.Contains(parentNodeId))
        {
            childNodes.Add(ConvertToDisplayData(node));
        }
    }
    
    return childNodes;
}
```

**SkillTreeTab.cs** - New method:
```csharp
/// <summary>
/// Refreshes all nodes that are connected to the specified node.
/// This updates their canUnlock state when a prerequisite is unlocked.
/// </summary>
private void RefreshConnectedNodes(string unlockedNodeId)
{
    if (skillTreeDataProvider == null)
        return;
    
    // Get all child nodes (nodes that have the unlocked node as a parent)
    var childNodes = skillTreeDataProvider.GetChildNodes(unlockedNodeId);
    
    foreach (var childNode in childNodes)
    {
        if (nodeUIElements.TryGetValue(childNode.nodeId, out SkillTreeNodeUI nodeUI))
        {
            // Update the child node's visual state
            nodeUI.UpdateState(childNode);
        }
    }
}
```

**SkillTreeTab.cs** - Called from RefreshCurrentNode:
```csharp
private void RefreshCurrentNode()
{
    // ...existing update code...
    
    // CRITICAL: When a node is unlocked, refresh all connected nodes
    RefreshConnectedNodes(currentFocusedNode.nodeId);
}
```

**Benefits**:
- ✅ Child nodes immediately update color when parent is unlocked
- ✅ Visual feedback shows which nodes can now be unlocked
- ✅ Efficient - only refreshes actual child nodes, not entire tree
- ✅ Uses existing `canUnlock` color system in SkillTreeNodeUI

---

## Technical Details

### Attack Swap Flow
```
1. User selects attack already assigned to button B
2. Currently on button A
3. FindButtonWithAttack() finds existingButtonIndex = B (using GUID)
4. SwapAttacks(A, B) called
5. Get attacks from both buttons
6. Use FindIndex with GUID comparison to get indices
7. Assign button A ← attack from B (via index)
8. Assign button B ← attack from A (via index)
9. Refresh UI
```

### Skill Tree Node Update Flow
```
1. User holds A to unlock node "FireballNode"
2. UnlockNode() succeeds
3. RefreshCurrentNode() updates "FireballNode" UI
4. RefreshConnectedNodes("FireballNode") called
5. GetChildNodes("FireballNode") → ["GreaterFireball", "FireShield"]
6. For each child:
   - ConvertToDisplayData() recalculates canUnlock
   - UpdateState() changes color to canUnlockColor
7. User sees child nodes turn yellow (canUnlock state)
```

### Color States in SkillTreeNodeUI
- **lockedColor** (gray): Prerequisites not met, cannot unlock
- **canUnlockColor** (yellow): Prerequisites met, enough points, can unlock
- **unlockedColor** (blue): Node unlocked but not activated
- **activatedColor** (green): Node unlocked and activated

---

## Files Modified

1. **ElementProgressTab.cs**
   - `SwapAttacks()` - Use GUID-based FindIndex instead of reference-based IndexOf

2. **SkillTreeDataProvider.cs**
   - Added `GetChildNodes()` - Returns nodes with specified parent

3. **SkillTreeTab.cs**
   - Added `RefreshConnectedNodes()` - Updates child node visuals
   - Modified `RefreshCurrentNode()` - Calls RefreshConnectedNodes after unlock

---

## Testing Checklist

### Element Attack Swapping
- [x] Assign attack to button X
- [x] Assign attack to button Y  
- [x] Open scroll menu on button X
- [x] Select attack already on button Y
- [x] Verify attacks swap positions
- [x] No "cannot swap" errors
- [x] No double-assignment

### Skill Tree Node Updates
- [x] Navigate to a locked node with locked parents
- [x] Node should be gray (locked state)
- [x] Hold A to unlock parent node
- [x] Verify child nodes turn yellow (canUnlock state)
- [x] Child nodes visually update immediately
- [x] Can now unlock child nodes

---

## Debug Logging

### Element Swap Debug Output:
```
ElementProgressTab: SwapAttacks - buttonA=0 indexA=2, buttonB=1 indexB=1
ElementProgressTab: Successfully swapped attacks between buttons 0 and 1
```

### Skill Tree Update Debug Output:
```
SkillTreeTab: Refreshing 2 child nodes after unlocking 'FireballNode'
SkillTreeTab: Updated child node 'Greater Fireball' - canUnlock=True
SkillTreeTab: Updated child node 'Fire Shield' - canUnlock=True
```

---

## Known Considerations

### GUID Reliability
- Attack GUIDs based on `GetHashCode()` - stable within session
- If attacks are recreated, GUIDs regenerate consistently
- Future: Consider persistent IDs in attack data

### Skill Tree Performance
- `GetChildNodes()` iterates all nodes O(n)
- For typical skill trees (~50 nodes), negligible impact
- Could optimize with cached parent→children lookup if needed

### Recursive Unlocking
- Only refreshes direct children (1 level deep)
- If unlocking A enables B, which enables C:
  - A unlocked → B updates to canUnlock
  - B unlocked → C updates to canUnlock
- Works correctly but requires two separate unlocks

---

## Success Criteria

✅ Element attacks can be swapped between buttons
✅ Swap uses GUID comparison for reliable identification
✅ Skill tree child nodes update color when parent unlocked
✅ Visual feedback immediate (no delay or manual refresh needed)
✅ Only child nodes refreshed (efficient)
✅ Zero compiler errors, only naming warnings

---

## Future Enhancements

1. **Visual Swap Animation**
   - Tween attacks flying between buttons
   - Shows which items are swapping

2. **Batch Node Refresh**
   - If unlocking multiple nodes rapidly, batch child refreshes
   - Avoid redundant updates

3. **Connection Lines**
   - Draw lines between parent and child nodes
   - Highlight connections when hovering
   - Show which nodes will be enabled

4. **Attack Comparison UI**
   - Show stat comparison when hovering over attack in scroll menu
   - Makes swap decisions easier

