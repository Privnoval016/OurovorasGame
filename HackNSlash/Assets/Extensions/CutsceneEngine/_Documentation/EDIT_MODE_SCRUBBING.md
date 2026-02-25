# Edit Mode Scrubbing Guide

## What is Edit Mode Scrubbing?

Edit mode scrubbing is when you drag the Timeline playhead **while NOT in Play Mode** to preview your cutscene during editing.

This is different from Play Mode scrubbing (which happens during gameplay).

---

## How It Works Now

### Edit Mode (✅ NOW SUPPORTED)
- Drag Timeline playhead in the editor (not playing)
- Actions update in real-time
- No Context needed (Context will be null - that's okay!)
- Perfect for positioning and timing

### Play Mode (✅ ALREADY WORKED)
- Press Play button
- Call `CutsceneDirector.Play()`
- Full Context available
- Actions work with systems (Animation, Camera, etc.)

---

## Requirements for Edit Mode Scrubbing

### 1. Track Must Be Bound to Actor
In Timeline window, drag your ICutsceneActor GameObject to the track binding slot (left side).

```
CutsceneActionTrack → [Drag Actor Here] ← Binding slot
```

### 2. Actions Must Handle Null Context
Edit mode has no Context (Motion, Animation, Camera systems are null).

**Built-in actions already handle this**:
- MoveActorAction ✅ - Works in edit mode
- RotateActorAction ✅ - Works in edit mode
- PlayAnimationAction ⚠️ - Skips animation in edit mode (can't play without context)

**Your custom actions should too**:
```csharp
public override void OnUpdate(ICutsceneActor actor, CutsceneContext context, float normalizedTime, float deltaTime)
{
    // ✅ Safe - direct transform manipulation
    actor.GetTransform().position = Vector3.Lerp(start, end, normalizedTime);
    
    // ❌ Unsafe - context might be null in edit mode
    // context.Animation.PlayAnimation(actor, "Walk");
    
    // ✅ Safe - null check
    if (context?.Animation != null)
    {
        context.Animation.PlayAnimation(actor, "Walk");
    }
}
```

---

## Testing Edit Mode Scrubbing

### Step 1: Setup
1. Create Timeline with MoveActorAction
2. Set Target position
3. Bind actor to track
4. **DON'T press Play**

### Step 2: Scrub
1. Open Timeline window
2. Drag the playhead left and right
3. Watch the actor in Scene view

### Step 3: Expected Behavior
- Actor should move smoothly between start and target
- Position updates in real-time as you drag
- Console shows minimal logging (only on first frame)

---

## What You Should See in Console

### First Time You Scrub
```
CutscenePlayableBehaviour.OnBehaviourPlay: firstFrame reset
ProcessFrame [firstFrame=True]: actor=True, action=True, context=False, editMode=True
ProcessFrame: Calling OnEnter for MoveActorAction
MoveActorAction OnEnter: start={position}, target={position}
```

### While Scrubbing
(Nothing - OnUpdate doesn't log spam)

### When You Stop Scrubbing
```
CutscenePlayableBehaviour.OnBehaviourPause: Calling OnExit for MoveActorAction
MoveActorAction OnExit: final position={position}
```

---

## Common Issues

### Issue: Actor Doesn't Move When Scrubbing

**Check #1**: Is the track bound?
- Look at Timeline track
- Left side should show actor name
- If it says "None", drag actor GameObject there

**Check #2**: Is the action assigned?
- Select clip in Timeline
- Check Inspector
- Should show action configuration

**Check #3**: Is ProcessFrame being called?
- Check console for "ProcessFrame [firstFrame=True]"
- If missing, Timeline isn't evaluating

### Issue: "SKIPPING - actor=False"

**Cause**: Track not bound to actor

**Fix**: Drag your ICutsceneActor GameObject to the track binding slot

### Issue: Works in Play Mode but Not Edit Mode

**Cause**: Your action requires Context systems

**Fix**: Add null checks for context:
```csharp
// Before (breaks in edit mode)
context.Animation.PlayAnimation(actor, "Walk");

// After (works in both modes)
if (context?.Animation != null)
{
    context.Animation.PlayAnimation(actor, "Walk");
}
```

---

## Actions That Work in Edit Mode

### ✅ Full Support (No Context Needed)
- MoveActorAction - Direct transform manipulation
- RotateActorAction - Direct transform manipulation
- Any action that only touches Transform

### ⚠️ Partial Support (Context Features Disabled)
- PlayAnimationAction - Runs but animation doesn't play
- FocusCameraAction - Runs but camera doesn't focus
- ShakeCameraAction - Runs but shake doesn't happen

These actions still work in Play Mode with full Context!

### Custom Actions
If your action only manipulates transforms → ✅ Full edit mode support
If your action needs systems → ⚠️ Add null checks

---

## Best Practices

### 1. Use Edit Mode Scrubbing for Positioning
Edit mode scrubbing is perfect for:
- Setting exact positions
- Timing actions
- Seeing movement paths
- Adjusting clip durations

### 2. Use Play Mode for Full Testing
Play mode is perfect for:
- Testing animations
- Testing camera movements
- Testing sound effects
- Testing full sequences

### 3. Design Actions to Work in Both Modes
```csharp
public class MyAction : CutsceneActionBase
{
    public Vector3 target;
    private Vector3 start;
    
    public override void OnEnter(ICutsceneActor actor, CutsceneContext context)
    {
        start = actor.GetTransform().position;
        
        // Optional: do something with context if available
        if (context?.Animation != null)
        {
            context.Animation.PlayAnimation(actor, "Walk");
        }
    }
    
    public override void OnUpdate(ICutsceneActor actor, CutsceneContext context, float normalizedTime, float deltaTime)
    {
        // Core functionality works in both modes
        actor.GetTransform().position = Vector3.Lerp(start, target, normalizedTime);
    }
}
```

---

## Workflow Recommendation

1. **Edit Mode**: Position and time your actions
   - Drag playhead to position actors
   - Adjust clip durations
   - See movement paths

2. **Play Mode**: Test full sequence
   - Press Play
   - Let cutscene run
   - Check animations, cameras, sounds

3. **Iterate**: Switch between modes as needed

---

## Summary

✅ **Edit mode scrubbing now works!**
- No need to enter Play Mode
- Drag playhead to see actions update
- Perfect for quick iteration
- Context is null (expected behavior)
- Built-in actions handle this automatically

⚠️ **Context-dependent features disabled in edit mode**
- Animations won't play
- Cameras won't move
- That's normal - they need Play Mode

🎬 **Use the right mode for the right task**
- Edit mode: Positioning & timing
- Play mode: Full testing

Your edit mode scrubbing should now work perfectly! 🎨

