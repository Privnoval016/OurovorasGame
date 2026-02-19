# Cutscene Engine - Major Update: Scrubbing & Duration Support

## What Changed

The cutscene system has been redesigned to properly support:
1. **Timeline Scrubbing** - Drag the playhead and actions update in real-time
2. **Clip Duration Respect** - Actions now use the clip's duration instead of ignoring it
3. **Continuous vs One-Shot Actions** - Clear distinction between action types

---

## New Action Lifecycle

### Old System (Broken)
```csharp
public interface ICutsceneAction
{
    void Execute(ICutsceneActor actor, CutsceneContext context); // Called once, ignored duration
}
```

**Problems**:
- Executed once on first frame, then stopped
- Couldn't respond to Timeline scrubbing
- Duration in Timeline was meaningless
- Move/Rotate actions started coroutines that Timeline couldn't control

### New System (Fixed)
```csharp
public interface ICutsceneAction
{
    void OnEnter(ICutsceneActor actor, CutsceneContext context);
    void OnUpdate(ICutsceneActor actor, CutsceneContext context, float normalizedTime, float deltaTime);
    void OnExit(ICutsceneActor actor, CutsceneContext context);
}
```

**Benefits**:
- `OnEnter`: Initialize when clip starts
- `OnUpdate`: Update every frame with normalized time (0.0 to 1.0)
- `OnExit`: Clean up when clip ends
- Scrubbing works - drag playhead and see updates
- Clip duration matters - adjust in Timeline, action responds

---

## How to Update Your Actions

### Base Class: CutsceneActionBase

For convenience, inherit from `CutsceneActionBase` to get default implementations:

```csharp
public abstract class CutsceneActionBase : ICutsceneAction
{
    public virtual void OnEnter(ICutsceneActor actor, CutsceneContext context) { }
    public virtual void OnUpdate(ICutsceneActor actor, CutsceneContext context, float normalizedTime, float deltaTime) { }
    public virtual void OnExit(ICutsceneActor actor, CutsceneContext context) { }
}
```

Only override the methods you need!

---

## Action Patterns

### Pattern 1: One-Shot Actions (OnEnter only)

Use for actions that happen instantly:
- Trigger animation
- Play sound
- Enable/disable component

```csharp
[Serializable]
public class PlaySoundAction : CutsceneActionBase
{
    public AudioClip Sound;
    
    public override void OnEnter(ICutsceneActor actor, CutsceneContext context)
    {
        AudioSource.PlayClipAtPoint(Sound, actor.GetTransform().position);
    }
}
```

---

### Pattern 2: Continuous Interpolation (OnUpdate only)

Use for smooth transitions over clip duration:
- Move from A to B
- Rotate from A to B
- Fade from A to B

```csharp
[Serializable]
public class MoveActorAction : CutsceneActionBase
{
    public Vector3 Target;
    private Vector3 startPosition;
    
    public override void OnEnter(ICutsceneActor actor, CutsceneContext context)
    {
        startPosition = actor.GetTransform().position;
    }
    
    public override void OnUpdate(ICutsceneActor actor, CutsceneContext context, float normalizedTime, float deltaTime)
    {
        // Lerp based on normalizedTime - supports scrubbing!
        actor.GetTransform().position = Vector3.Lerp(startPosition, Target, normalizedTime);
    }
    
    public override void OnExit(ICutsceneActor actor, CutsceneContext context)
    {
        // Ensure exact final position
        actor.GetTransform().position = Target;
    }
}
```

---

### Pattern 3: Full Lifecycle (OnEnter + OnUpdate + OnExit)

Use for complex actions with state:
- Start animation, update blend, stop animation
- Activate system, update parameters, deactivate system

```csharp
[Serializable]
public class ChargeAttackAction : CutsceneActionBase
{
    public float MaxCharge = 100f;
    private ParticleSystem chargeVFX;
    
    public override void OnEnter(ICutsceneActor actor, CutsceneContext context)
    {
        chargeVFX = actor.GetTransform().GetComponentInChildren<ParticleSystem>();
        chargeVFX?.Play();
        context.Animation.PlayAnimation(actor, "ChargeStart");
    }
    
    public override void OnUpdate(ICutsceneActor actor, CutsceneContext context, float normalizedTime, float deltaTime)
    {
        // Update charge level based on time
        float currentCharge = MaxCharge * normalizedTime;
        // Update VFX intensity, shader parameters, etc.
    }
    
    public override void OnExit(ICutsceneActor actor, CutsceneContext context)
    {
        chargeVFX?.Stop();
        context.Animation.PlayAnimation(actor, "ChargeRelease");
    }
}
```

---

## Updated Built-in Actions

All built-in actions have been updated:

| Action | Pattern | Scrubbing Support |
|--------|---------|-------------------|
| MoveActorAction | Continuous | ✅ Yes - interpolates position |
| RotateActorAction | Continuous | ✅ Yes - interpolates rotation |
| PlayAnimationAction | One-Shot | N/A - triggers animation |
| FocusCameraAction | One-Shot | N/A - focuses camera |
| MoveCameraAction | Continuous | ✅ Yes - interpolates camera |
| ShakeCameraAction | One-Shot | N/A - triggers shake |

---

## Actor Method Actions (User Methods)

**UPDATED**: User methods marked with `[CutsceneAction]` can now be **continuous OR one-shot**!

### One-Shot Actions (Default)

```csharp
[CutsceneAction("Play Sound")]
public void PlaySound(AudioClip clip)
{
    AudioSource.PlayClipAtPoint(clip, transform.position);
}
```

Executes **once** when the clip starts.

### Continuous Actions (New!)

```csharp
[CutsceneAction("Move To", CutsceneActionExecutionMode.OnUpdate)]
public void MoveTo(float normalizedTime, Vector3 target)
{
    // normalizedTime is automatic (0.0 to 1.0)
    transform.position = Vector3.Lerp(startPos, target, normalizedTime);
}
```

Executes **every frame** - supports scrubbing and respects clip duration!

**Key Points**:
- First parameter MUST be `float normalizedTime` for OnUpdate methods
- normalizedTime is provided automatically (not shown in Inspector)
- You can have additional parameters after normalizedTime
- Supports Timeline scrubbing out of the box

### Cleanup Actions

```csharp
[CutsceneAction("Stop Effects", CutsceneActionExecutionMode.OnExit)]
public void StopEffects()
{
    particleSystem.Stop();
}
```

Executes **once** when clip ends.

### If You Need Even More Control

If attributes aren't flexible enough, you can still create custom `ICutsceneAction` classes.

### If You Need Even More Control

If attributes aren't flexible enough, you can still create custom `ICutsceneAction` classes:

```csharp
// ✅ For reusable, system-agnostic actions
[Serializable]
public class GenericMoveAction : CutsceneActionBase
{
    public Vector3 Target;
    private Vector3 start;
    
    public override void OnEnter(ICutsceneActor actor, CutsceneContext context)
    {
        start = actor.GetTransform().position;
    }
    
    public override void OnUpdate(ICutsceneActor actor, CutsceneContext context, float normalizedTime, float deltaTime)
    {
        actor.GetTransform().position = Vector3.Lerp(start, Target, normalizedTime);
    }
}
```

**See**: ATTRIBUTE_ACTIONS_GUIDE.md for complete examples and patterns.

---

## Normalized Time Explanation

`normalizedTime` goes from **0.0 to 1.0** over the clip's duration:

```
Timeline Clip: [========================================] 5 seconds
normalizedTime: 0.0 -----> 0.5 -----> 1.0

At 0 seconds: normalizedTime = 0.0
At 2.5 seconds: normalizedTime = 0.5
At 5 seconds: normalizedTime = 1.0
```

**Why it's powerful**:
- Same code works for any clip duration
- Scrubbing just changes normalizedTime
- Easy to interpolate: `Lerp(start, end, normalizedTime)`

---

## Scrubbing Support

### What is Scrubbing?
Dragging the Timeline playhead while paused to preview the cutscene.

### How Actions Support It

**Continuous Actions** (OnUpdate):
- Update based on normalizedTime
- When you drag playhead, normalizedTime changes
- Action updates immediately
- **Result**: Smooth real-time preview!

**One-Shot Actions** (OnEnter):
- Execute once when playhead crosses clip start
- Don't respond to scrubbing within the clip
- **Result**: See the initial state, but not continuous changes

---

## Migration Guide

### If You Had Custom Actions

1. **Inherit from CutsceneActionBase** instead of ICutsceneAction
2. **Rename Execute → OnEnter** (for one-shot actions)
3. **Or use OnUpdate** (for continuous actions)

Before:
```csharp
public class MyAction : ICutsceneAction
{
    public void Execute(ICutsceneActor actor, CutsceneContext context)
    {
        // One-shot logic
    }
}
```

After (One-Shot):
```csharp
public class MyAction : CutsceneActionBase
{
    public override void OnEnter(ICutsceneActor actor, CutsceneContext context)
    {
        // Same one-shot logic
    }
}
```

After (Continuous):
```csharp
public class MyAction : CutsceneActionBase
{
    public override void OnUpdate(ICutsceneActor actor, CutsceneContext context, float normalizedTime, float deltaTime)
    {
        // Continuous logic using normalizedTime
    }
}
```

---

## Context Systems (Motion, Animation, Camera)

**Important Note**: The old `IMotionSystem` and related interfaces are now **OPTIONAL**.

You don't need to use them anymore! The new system directly manipulates transforms in actions.

### Old Way (Still Works)
```csharp
public override void OnEnter(ICutsceneActor actor, CutsceneContext context)
{
    context.Motion.Move(actor, target, duration); // Uses IMotionSystem
}
```

### New Way (Recommended)
```csharp
public override void OnUpdate(ICutsceneActor actor, CutsceneContext context, float normalizedTime, float deltaTime)
{
    actor.GetTransform().position = Vector3.Lerp(start, target, normalizedTime);
}
```

**Why the new way is better**:
- Direct control - no abstraction layer
- Works with scrubbing out of the box
- No coroutines - Timeline manages everything
- Simpler to understand and debug

---

## Testing Your Actions

### Test Scrubbing
1. Create a cutscene with your action
2. Enter Play Mode
3. **Pause** the Timeline
4. **Drag** the playhead back and forth
5. **Verify** the action updates in real-time

### Test Duration
1. Create a clip with your action
2. Set duration to 2 seconds → test
3. Set duration to 10 seconds → test
4. Action should take the full duration regardless

---

## Common Questions

### Q: Do I need to implement all three methods?
**A**: No! Inherit from `CutsceneActionBase` and only override what you need.

### Q: Can I still use [CutsceneAction] methods?
**A**: Yes! They work as one-shot actions (OnEnter behavior).

### Q: What if I need a coroutine?
**A**: Don't use coroutines. Use OnUpdate with deltaTime instead.

### Q: Can I mix one-shot and continuous in one action?
**A**: Yes! Override OnEnter (initialize) and OnUpdate (animate).

### Q: Does this break existing cutscenes?
**A**: No - one-shot actions still work, they just use OnEnter now.

---

## Performance Notes

- **OnUpdate** is called **every frame** while the clip is active
- Keep OnUpdate logic lightweight
- Cache references in OnEnter, use them in OnUpdate
- Avoid `GetComponent` calls in OnUpdate

---

## Summary

✅ **Scrubbing works** - Drag playhead to preview  
✅ **Duration matters** - Clip length controls action length  
✅ **Clear patterns** - One-shot, Continuous, or Full lifecycle  
✅ **No coroutines** - Timeline manages everything  
✅ **Backwards compatible** - Existing actions still work  

The cutscene system is now production-ready for timeline-driven animations! 🎬

