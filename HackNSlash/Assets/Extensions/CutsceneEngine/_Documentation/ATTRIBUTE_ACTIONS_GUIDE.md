# Attribute-Based Cutscene Actions - Complete Guide

## Overview

You can now define **continuous actions** using attributes, just like one-shot actions!

The system automatically detects the execution mode from the attribute and handles everything for you.

---

## Quick Start

### One-Shot Action (Default)
```csharp
[CutsceneAction("Play Sound")]
public void PlaySound(AudioClip clip)
{
    AudioSource.PlayClipAtPoint(clip, transform.position);
}
```

### Continuous Action (New!)
```csharp
[CutsceneAction("Move To", CutsceneActionExecutionMode.OnUpdate)]
public void MoveTo(float normalizedTime, Vector3 target)
{
    // normalizedTime goes from 0.0 to 1.0 over the clip duration
    transform.position = Vector3.Lerp(startPosition, target, normalizedTime);
}
```

That's it! The attribute tells the system when to call your method.

---

## Execution Modes

### OnEnter - One-Shot Actions
Execute **once** when the clip starts.

```csharp
[CutsceneAction("Trigger Event")]
public void TriggerEvent()
{
    Debug.Log("Event triggered!");
}

[CutsceneAction("Enable Component")]
public void EnableComponent(GameObject target)
{
    target.SetActive(true);
}
```

**When to use**:
- Trigger animations
- Play sounds
- Enable/disable objects
- Fire events
- Anything instant

---

### OnUpdate - Continuous Actions
Execute **every frame** while the clip is active.

**IMPORTANT**: First parameter MUST be `float normalizedTime`.

```csharp
[CutsceneAction("Smooth Move", CutsceneActionExecutionMode.OnUpdate)]
public void SmoothMove(float normalizedTime, Vector3 target)
{
    transform.position = Vector3.Lerp(startPos, target, normalizedTime);
}

[CutsceneAction("Rotate Over Time", CutsceneActionExecutionMode.OnUpdate)]
public void RotateOverTime(float normalizedTime, Vector3 eulerAngles)
{
    Quaternion targetRot = Quaternion.Euler(eulerAngles);
    transform.rotation = Quaternion.Slerp(startRot, targetRot, normalizedTime);
}

[CutsceneAction("Fade Material", CutsceneActionExecutionMode.OnUpdate)]
public void FadeMaterial(float normalizedTime, float targetAlpha)
{
    Color color = material.color;
    color.a = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
    material.color = color;
}
```

**When to use**:
- Smooth movement
- Smooth rotation
- Fading effects
- Animated parameters
- Anything that changes over time

**Key Points**:
- ✅ normalizedTime is **automatic** (0.0 to 1.0)
- ✅ Supports **Timeline scrubbing**
- ✅ Respects **clip duration**
- ✅ You can have **other parameters** after normalizedTime

---

### OnExit - Cleanup Actions
Execute **once** when the clip ends.

```csharp
[CutsceneAction("Cleanup", CutsceneActionExecutionMode.OnExit)]
public void Cleanup()
{
    particleSystem.Stop();
    animator.SetBool("IsActing", false);
}

[CutsceneAction("Reset State", CutsceneActionExecutionMode.OnExit)]
public void ResetState(bool resetPosition)
{
    if (resetPosition)
    {
        transform.position = initialPosition;
    }
}
```

**When to use**:
- Stop effects
- Reset state
- Cleanup resources
- Return to idle

---

## Complete Examples

### Example 1: Simple Character Movement

```csharp
using UnityEngine;
using Extensions.CutsceneEngine;

public class CharacterActor : MonoBehaviour, ICutsceneActor
{
    private CutsceneActionAdapter adapter;
    private Vector3 moveStartPosition;
    
    private void Awake()
    {
        InitializeAdapter();
    }
    
    private void InitializeAdapter()
    {
        if (adapter == null)
        {
            adapter = new CutsceneActionAdapter(this);
        }
    }
    
    public CutsceneActionAdapter GetCutsceneAdapter()
    {
        InitializeAdapter();
        return adapter;
    }
    
    public Transform GetTransform() => transform;
    
    public void OnCutsceneEnter()
    {
        // Disable gameplay
    }
    
    public void OnCutsceneExit()
    {
        // Enable gameplay
    }
    
    // ===== CUTSCENE ACTIONS =====
    
    // One-shot: Play animation instantly
    [CutsceneAction("Play Animation")]
    public void PlayAnimation(string animName)
    {
        GetComponent<Animator>().Play(animName);
    }
    
    // Continuous: Move smoothly over clip duration
    [CutsceneAction("Move To Position", CutsceneActionExecutionMode.OnUpdate)]
    public void MoveToPosition(float normalizedTime, Vector3 target)
    {
        // Cache start position on first frame
        if (normalizedTime == 0f)
        {
            moveStartPosition = transform.position;
        }
        
        // Lerp based on normalized time - supports scrubbing!
        transform.position = Vector3.Lerp(moveStartPosition, target, normalizedTime);
    }
    
    // One-shot: Trigger event
    [CutsceneAction("Shout")]
    public void Shout(string message)
    {
        Debug.Log($"{name} shouts: {message}");
        // Play voice line, show subtitle, etc.
    }
}
```

---

### Example 2: VFX Controller

```csharp
using UnityEngine;
using Extensions.CutsceneEngine;

public class VFXActor : MonoBehaviour, ICutsceneActor
{
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private Light spotlight;
    
    private CutsceneActionAdapter adapter;
    private float lightStartIntensity;
    
    // ...ICutsceneActor implementation...
    
    // Start particles (one-shot)
    [CutsceneAction("Start VFX")]
    public void StartVFX()
    {
        particles.Play();
    }
    
    // Fade light over time (continuous)
    [CutsceneAction("Fade Light", CutsceneActionExecutionMode.OnUpdate)]
    public void FadeLight(float normalizedTime, float targetIntensity)
    {
        if (normalizedTime == 0f)
        {
            lightStartIntensity = spotlight.intensity;
        }
        
        spotlight.intensity = Mathf.Lerp(lightStartIntensity, targetIntensity, normalizedTime);
    }
    
    // Stop everything (cleanup)
    [CutsceneAction("Stop VFX", CutsceneActionExecutionMode.OnExit)]
    public void StopVFX()
    {
        particles.Stop();
        spotlight.enabled = false;
    }
}
```

---

### Example 3: Camera Controller

```csharp
using UnityEngine;
using Extensions.CutsceneEngine;

public class CameraActor : MonoBehaviour, ICutsceneActor
{
    private CutsceneActionAdapter adapter;
    private Vector3 moveStart;
    private Quaternion rotateStart;
    
    // ...ICutsceneActor implementation...
    
    // Snap to position (one-shot)
    [CutsceneAction("Snap To")]
    public void SnapTo(Transform target)
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
    }
    
    // Smooth move (continuous)
    [CutsceneAction("Move Camera", CutsceneActionExecutionMode.OnUpdate)]
    public void MoveCamera(float normalizedTime, Vector3 target)
    {
        if (normalizedTime == 0f)
        {
            moveStart = transform.position;
        }
        
        transform.position = Vector3.Lerp(moveStart, target, normalizedTime);
    }
    
    // Smooth rotate (continuous)
    [CutsceneAction("Look At", CutsceneActionExecutionMode.OnUpdate)]
    public void LookAt(float normalizedTime, Transform target)
    {
        if (normalizedTime == 0f)
        {
            rotateStart = transform.rotation;
        }
        
        Quaternion targetRot = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(rotateStart, targetRot, normalizedTime);
    }
    
    // Shake (one-shot, uses coroutine internally - fine for one-shot!)
    [CutsceneAction("Shake")]
    public void Shake(float intensity, float duration)
    {
        StartCoroutine(ShakeCoroutine(intensity, duration));
    }
    
    private System.Collections.IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            transform.position = originalPos + Random.insideUnitSphere * intensity;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPos;
    }
}
```

---

## Advanced Patterns

### Pattern: Caching Start Values

For continuous actions, cache the start value on the first frame:

```csharp
[CutsceneAction("Scale Over Time", CutsceneActionExecutionMode.OnUpdate)]
public void ScaleOverTime(float normalizedTime, float targetScale)
{
    if (normalizedTime == 0f)
    {
        startScale = transform.localScale.x;
    }
    
    float scale = Mathf.Lerp(startScale, targetScale, normalizedTime);
    transform.localScale = Vector3.one * scale;
}
```

### Pattern: Smooth Curves

Use AnimationCurve for non-linear interpolation:

```csharp
[SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

[CutsceneAction("Curved Move", CutsceneActionExecutionMode.OnUpdate)]
public void CurvedMove(float normalizedTime, Vector3 target)
{
    float curvedTime = moveCurve.Evaluate(normalizedTime);
    transform.position = Vector3.Lerp(startPos, target, curvedTime);
}
```

### Pattern: Multiple Parameters

You can have as many parameters as you want after normalizedTime:

```csharp
[CutsceneAction("Complex Action", CutsceneActionExecutionMode.OnUpdate)]
public void ComplexAction(float normalizedTime, Vector3 target, float speed, bool useGravity)
{
    // normalizedTime is automatic
    // target, speed, useGravity are set in the Inspector
    
    float adjustedTime = normalizedTime * speed;
    Vector3 pos = Vector3.Lerp(startPos, target, adjustedTime);
    
    if (useGravity)
    {
        pos.y -= Mathf.Pow(normalizedTime, 2) * 9.81f;
    }
    
    transform.position = pos;
}
```

---

## Parameter Types Supported

| Type | Description | Example |
|------|-------------|---------|
| `int` | Integer values | `42` |
| `float` | Floating point | `3.14f` |
| `bool` | True/false | `true` |
| `string` | Text | `"Hello"` |
| `Vector3` | 3D position/direction | `new Vector3(1, 2, 3)` |
| `GameObject` | Scene reference | Drag from Hierarchy |
| `Enum` | Custom enums | Your enum values |

---

## Comparison: Attribute vs Custom Action

### Use Attributes When:
✅ You have access to the actor's MonoBehaviour  
✅ Action is specific to this actor type  
✅ You want quick iteration  
✅ You want Inspector-friendly parameters  

```csharp
// In your actor class
[CutsceneAction("Custom Move", CutsceneActionExecutionMode.OnUpdate)]
public void CustomMove(float normalizedTime, Vector3 target)
{
    // Direct access to all actor fields
    transform.position = Vector3.Lerp(cachedStart, target, normalizedTime);
}
```

### Use Custom ICutsceneAction When:
✅ Need to work on ANY actor  
✅ Want reusable logic across projects  
✅ Need complex lifecycle management  
✅ Building a library of actions  

```csharp
// In separate action file
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

---

## Rules & Best Practices

### ✅ DO

- **Cache start values** on first frame (normalizedTime == 0f)
- **Use normalizedTime for interpolation** (Lerp, Slerp, etc.)
- **Keep OnUpdate methods lightweight** (called every frame)
- **Use descriptive display names** in attributes
- **Add XML comments** to document your actions

### ❌ DON'T

- **DON'T start coroutines in OnUpdate** (called every frame!)
- **DON'T use GetComponent in OnUpdate** (cache in OnEnter)
- **DON'T forget normalizedTime parameter** for OnUpdate methods
- **DON'T mutate state between frames** (use normalizedTime for determinism)

---

## Inspector Behavior

### One-Shot Actions
```
Method: Play Sound
Parameters:
  └─ clip: [AudioClip field]
```

### Continuous Actions
```
Method: Move To
⏱️ Continuous Action: Executes every frame. First parameter (normalizedTime) is provided automatically.
Parameters:
  └─ target: [Vector3 field]
```

Note: `normalizedTime` is **not shown** in Inspector - it's automatic!

---

## Timeline Workflow

1. **Mark methods** with `[CutsceneAction]`
2. **Open Cutscene Editor** (Window > Cutscene Engine > Cutscene Editor)
3. **Select your actor**
4. **See your actions** in the list
5. **Click "Add to Timeline"**
6. **Inspector shows** execution mode automatically
7. **Set parameters** in Inspector
8. **Adjust clip duration** in Timeline (continuous actions respond!)

---

## Debugging Tips

### Check Execution Mode
```csharp
// In your method
Debug.Log($"Executed at normalizedTime: {normalizedTime}");
```

### Verify Scrubbing
1. Enter Play Mode
2. Pause Timeline
3. Drag playhead back and forth
4. Continuous actions should update in real-time

### Test Different Durations
- 1 second clip
- 5 second clip
- 10 second clip
- Action should scale with duration

---

## Summary

🎯 **Attribute-Based Actions**: Convenient, fast, actor-specific  
🎯 **Execution Modes**: OnEnter (instant), OnUpdate (continuous), OnExit (cleanup)  
🎯 **normalizedTime**: Automatic 0.0 to 1.0 timing  
🎯 **Scrubbing Support**: Continuous actions update in real-time  
🎯 **Inspector Friendly**: All parameters visible and editable  

The attribute system gives you the best of both worlds: convenience AND power! 🚀

