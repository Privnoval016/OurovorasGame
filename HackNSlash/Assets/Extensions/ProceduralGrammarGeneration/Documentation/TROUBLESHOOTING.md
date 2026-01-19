# Troubleshooting Guide

## Issue: Terminals Still Have Parameters in Generation

### Symptoms:
- Spatial output shows terminals (Door, Window, Wall, RoofTile) with parameters like `width`, `height`, `depth`
- Symbol distribution shows all symbols as `[NON-TERMINAL]`
- Incorrect spacing/positioning during generation
- Elements appear at wrong Y coordinate (e.g., roof tiles at Y=0 instead of top of building)

### Root Cause:
The `.pgr` grammar file is correct (terminals have no parameters), but the `GrammarAsset` (Unity ScriptableObject) is loading **cached/outdated symbol definitions** with parameters still attached.

### Solution:
**Re-import the grammar file into Unity:**

1. **Option A - Context Menu:**
   - In Unity Project window, right-click on `ComplexBuilding.pgr`
   - Select "Import Grammar" or "Reimport"
   - Unity will parse the .pgr file and update the GrammarAsset

2. **Option B - Delete and Recreate:**
   - Delete the existing GrammarAsset file
   - Right-click on `ComplexBuilding.pgr` → "Import Grammar"
   - Reassign the new GrammarAsset to your test scripts

3. **Option C - Manual Refresh (in Grammar Editor Window):**
   - Open the Grammar Editor Window
   - Load the ComplexBuilding grammar
   - Click "Reload from .pgr" or "Sync from Source"

### Verification:
After re-importing, check the derivation output (`grammar_output.txt`):

**Incorrect (cached):**
```
- Window [TERMINAL] (width=2, height=2.45, depth=0.2) [Terminal]
- Door [TERMINAL] (width=1.5, height=3.5, depth=0.3) [Terminal]
```

**Correct (after re-import):**
```
- Window [TERMINAL] [Terminal]
- Door [TERMINAL] [Terminal]
```

### Why This Happens:
- Unity serializes GrammarAsset symbol definitions when first imported
- Editing the .pgr file directly doesn't automatically update the serialized Unity asset
- The grammar engine reads from GrammarAsset, not directly from .pgr
- Similar to how editing a .fbx file doesn't auto-update meshes until re-import

### Prevention:
- Always use Unity's import system when modifying grammar files
- Keep .pgr file as source of truth
- Regenerate GrammarAsset after grammar structure changes
- Use version control for both .pgr and .asset files
