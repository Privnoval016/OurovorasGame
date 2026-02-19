# Cutscene Engine - Editor Tooling Guide

## Overview

The Cutscene Engine provides three levels of editor tooling to streamline cutscene authoring:

1. **Custom Clip Inspector** - Smart inspector for Timeline clips
2. **Cutscene Editor Window** - High-level cutscene assembly interface
3. **Action Library** - Reusable action asset management

---

## Table of Contents

1. [Custom Clip Inspector](#custom-clip-inspector)
2. [Cutscene Editor Window](#cutscene-editor-window)
3. [Action Library Management](#action-library-management)
4. [Workflow Examples](#workflow-examples)
5. [Tips & Best Practices](#tips--best-practices)

---

## Custom Clip Inspector

### Location
Automatically appears when selecting a `CutsceneActionClip` in Timeline.

### Features

#### 1. Bound Actor Display
- Shows which actor is bound to the track
- Lists available `[CutsceneAction]` methods on that actor
- Warns if no actor is bound

#### 2. Action Type Selection
- Dropdown showing all `ICutsceneAction` implementations
- Options include:
  - **Actor Method** - Call any `[CutsceneAction]` method on actor
  - **Move Actor** - Move actor to position
  - **Rotate Actor** - Rotate actor to orientation
  - **Play Animation** - Trigger animation by ID
  - **Focus Camera** - Point camera at actor
  - **Move Camera** - Move camera to position
  - **Shake Camera** - Apply camera shake effect

#### 3. Dynamic Parameter Generation
When **Actor Method** is selected:
- Dropdown shows all methods marked with `[CutsceneAction]` on the bound actor
- Selecting a method auto-generates parameter fields based on method signature
- Supported parameter types:
  - `int` → IntField
  - `float` → FloatField
  - `bool` → Toggle
  - `string` → TextField
  - `Vector3` → Vector3Field
  - `GameObject` → ObjectField
  - `Enum` → EnumPopup

**Example**:
```csharp
// On EnemyActor.cs
[CutsceneAction("Cast Spell")]
public void CastSpell(string spellName, Vector3 target, float intensity)
{
    // Spell logic
}
```

**Inspector will show**:
```
Method: [Cast Spell ▼]
Parameters:
  spellName: [____________]
  target:    [X: 0, Y: 0, Z: 0]
  intensity: [1.5]
```

#### 4. Action-Specific Configuration
For other action types, shows relevant fields:

**Move Actor**:
- Target (Vector3)
- Duration (float)
- Play Walk Animation (bool)

**Rotate Actor**:
- Euler Rotation (Vector3)
- Duration (float)

**Play Animation**:
- Animation ID (string)

**Focus Camera**:
- Focus On Self (bool)
- Explicit Target (GameObject)

**Move Camera**:
- Target Position (Vector3)
- Duration (float)
- Use Target Object (bool)
- Target Object (GameObject)

**Shake Camera**:
- Intensity (float, 0-10)
- Duration (float)

#### 5. Validation Button
- "Validate Action" button at bottom of inspector
- Checks:
  - Action is configured
  - Actor is bound
  - Parameters match method signature
  - Required references are assigned
- Shows validation report dialog

### Usage

1. **Select clip in Timeline**
2. **Inspector updates automatically**
3. **Choose action type** from dropdown
4. **Configure parameters**
5. **Click "Validate Action"** to check for issues

---

## Cutscene Editor Window

### Opening
`Window > Cutscene Engine > Cutscene Editor`

### Layout

```
┌─────────────────────────────────────────────────────────┐
│              CUTSCENE EDITOR                            │
│      Fast cutscene authoring for Timeline               │
├─────────────────────────────────────────────────────────┤
│ Playable Director: [___________] [Open Timeline]       │
│                                  [Play] [Stop]          │
├─────────────────────────────────────────────────────────┤
│ [Scene Actors] [Action Library] [Settings]             │
├─────────────────────────────────────────────────────────┤
│ (Tab Content)                                           │
└─────────────────────────────────────────────────────────┘
```

### Tabs

#### Scene Actors Tab

**Purpose**: Browse actors in scene and quickly add their actions to Timeline.

**Layout**:
```
┌─────────────────┬───────────────────────────────────┐
│ Scene Actors    │ Actions for: PlayerActor          │
│ [Refresh]       │ Type: PlayerActor                 │
│                 │                                   │
│ PlayerActor     │ 8 Available Actions:              │
│ [Select]        │                                   │
│                 │ • Cast Fireball [Add to Timeline] │
│ EnemyBoss       │ • Equip Weapon  [Add to Timeline] │
│ [Select]        │ • Say Dialogue  [Add to Timeline] │
│                 │ • Play Victory  [Add to Timeline] │
│ CameraRig       │ ...                               │
│ [Select]        │                                   │
└─────────────────┴───────────────────────────────────┘
```

**Features**:
- Lists all `ICutsceneActor` GameObjects in scene
- Click actor to view its available actions
- "Add to Timeline" creates clip on Timeline
  - Finds or creates track for actor
  - Creates clip with selected action
  - Binds track to actor
- Automatically refreshes when scene changes

**Workflow**:
1. Click "Refresh" to scan scene for actors
2. Select an actor from the list
3. Browse available `[CutsceneAction]` methods
4. Click "Add to Timeline" for any action
5. Clip appears in Timeline ready to configure

#### Action Library Tab

**Purpose**: Manage reusable action definition assets.

**Features**:
- Lists all `CutsceneActionDefinition` ScriptableObjects in project
- Browse action templates
- Create new action definitions
- (Future) Drag-and-drop into Timeline

**Creating Action Definition**:
1. Click "Create New Action Definition"
2. Choose save location
3. Configure action name and parameters
4. Save asset

**Using Action Definition**:
- Reference in scripts
- Instantiate with preconfigured parameters
- Share across multiple cutscenes

#### Settings Tab

**Purpose**: Configure editor preferences and defaults.

**Planned Settings**:
- Default action durations
- Auto-sync clip lengths to action duration
- Preview options
- Validation rules
- Action categories/filters

---

## Action Library Management

### Creating Reusable Actions

**Method 1: From Assets Menu**
```
Right-click in Project > Create > Cutscene > Action Definition
```

**Method 2: From Cutscene Editor**
```
Cutscene Editor > Action Library Tab > "Create New Action Definition"
```

### Action Definition Structure

```
CutsceneActionDefinition
├── actionName (string) - Method name to invoke
└── parameters (List<SerializedCutsceneParameter>)
    ├── Parameter 0
    │   ├── type (Int/Float/Bool/String/Vector3/GameObject)
    │   └── value (based on type)
    ├── Parameter 1
    └── ...
```

### Example Action Definition

**Boss Taunt Action**:
```
actionName: "Play Taunt"
parameters:
  [0] type: String, stringValue: "You cannot defeat me!"
  [1] type: Float, floatValue: 3.0
```

This can be reused across multiple boss encounters with different dialogue and durations.

---

## Workflow Examples

### Workflow 1: Quick Action from Scene

**Goal**: Make player cast a fireball spell.

**Steps**:
1. Open Cutscene Editor (`Window > Cutscene Engine > Cutscene Editor`)
2. Select Playable Director in scene
3. Go to "Scene Actors" tab
4. Click on "PlayerActor" in list
5. Find "Cast Fireball" in available actions
6. Click "Add to Timeline"
7. Select new clip in Timeline
8. Inspector shows method parameters
9. Configure: `spellName = "Fireball"`, `target = (10, 0, 5)`, `damage = 50`
10. Done!

**Time**: ~30 seconds

---

### Workflow 2: Complex Cutscene Assembly

**Goal**: Boss introduction with camera work and dialogue.

**Steps**:

**Phase 1: Setup**
1. Create Timeline on empty GameObject
2. Add `CutsceneDirector` component
3. Assign Timeline to director
4. Open Cutscene Editor

**Phase 2: Add Boss Actions**
1. Select "EnemyBoss" actor in Cutscene Editor
2. Add "Roar" action → appears at 0:00
3. Add "Summon Minions" action → move to 2:00
4. Add "Play Taunt" action → move to 4:00

**Phase 3: Add Camera Actions**
1. Create empty GameObject "CutsceneCamera" with `ICutsceneActor`
2. Add Timeline track for CutsceneCamera
3. Create clip: Action Type = "Focus Camera", target = Boss
4. Create clip: Action Type = "Shake Camera", intensity = 5
5. Create clip: Action Type = "Move Camera", target = behind player

**Phase 4: Add Player Reactions**
1. Select "PlayerActor" in Cutscene Editor
2. Add "Move Back" action
3. Add "Play Reaction Anim" action
4. Add "Say Dialogue" action with text: "That's a big boss..."

**Phase 5: Fine-tune**
1. Adjust clip timings in Timeline
2. Use Timeline scrubbing to preview
3. Click "Play Cutscene" in Cutscene Editor to test
4. Validate all clips

**Time**: ~5-10 minutes

---

### Workflow 3: Reusable Action Asset

**Goal**: Create a reusable "Boss Entrance" action sequence.

**Steps**:
1. Open Cutscene Editor > Action Library Tab
2. Click "Create New Action Definition"
3. Name: "BossEntrance.asset"
4. Configure:
   - actionName: "Roar"
   - parameters: (none)
5. Save asset
6. Create more definitions: "SummonMinions", "Taunt", etc.
7. In future cutscenes, reference these assets
8. Consistent boss behaviors across all encounters

**Benefits**:
- Consistent animations/timing
- Easy to update all bosses at once
- Designer-friendly asset references

---

## Tips & Best Practices

### Inspector Tips

1. **Use Validation Often**: Click "Validate Action" button regularly to catch issues early

2. **Parameter Tooltips**: Hover over parameter names to see type information

3. **Undo Support**: All parameter changes support Unity's undo system (Ctrl+Z)

4. **Type Ahead**: In method dropdowns, start typing to filter options

5. **Copy/Paste Clips**: Timeline clips can be duplicated (Ctrl+D) with all parameters

### Cutscene Editor Tips

1. **Keep Window Open**: Dock Cutscene Editor next to Timeline for fastest workflow

2. **Refresh Frequently**: Hit "Refresh" in Scene Actors tab after adding new actors

3. **Name Your Actors**: Use descriptive GameObject names - they appear in actor list

4. **Quick Timeline Access**: Use "Open Timeline" button to jump between windows

5. **Play Mode Testing**: Use "Play Cutscene" and "Stop Cutscene" buttons to test without leaving editor

### Action Library Tips

1. **Organize Assets**: Create folders for action types:
   ```
   Assets/Cutscenes/Actions/
   ├── Boss/
   ├── Player/
   ├── Camera/
   └── Environment/
   ```

2. **Naming Convention**: Use descriptive names that indicate actor and action:
   ```
   Boss_Entrance.asset
   Player_Victory.asset
   Camera_DramaticZoom.asset
   ```

3. **Version Actions**: For iterative changes, duplicate assets:
   ```
   Boss_Entrance_v1.asset
   Boss_Entrance_v2.asset
   ```

4. **Document Complex Actions**: Add comments in the ScriptableObject inspector

### General Best Practices

1. **Start Simple**: Build basic cutscenes before adding complex actions

2. **Test Incrementally**: Add one action, test, add another, test

3. **Use Explicit Targets**: For multi-actor interactions, use explicit target overrides

4. **Validate Before Committing**: Run validation on all clips before marking cutscene complete

5. **Leverage Lifecycle Hooks**: Implement `OnCutsceneEnter/Exit` properly to pause gameplay

6. **Preview in Timeline**: Use Timeline scrubbing to preview without entering Play Mode

7. **Organize Tracks**: Name Timeline tracks clearly (e.g., "Boss - Actions", "Player - Reactions", "Camera")

8. **Lock Track Bindings**: Once bound, avoid changing actor bindings mid-production

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl/Cmd + D` | Duplicate selected Timeline clip |
| `Ctrl/Cmd + Z` | Undo parameter change |
| `Ctrl/Cmd + Y` | Redo parameter change |
| `Delete` | Delete selected Timeline clip |
| `F` (in Timeline) | Frame selected clip |
| `Space` (in Timeline) | Play/pause Timeline |

---

## Troubleshooting

### "No actor bound to track"

**Cause**: Timeline track is not bound to an actor GameObject.

**Fix**: 
1. Select track in Timeline
2. Drag actor GameObject to "Bound Object" field in Track Inspector
3. Or use Cutscene Editor's "Add to Timeline" to auto-bind

### "Method not found on actor"

**Cause**: Selected method name doesn't match any `[CutsceneAction]` on actor.

**Fix**:
1. Verify actor has `[CutsceneAction("MethodName")]` attribute
2. Check spelling matches exactly
3. Ensure actor's `GetCutsceneAdapter()` is implemented correctly
4. Click "Refresh" in Cutscene Editor

### "Parameter count mismatch"

**Cause**: Method signature changed but clip parameters weren't updated.

**Fix**:
1. Select clip
2. Choose method again from dropdown
3. Parameters will regenerate automatically

### "Cutscene Editor doesn't show actors"

**Cause**: No actors in scene or actors don't implement `ICutsceneActor`.

**Fix**:
1. Ensure GameObjects implement `ICutsceneActor`
2. Click "Refresh" button
3. Check that actors are active in hierarchy

### "Action doesn't execute"

**Cause**: Multiple possible issues.

**Fix**:
1. Verify `CutsceneDirector` component exists on PlayableDirector GameObject
2. Check that Timeline is playing
3. Ensure actor is registered (in `CutsceneDirector.RegisterActors()`)
4. Verify action is properly configured in clip inspector
5. Check Console for error messages

---

## Advanced Features

### Custom Action Type Creation

To add your own action type that appears in the inspector:

1. **Create action class**:
```csharp
[Serializable]
public class TeleportActorAction : ICutsceneAction
{
    public Vector3 Destination;
    public ParticleSystem TeleportEffect;
    
    public void Execute(ICutsceneActor actor, CutsceneContext context)
    {
        // Teleport logic
    }
}
```

2. **Action automatically appears** in inspector dropdown
3. **Fields are auto-drawn** using reflection
4. **No editor code needed**!

### Extending the Inspector

To add custom UI for your action type:

```csharp
// In CutsceneActionClipInspector.DrawActionConfiguration()
if (action is TeleportActorAction teleportAction)
{
    DrawTeleportActionUI(teleportAction);
}
```

### Custom Validation Rules

Add to `CutsceneValidationUtility`:

```csharp
private static void ValidateTeleportAction(TeleportActorAction action, ValidationResult result)
{
    if (action.Destination == Vector3.zero)
    {
        result.Warnings.Add("Teleporting to origin - is this intentional?");
    }
}
```

---

## Future Enhancements

Planned features for future versions:

1. **Visual Node Editor**: Graph-based cutscene assembly
2. **Timeline Preview Mode**: Non-destructive action preview
3. **Action Recording**: Record gameplay and convert to cutscene
4. **Template System**: Pre-built cutscene templates (dialogue, boss intro, level transition)
5. **Drag-and-Drop**: Drag actions from library directly into Timeline
6. **Batch Validation**: Validate all cutscenes in project at once
7. **Animation Curve Support**: Ease-in/out for move/rotate actions
8. **Multi-Actor Actions**: Actions that affect multiple actors simultaneously
9. **Conditional Actions**: Execute based on game state
10. **Timeline Markers**: Add comments and notes to Timeline

---

## Summary

The Cutscene Engine editor tooling provides three complementary interfaces:

1. **Custom Clip Inspector** - For precise, per-clip configuration
2. **Cutscene Editor Window** - For rapid, high-level assembly
3. **Action Library** - For reusable, asset-based workflows

Combined, these tools enable designers to create complex, polished cutscenes without writing code, while maintaining flexibility for programmers to extend the system with custom actions and validation rules.

**Recommended Workflow**:
- Use **Cutscene Editor** for initial assembly
- Use **Timeline** for timing and blending
- Use **Custom Inspector** for fine-tuning parameters
- Use **Action Library** for sharing across cutscenes

This multi-level approach scales from simple two-actor dialogues to complex multi-camera cinematic sequences.

---

**Version**: 1.0  
**Last Updated**: February 18, 2026  
**For**: Unity 2022.3+, Timeline, Cutscene Engine 1.0

