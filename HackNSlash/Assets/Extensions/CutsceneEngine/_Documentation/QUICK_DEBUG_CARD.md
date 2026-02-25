# Quick Debugging Reference Card

## "Only firstFrame reset" Issue

### Most Likely Cause (95% of cases)
You're calling `PlayableDirector.Play()` instead of `CutsceneDirector.Play()`.

### How to Check
Look for this log:
```
CutsceneDirector.Play() called
```

**If you DON'T see it**: You're using the wrong Play() method!

### The Fix
```csharp
// Find your script that starts the cutscene
public CutsceneDirector cutsceneDirector; // Not PlayableDirector!

void SomeMethod()
{
    cutsceneDirector.Play(); // Call this!
}
```

---

## Expected Log Sequence

When everything works correctly, you should see:

```
1. CutsceneDirector.Play() called
2. CutsceneDirector: Context built - Motion=True, Animation=True, Camera=...
3. CutsceneDirector: Registered X actors
4. CutsceneDirector: Calling OnCutsceneEnter on ActorName
5. CutsceneDirector: PlayableDirector.Play() called
6. CutscenePlayableBehaviour.OnBehaviourPlay: firstFrame reset  ← You're seeing this
7. ProcessFrame called: playable valid=True, playerData type=...  ← But not this!
8. GetContext: resolver type=PlayableDirector
9. GetContext: director found, name=...
10. GetContext: CutsceneDirector component=True
11. GetContext: Context=True
12. ProcessFrame: context=True
13. ProcessFrame: explicitActor=..., playerData as ICutsceneActor=..., final actor=True
14. ProcessFrame: action type=...
15. ProcessFrame: Calling OnEnter for ...
```

If you're missing steps 1-6, that's your problem!

---

## Common Setup Mistakes

### Mistake 1: No CutsceneDirector Component
**Symptoms**: Only see "firstFrame reset"

**Check**: GameObject with PlayableDirector must also have CutsceneDirector component

**Fix**: Add CutsceneDirector component to same GameObject

---

### Mistake 2: Calling PlayableDirector.Play() Directly
**Symptoms**: Only see "firstFrame reset"

**Check**: Your code calls `playableDirector.Play()` somewhere

**Fix**: Change all calls to `cutsceneDirector.Play()`

---

### Mistake 3: Track Not Bound to Actor
**Symptoms**: See logs up to "ProcessFrame: actor=False"

**Check**: Timeline track binding slot (left side) should show actor

**Fix**: Drag ICutsceneActor GameObject to track binding slot

---

### Mistake 4: Action Reference is Null
**Symptoms**: See "ProcessFrame: action=False"

**Check**: Clip has action assigned

**Fix**: Select clip, assign action in Inspector

---

## Quick Diagnostic

Run this checklist:

1. [ ] See "CutsceneDirector.Play() called" in console
   - If NO: Fix your play script
   
2. [ ] See "Context built - Motion=True..." in console
   - If NO: CutsceneDirector component missing
   
3. [ ] See "ProcessFrame called:" in console
   - If NO: Timeline not starting or clips not set up
   
4. [ ] See "GetContext: Context=True" in console
   - If NO: Context wasn't built (see #1)
   
5. [ ] See "ProcessFrame: actor=True" in console
   - If NO: Track not bound to actor
   
6. [ ] See "ProcessFrame: action=True" in console
   - If NO: Action reference is null in clip
   
7. [ ] See "ProcessFrame: Calling OnEnter" in console
   - If YES: It works! 🎉

---

## Still Stuck?

Share your **full console output** including:
- All logs from pressing Play
- Any warnings or errors (red/yellow)
- The exact sequence of logs you see

This will show exactly where it's failing!

