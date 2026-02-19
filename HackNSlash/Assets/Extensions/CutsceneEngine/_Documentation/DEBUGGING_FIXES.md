# Cutscene Engine - Debugging & Fixes Applied

## 🚨 CRITICAL: If You Only See "firstFrame reset"

**This is the most common issue!**

**The Problem**: You're calling `PlayableDirector.Play()` directly instead of `CutsceneDirector.Play()`.

**Why it fails**: 
- `CutsceneDirector.Play()` builds the Context
- `PlayableDirector.Play()` just starts Timeline
- Without Context, all actions fail null checks

**The Fix**:
```csharp
// ❌ WRONG - This won't work
public PlayableDirector playableDirector;

void Start()
{
    playableDirector.Play(); // Context is null!
}

// ✅ CORRECT - This works
public CutsceneDirector cutsceneDirector;

void Start()
{
    cutsceneDirector.Play(); // Context is built first!
}
```

**Quick Test**: After you see "firstFrame reset", check console for:
- `CutsceneDirector.Play() called` → If missing, you're not calling the right method!
- `GetContext: Context=False` → Context wasn't built

**Once you fix this, you should see**:
```
CutsceneDirector.Play() called
CutsceneDirector: Context built - Motion=True, Animation=True, Camera=...
ProcessFrame called: playable valid=True, playerData type=...
ProcessFrame: Calling OnEnter for ActorMethodAction
ActorMethodAction.OnEnter: method=..., mode=..., resolved=True
```

---

## Issues Fixed

### Issue 1: Timeline Scrubbing Not Working on MoveActorAction
**Problem**: When scrubbing the Timeline playhead, MoveActorAction didn't update the actor's position.

**Root Cause**: 
- The `startPosition` field was not marked with `[System.NonSerialized]`
- When scrubbing jumps around, the start position was getting lost
- OnEnter wasn't being called reliably during scrubbing

**Fix Applied**:
1. Added `hasInitialized` flag to track if start position is cached
2. OnUpdate now checks if initialized and sets start position if not
3. This ensures scrubbing works even if OnEnter is skipped
4. Added debug logging to trace execution

**Test**:
1. Create a MoveActorAction clip in Timeline
2. Enter Play Mode
3. Pause Timeline
4. Drag playhead back and forth
5. Actor should smoothly move between positions

---

### Issue 2: Instantaneous CutsceneActions Not Executing (No Debug.Log)
**Problem**: One-shot actions marked with `[CutsceneAction]` weren't being called.

**Root Cause**:
- `OnBehaviourPause` only called `OnExit` if `explicitActor` was set
- Track-bound actors weren't being stored for OnExit callback
- Context might be null

**Fix Applied**:
1. Added `cachedActor` field to store actor from ProcessFrame
2. OnBehaviourPause now uses cached actor
3. Added comprehensive debug logging throughout
4. Fixed null checks in all lifecycle methods

**Test**:
```csharp
[CutsceneAction("Test Action")]
public void TestAction()
{
    Debug.Log("This should print!");
}
```

1. Add clip with this action
2. Play Timeline
3. You should see the debug log

---

## Debug Logging Added (Updated)

### CutsceneDirector
```
CutsceneDirector.Play() called
CutsceneDirector: Context built - Motion={bool}, Animation={bool}, Camera={bool}
CutsceneDirector: Registered {count} actors
CutsceneDirector: Calling OnCutsceneEnter on {actorName}
CutsceneDirector: PlayableDirector.Play() called
```

### CutscenePlayableBehaviour
```
CutscenePlayableBehaviour.OnBehaviourPlay: firstFrame reset
ProcessFrame called: playable valid={bool}, playerData type={type}
ProcessFrame: context={bool}
ProcessFrame: explicitActor={bool}, playerData as ICutsceneActor={bool}, final actor={bool}
ProcessFrame: action type={actionType}
ProcessFrame: Calling OnEnter for {action}
CutscenePlayableBehaviour.OnBehaviourPause: Calling OnExit for {action}
```

### GetContext
```
GetContext: resolver type={type}
GetContext: director found, name={name}
GetContext: CutsceneDirector component={bool}
GetContext: Context={bool}
```

### MoveActorAction
```
MoveActorAction OnEnter: start={position}, target={position}
MoveActorAction OnExit: final position={position}
```

### ActorMethodAction
```
ActorMethodAction.OnEnter: method={name}, mode={mode}, resolved={bool}
ActorMethodAction.InvokeMethod: Invoking {method} with {count} args, mode={mode}
```

---

## What to Look For Based on Your Issue

### If you only see "firstFrame reset":

**This means**: `OnBehaviourPlay` is called but `ProcessFrame` never executes or fails immediately.

**Check for these logs**:
1. `ProcessFrame called:` - If missing, Timeline isn't calling ProcessFrame at all
2. `GetContext: resolver type=` - Shows if we can access the PlayableDirector
3. `GetContext: CutsceneDirector component=False` - Most common issue
4. `ProcessFrame: SKIPPING` - Shows which null check failed

**Common causes**:
- Not calling `CutsceneDirector.Play()` (calling `PlayableDirector.Play()` directly instead)
- CutsceneDirector component missing from GameObject
- Context is null because Play() wasn't called

**Solution**: 
```csharp
// ❌ DON'T DO THIS
playableDirector.Play();

// ✅ DO THIS
cutsceneDirector.Play(); // This builds Context first
```

### If you see "ProcessFrame: SKIPPING":

Look at what's null:
- `context=False` → Context not built, call CutsceneDirector.Play()
- `actor=False` → Track not bound to ICutsceneActor
- `action=False` → Action reference is null in clip

---

## How to Debug Your Actions

### 1. Check if Action is Being Called

Add debug log to your action:
```csharp
[CutsceneAction("My Action")]
public void MyAction()
{
    Debug.Log("MyAction called!");
    // Your code here
}
```

### 2. Check Execution Mode

For continuous actions:
```csharp
[CutsceneAction("Continuous Action", CutsceneActionExecutionMode.OnUpdate)]
public void ContinuousAction(float normalizedTime)
{
    Debug.Log($"ContinuousAction called with normalizedTime={normalizedTime}");
    // Your code here
}
```

### 3. Check Timeline Setup

**Console Warnings to Look For**:
- `Skipping - context=false` → CutsceneDirector not configured
- `actor=false` → Track not bound to actor
- `action=false` → Action reference is null

**Fix**:
1. Ensure GameObject has `CutsceneDirector` component
2. Ensure track is bound to ICutsceneActor GameObject
3. Ensure clip has action assigned

### 4. Check Actor Binding

```csharp
public class MyActor : MonoBehaviour, ICutsceneActor
{
    private CutsceneActionAdapter adapter;
    
    private void Awake()
    {
        InitializeAdapter();
        Debug.Log($"MyActor initialized: {name}");
    }
    
    private void InitializeAdapter()
    {
        if (adapter == null)
        {
            adapter = new CutsceneActionAdapter(this);
            Debug.Log($"Adapter initialized for {name}");
        }
    }
    
    public CutsceneActionAdapter GetCutsceneAdapter()
    {
        InitializeAdapter();
        return adapter;
    }
    
    // ...rest of implementation...
}
```

---

## Common Issues & Solutions

### Issue: "Cannot call OnExit - actor=false"

**Cause**: Actor wasn't cached from ProcessFrame

**Solution**: This is now fixed - actor is cached when ProcessFrame runs

### Issue: "Skipping - context=false"

**Cause**: CutsceneDirector.Context is null

**Solution**:
1. Ensure CutsceneDirector component exists on same GameObject as PlayableDirector
2. Call `CutsceneDirector.Play()` instead of `PlayableDirector.Play()`
3. Context is built when cutscene starts

### Issue: Scrubbing doesn't update position

**Cause**: Start position lost during scrubbing

**Solution**: Now fixed - MoveActorAction re-initializes if needed in OnUpdate

### Issue: Action executes but does nothing

**Cause**: Execution mode mismatch

**Check**:
```csharp
// For one-shot:
[CutsceneAction("One Shot")] // Defaults to OnEnter
public void OneShot() { }

// For continuous:
[CutsceneAction("Continuous", CutsceneActionExecutionMode.OnUpdate)]
public void Continuous(float normalizedTime) { } // MUST have normalizedTime parameter
```

---

## Verification Checklist

Before reporting an issue, verify:

- [ ] CutsceneDirector component on PlayableDirector GameObject
- [ ] Track bound to ICutsceneActor GameObject
- [ ] Actor has CutsceneActionAdapter initialized
- [ ] Action has correct execution mode
- [ ] Continuous actions have `float normalizedTime` first parameter
- [ ] Debug logs show up in Console (check log filtering)
- [ ] Timeline is playing (not paused unless testing scrubbing)

---

## Testing Scrubbing

### Manual Test
1. Enter Play Mode
2. Open Timeline window
3. Click play button in Timeline
4. Click pause button
5. Drag playhead left and right
6. **Expected**: Actor position updates in real-time

### Debug Scrubbing
Enable debug logs and check for:
```
// When scrubbing backward:
MoveActorAction OnEnter: start={pos}, target={pos}
// Multiple OnUpdate calls as you drag
// No OnExit (only when clip ends)
```

---

## Performance Note

Debug logs are **temporary** for debugging. Remove them in production:

**To Disable Debug Logs**:
1. Comment out Debug.Log lines in:
   - CutscenePlayableBehaviour.cs
   - ActorMethodAction.cs
   - MoveActorAction.cs
2. Or wrap in `#if UNITY_EDITOR` blocks:

```csharp
#if UNITY_EDITOR
Debug.Log("Debug message");
#endif
```

---

## Summary of Changes

| File | Change | Purpose |
|------|--------|---------|
| MoveActorAction.cs | Added hasInitialized flag | Fix scrubbing |
| MoveActorAction.cs | Check in OnUpdate | Re-init if needed |
| MoveActorAction.cs | Added debug logs | Trace execution |
| ActorMethodAction.cs | Added debug logs | Trace invocation |
| CutscenePlayableBehaviour.cs | Added cachedActor | Fix OnExit callback |
| CutscenePlayableBehaviour.cs | Added debug logs | Trace lifecycle |

All changes are **backwards compatible** - existing cutscenes will work without modification.

---

## Next Steps

1. **Test your cutscenes** - Check console for debug logs
2. **Verify scrubbing** - Drag playhead and watch actor move
3. **Check one-shot actions** - Ensure Debug.Logs appear
4. **Report back** - Share console output if issues persist

The system should now work correctly with full scrubbing support! 🎬



