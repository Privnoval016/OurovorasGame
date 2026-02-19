# Cutscene Engine - Track Binding Guide

A common source of confusion: **"How do I add my actor to the Timeline track?"**

This guide explains the Timeline binding system and how to properly connect actors to tracks.

---

## Understanding Timeline Bindings

In Unity Timeline, tracks have **bindings** - references to scene objects that the track will operate on.

Think of it like this:
- **Track** = "I will perform actions on *something*"
- **Binding** = "That *something* is THIS specific GameObject"

---

## The Binding Slot

Every track in Timeline has a **binding slot** on the left side:

```
┌─────────────────────────────────────────────┐
│ [●] CutsceneActionTrack  [None (MonoBehaviour)]│ ← Binding slot
│     ├─ Clip: "Wave Hello"                   │
│     └─ Clip: "Say Something"                │
└─────────────────────────────────────────────┘
```

That slot is where you assign your actor!

---

## How to Bind an Actor to a Track

### Method 1: Drag and Drop (Easiest)

1. **Open Timeline Window**
   - Select your PlayableDirector GameObject
   - Timeline window should open automatically
   - If not: `Window > Sequencing > Timeline`

2. **Add a Cutscene Action Track**
   - Right-click in Timeline
   - `Extensions > Cutscene Engine > Cutscene Action Track`
   - A new track appears

3. **Drag Your Actor to the Track**
   - Find your actor GameObject in Hierarchy (e.g., "Player", "Enemy")
   - Drag it onto the binding slot (left side of track)
   - The slot should now show the actor's name

Done! The track is now bound to that actor.

---

### Method 2: Use Cutscene Editor Window (Fastest)

1. **Open Cutscene Editor**
   - `Window > Cutscene Engine > Cutscene Editor`

2. **Select Your Director**
   - Click "Find Director in Scene"
   - Or drag your PlayableDirector to the field

3. **Add Action from Scene Actor**
   - Go to "Scene Actors" tab
   - Click on your actor in the list
   - Click "Add to Timeline" for any action
   - **The track is created AND bound automatically!**

This method is recommended because it:
- Creates the track for you
- Binds the actor automatically
- Adds the clip in one step
- No manual binding needed

---

### Method 3: Manual Inspector Assignment

1. **Select the Track**
   - Click on the track name in Timeline

2. **Look at Inspector**
   - You'll see "Track Binding" field
   - It shows the type: `(MonoBehaviour)`

3. **Assign the Actor**
   - Drag actor GameObject to this field
   - Or click the circle icon to open object picker

---

## What Can Be Bound?

The `CutsceneActionTrack` accepts:
- ✅ Any MonoBehaviour that implements `ICutsceneActor`
- ❌ GameObjects directly (won't work - must be a component)
- ❌ Non-MonoBehaviour objects

### Valid Examples:
```csharp
// ✅ These can be bound to tracks
public class PlayerActor : MonoBehaviour, ICutsceneActor { ... }
public class EnemyActor : MonoBehaviour, ICutsceneActor { ... }
public class NPCActor : MonoBehaviour, ICutsceneActor { ... }
```

### Invalid Examples:
```csharp
// ❌ These CANNOT be bound
public class GameManager { ... }  // Not a MonoBehaviour
public class PlayerData : ScriptableObject { ... }  // Not in scene
```

---

## Verifying the Binding

### Visual Check in Timeline

Look at the track:
```
Good:
[●] CutsceneActionTrack  [PlayerActor (CutsceneActorProxy)]
                          ↑ Shows the bound component

Bad:
[●] CutsceneActionTrack  [None (MonoBehaviour)]
                          ↑ Nothing bound!
```

### Inspector Check

Select any clip on the track and check the Inspector:

**If Bound Correctly**:
```
╔════════════════════════════════════╗
║ Cutscene Action Configuration      ║
╠════════════════════════════════════╣
║ Bound Actor                        ║
║ ┌────────────────────────────────┐ ║
║ │ Actor: PlayerActor             │ ║
║ │ Available Actions: 5           │ ║
║ └────────────────────────────────┘ ║
╚════════════════════════════════════╝
```

**If NOT Bound**:
```
╔════════════════════════════════════╗
║ Cutscene Action Configuration      ║
╠════════════════════════════════════╣
║ ⚠ No actor bound to track.        ║
║   Assign an actor in the Timeline  ║
║   track binding.                   ║
╚════════════════════════════════════╝
```

---

## Common Binding Issues

### Issue: "Cannot Configure Actor Method Action: No Actor Bound to Track"

**Cause**: Track has no binding assigned.

**Solution**: Follow Method 1 above to drag actor to track binding slot.

---

### Issue: "Type mismatch: Cannot bind [X] to track"

**Cause**: Trying to bind wrong component type.

**Solution**: 
- Ensure the component implements `ICutsceneActor`
- Don't drag the GameObject - drag the component
- Check that actor script is on the GameObject

---

### Issue: "Binding disappears when entering Play Mode"

**Cause**: Actor GameObject is instantiated at runtime, not in scene.

**Solution**: 
- Bindings must reference scene objects
- If actor spawns at runtime, use `explicitTarget` field on clip instead
- Or bind to a persistent placeholder actor

---

### Issue: "Multiple actors on same GameObject"

**Scenario**: GameObject has multiple ICutsceneActor components.

**Solution**: Timeline will bind to the first component found. To control which:
1. Keep one actor per GameObject (recommended)
2. Or use `explicitTarget` field on clips to override

---

## Advanced: Explicit Target Override

Sometimes you want a clip to target a different actor than the track binding.

**Example Use Case**: Boss cutscene where most clips target boss, but one clip makes player react.

**How to Override**:
1. Select the specific clip
2. In Inspector, find "Explicit Target" field
3. Drag the override actor to this field
4. This clip now targets the override, others use track binding

```
Track: BossActor (main binding)
├─ Clip 1: Boss taunts    [uses track binding: BossActor]
├─ Clip 2: Player reacts  [explicit target: PlayerActor] ← Override!
└─ Clip 3: Boss attacks   [uses track binding: BossActor]
```

---

## Best Practices

### 1. One Track Per Actor
```
Good:
├─ PlayerTrack    [bound to PlayerActor]
├─ EnemyTrack     [bound to EnemyActor]
└─ CameraTrack    [bound to CameraActor]

Bad:
└─ ActionsTrack   [bound to PlayerActor, but has enemy clips too]
```

### 2. Name Tracks After Actors
```
Good: "PlayerActor Track"
Bad: "Track 1"
```
Right-click track → Rename

### 3. Use Cutscene Editor Window
Fastest workflow that avoids binding issues entirely.

### 4. Verify Before Testing
Before pressing Play:
- Check all tracks have bindings
- Click each clip to verify Inspector shows "Bound Actor"

---

## Workflow Checklist

Before you can add actions:
- [ ] Actor script implements `ICutsceneActor`
- [ ] Actor GameObject exists in scene
- [ ] Timeline created (PlayableDirector + Timeline asset)
- [ ] CutsceneDirector component on director GameObject
- [ ] Track added to Timeline
- [ ] **Actor bound to track** ← THIS STEP!
- [ ] Clips added to track

If any clip shows "No actor bound", revisit this checklist.

---

## Quick Reference

| Action | Keyboard | Result |
|--------|----------|--------|
| Open Timeline | `Ctrl+6` (Win) / `Cmd+6` (Mac) | Opens Timeline window |
| Add Track | Right-click Timeline → Extensions → CutsceneEngine | Adds CutsceneActionTrack |
| Bind Actor | Drag GameObject to track binding slot | Links actor to track |
| Verify Binding | Click track, check Inspector | Shows bound object |
| Open Cutscene Editor | Window → Cutscene Engine → Cutscene Editor | Auto-binds actors |

---

## Summary

**The Key Rule**: Every CutsceneActionTrack MUST have an actor bound to it.

**The Easiest Way**: Use the Cutscene Editor Window - it handles bindings automatically.

**The Manual Way**: Drag your actor GameObject onto the track binding slot in Timeline.

**How to Verify**: Select any clip and check Inspector shows "Bound Actor" section.

If you see "No actor bound to track", you forgot to bind! 

🎬 Happy cutscene authoring!

