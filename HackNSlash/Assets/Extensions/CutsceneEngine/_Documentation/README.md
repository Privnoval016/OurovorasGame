# Cutscene Engine

A powerful, reflection-based cutscene authoring system that integrates seamlessly with Unity Timeline to create cinematic sequences without writing code.

## Table of Contents

1. [Overview](#overview)
2. [Core Architecture](#core-architecture)
3. [System Components](#system-components)
4. [Action System](#action-system)
5. [Timeline Integration](#timeline-integration)
6. [Extensibility](#extensibility)
7. [Usage Guide](#usage-guide)
8. [Missing Features & TODOs](#missing-features--todos)

---

## Overview

The Cutscene Engine transforms Unity Timeline into a fast authoring surface for designers, enabling the creation of cinematic or gameplay-interrupt cutscenes without code. The system uses **reflection-based action discovery**, **interface-driven system abstractions**, and **Timeline integration** to provide a flexible, data-driven workflow.

### Key Features

- **Reflection-Based Action Discovery**: Methods marked with `[CutsceneAction]` are automatically discovered and exposed in the editor
- **System-Agnostic**: Works with any motion system (NavMesh, Transform, CharacterController), animation system (Animator, Animancer), or camera system (Cinemachine, custom)
- **Timeline Integration**: Native Timeline tracks and clips for precise timing and blending
- **Actor Lifecycle Management**: Automatic enter/exit hooks ensure gameplay systems pause and resume correctly
- **Reusable Actions**: Actions can be authored inline or saved as assets for reuse across cutscenes
- **Type-Safe Parameters**: Auto-generated UI fields based on method signatures
- **No Code Required**: Designers can assemble complex sequences without touching C#

---

## Core Architecture

### Data Flow

```
CutsceneDirector (PlayableDirector wrapper)
    ↓
CutsceneContext (system references: Motion, Animation, Camera)
    ↓
CutsceneActionTrack (Timeline track, bound to ICutsceneActor)
    ↓
CutsceneActionClip (Timeline clip, contains CutsceneActionReference)
    ↓
CutscenePlayableBehaviour (executes action during playback)
    ↓
ICutsceneAction.Execute() (invokes logic on actor)
    ↓
IMotionSystem / IAnimationSystem / ICameraSystem
```

### Key Interfaces

```csharp
ICutsceneActor
  ├─ ICutsceneOverridable
  │    ├─ GetCutsceneAdapter() → CutsceneActionAdapter
  │    ├─ OnCutsceneEnter()
  │    └─ OnCutsceneExit()
  └─ GetTransform() → Transform

ICutsceneAction
  └─ Execute(ICutsceneActor actor, CutsceneContext context)

IMotionSystem
  ├─ Move(actor, target, duration)
  └─ Rotate(actor, rotation, duration)

IAnimationSystem
  └─ PlayAnimation(actor, id)

ICameraSystem
  ├─ FocusOn(target)
  ├─ MoveTo(position, duration)
  └─ Shake(intensity, duration)
```

---

## System Components

### 1. CutsceneDirector

**Purpose**: Main entry point for cutscene playback. Wraps Unity's `PlayableDirector` and manages actor lifecycle.

**Responsibilities**:
- Build `CutsceneContext` with system references
- Register all actors in the Timeline
- Call `OnCutsceneEnter()` on all actors when cutscene starts
- Call `OnCutsceneExit()` on all actors when cutscene ends
- Manage `CutsceneRuntime.IsCutsceneActive` state

**Key Methods**:
- `Play()` - Starts cutscene, enters actors, builds context
- `Stop()` - Stops cutscene, exits actors, cleans up
- `RegisterActors()` - Discovers actors from Timeline bindings

**Usage**:
```csharp
// Attach to GameObject with PlayableDirector
public CutsceneDirector director;
director.Play();  // Start cutscene
director.Stop();  // Force stop cutscene
```

---

### 2. CutsceneContext

**Purpose**: Provides centralized access to game systems during cutscene execution.

**Responsibilities**:
- Hold references to motion, animation, and camera systems
- Pass system references to actions for execution
- Enable system-agnostic action implementation

**Properties**:
- `Motion` - `IMotionSystem` for moving/rotating actors
- `Animation` - `IAnimationSystem` for playing animations
- `Camera` - `ICameraSystem` for camera control

**Built By**: `CutsceneContextBuilder.Build()`

---

### 3. CutsceneContextBuilder

**Purpose**: Factory for creating `CutsceneContext` with appropriate system implementations.

**Current Implementation**:
```csharp
public static CutsceneContext Build(MonoBehaviour runner, CinemachineCamera cam = null)
{
    return new CutsceneContext
    {
        Motion = new TransformMotionSystem(runner),
        Animation = new AnimatorAnimationSystem(),
        Camera = cam ? new CinemachineCameraSystem(runner, cam) : null
    };
}
```

**Customization**: Modify this to swap in different system implementations (e.g., `NavMeshMotionSystem` instead of `TransformMotionSystem`).

---

### 4. CutsceneRuntime

**Purpose**: Global state tracker for cutscene playback.

**Properties**:
- `IsCutsceneActive` - `bool` indicating if a cutscene is currently playing

**Methods**:
- `BeginCutscene()` - Set active state to `true`
- `EndCutscene()` - Set active state to `false`

**Usage**:
```csharp
// In gameplay code
if (CutsceneRuntime.IsCutsceneActive)
{
    // Disable player input
}
```

---

### 5. CutsceneActionAdapter

**Purpose**: Reflection-based method invoker for actor actions.

**Responsibilities**:
- Discover methods marked with `[CutsceneAction]` attribute
- Cache method info in a dictionary (`DisplayName` → `MethodInfo`)
- Invoke methods dynamically with provided arguments

**Key Methods**:
- `GetActionNames()` - Returns all available action names for UI population
- `GetMethod(string actionName)` - Returns `MethodInfo` for a specific action
- `Invoke(string actionName, object[] args)` - Invokes action with arguments

**How It Works**:
```csharp
// On actor
[CutsceneAction("Jump", allowDuringGameplay = true)]
public void PerformJump(float height)
{
    // Jump logic
}

// Adapter usage
adapter.Invoke("Jump", new object[] { 5f });
```

---

### 6. CutsceneReflectionUtility

**Purpose**: Static utility for reflection operations.

**Key Method**:
```csharp
GetCutsceneActions(object target) → Dictionary<string, MethodInfo>
```

**Process**:
1. Get all methods (public, private, instance) on target
2. Filter methods with `[CutsceneAction]` attribute
3. Map `DisplayName` → `MethodInfo`
4. Return dictionary

---

### 7. CutsceneActionAttribute

**Purpose**: Mark methods as cutscene-callable.

**Properties**:
- `DisplayName` - Name shown in editor dropdowns
- `AllowDuringGameplay` - If true, can be called outside cutscenes
- `CinematicOnly` - If true, only callable in cinematic contexts

**Usage**:
```csharp
[CutsceneAction("Cast Fireball", cinematicOnly = true)]
public void CastFireball(Vector3 target, float damage)
{
    // Ability logic
}
```

---

### 8. Actor Interfaces

#### ICutsceneActor

**Purpose**: Marks a GameObject as a cutscene actor.

**Requirements**:
- Implement `ICutsceneOverridable`
- Provide `GetTransform()` method

#### ICutsceneOverridable

**Purpose**: Defines lifecycle hooks for cutscene participation.

**Methods**:
- `GetCutsceneAdapter()` - Returns adapter for action invocation
- `OnCutsceneEnter()` - Called when cutscene starts (pause gameplay, disable AI)
- `OnCutsceneExit()` - Called when cutscene ends (resume gameplay, enable AI)

**Example Implementation**:
```csharp
public class EnemyActor : MonoBehaviour, ICutsceneActor
{
    private CutsceneActionAdapter adapter;
    
    public void Awake()
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
    
    // IMPORTANT: Initialize adapter in GetCutsceneAdapter() for edit-mode support
    // The Cutscene Editor window needs to access actions in edit mode,
    // but Awake() is only called during play mode
    public CutsceneActionAdapter GetCutsceneAdapter()
    {
        InitializeAdapter();
        return adapter;
    }
    
    public Transform GetTransform() => transform;
    
    public void OnCutsceneEnter()
    {
        // Disable AI
        GetComponent<AIController>().enabled = false;
    }
    
    public void OnCutsceneExit()
    {
        // Enable AI
        GetComponent<AIController>().enabled = true;
    }
    
    [CutsceneAction("Taunt")]
    public void PlayTaunt()
    {
        animator.SetTrigger("Taunt");
    }
}
```

---

## Action System

### ICutsceneAction

**Purpose**: Contract for cutscene actions.

**Method**:
```csharp
void Execute(ICutsceneActor actor, CutsceneContext context)
```

**Implementations**:

#### 1. ActorMethodAction

**Purpose**: Invoke a method on the actor via reflection.

**Fields**:
- `MethodName` - Name of the method to invoke (must match `[CutsceneAction]` display name)
- `Parameters` - Array of `SerializedCutsceneParameter` for method arguments

**Execution**:
```csharp
adapter.Invoke(MethodName, ConvertParameters(Parameters));
```

**Use Case**: Call any custom method on actor (abilities, dialogue, events)

---

#### 2. MoveActorAction

**Purpose**: Move actor to a target position.

**Fields**:
- `Target` - `Vector3` destination
- `Duration` - Time to reach destination
- `PlayWalkAnimation` - If true, plays walk animation during movement

**Execution**:
```csharp
context.Motion.Move(actor, Target, Duration);
if (PlayWalkAnimation) context.Animation.PlayAnimation(actor, "Walk");
```

**Use Case**: Reposition actors during cutscenes

---

#### 3. RotateActorAction

**Purpose**: Rotate actor to a target orientation.

**Fields**:
- `EulerRotation` - `Vector3` rotation (converted to Quaternion)
- `Duration` - Time to complete rotation

**Execution**:
```csharp
context.Motion.Rotate(actor, Quaternion.Euler(EulerRotation), Duration);
```

**Use Case**: Face actors toward targets or directions

---

#### 4. PlayAnimationAction

**Purpose**: Play a specific animation on actor.

**Fields**:
- `AnimationId` - String identifier for animation

**Execution**:
```csharp
context.Animation.PlayAnimation(actor, AnimationId);
```

**Use Case**: Trigger specific animations (attack, gesture, reaction)

---

### CutsceneActionReference

**Purpose**: Serializable wrapper for `ICutsceneAction`.

**Why It Exists**: Unity cannot serialize interfaces directly. This class uses `[SerializeReference]` to enable polymorphic serialization of action implementations.

**Usage**:
```csharp
[Serializable]
public class CutsceneActionReference
{
    [SerializeReference]
    public ICutsceneAction Action;
}
```

---

### CutsceneActionDefinition

**Purpose**: ScriptableObject template for reusable actions.

**Fields**:
- `actionName` - Method name to invoke
- `parameters` - List of parameter values

**Use Case**: Create asset-based action templates that can be dragged into Timeline clips

**Creation**: `Create > Cutscene > Action Definition`

---

### SerializedCutsceneParameter

**Purpose**: Serialize action parameters for Unity's inspector.

**Supported Types**:
- `Int`
- `Float`
- `Bool`
- `String`
- `Vector3`
- `GameObject`

**Why It Exists**: Unity serialization limitations require explicit type handling.

**Usage**:
```csharp
var param = new SerializedCutsceneParameter
{
    type = ParamType.Float,
    floatValue = 2.5f
};
object value = param.GetValue(); // Returns 2.5f as object
```

---

## Timeline Integration

### CutsceneActionTrack

**Purpose**: Custom Timeline track that holds `CutsceneActionClip` instances.

**Binding**: Binds to a `MonoBehaviour` that implements `ICutsceneActor`.

**Usage**:
1. Add track to Timeline
2. Bind track to actor GameObject
3. Add clips to track
4. Configure clip actions in Inspector

---

### CutsceneActionClip

**Purpose**: Timeline clip that executes a cutscene action.

**Fields**:
- `action` - `CutsceneActionReference` to execute
- `explicitTarget` - Optional override for the bound actor

**Behavior**:
- During Timeline playback, creates a `CutscenePlayableBehaviour`
- Passes action reference and actor to playable
- Playable executes action when clip is processed

---

### CutscenePlayableBehaviour

**Purpose**: Playable that executes the action during Timeline playback.

**Execution Flow**:
1. Timeline processes frame
2. `ProcessFrame()` is called
3. If not already executed:
   - Get `CutsceneDirector` from `PlayableDirector`
   - Get `CutsceneContext` from director
   - Resolve actor (explicit or bound)
   - Call `actionReference.Action.Execute(actor, context)`
   - Mark as executed

**One-Shot Execution**: Actions only execute once per clip playback (prevents repeated calls on scrubbing).

---

## Extensibility

### Adding New Systems

#### Custom Motion System

```csharp
public class NavMeshMotionSystem : IMotionSystem
{
    public void Move(ICutsceneActor actor, Vector3 target, float duration)
    {
        var agent = actor.GetTransform().GetComponent<NavMeshAgent>();
        agent.SetDestination(target);
        // Handle duration-based stopping
    }
    
    public void Rotate(ICutsceneActor actor, Quaternion rotation, float duration)
    {
        // Implement rotation
    }
}

// Register in CutsceneContextBuilder
Motion = new NavMeshMotionSystem()
```

#### Custom Animation System

```csharp
public class AnimancerAnimationSystem : IAnimationSystem
{
    public void PlayAnimation(ICutsceneActor actor, string id)
    {
        var animancer = actor.GetTransform().GetComponent<AnimancerComponent>();
        animancer.Play(id);
    }
}

// Register in CutsceneContextBuilder
Animation = new AnimancerAnimationSystem()
```

---

### Adding New Actions

```csharp
[Serializable]
public class TeleportActorAction : ICutsceneAction
{
    public Vector3 Destination;
    public ParticleSystem TeleportEffect;
    
    public void Execute(ICutsceneActor actor, CutsceneContext context)
    {
        if (TeleportEffect != null)
            Object.Instantiate(TeleportEffect, actor.GetTransform().position, Quaternion.identity);
        
        actor.GetTransform().position = Destination;
    }
}
```

---

### Adding Actor Methods

```csharp
public class PlayerActor : MonoBehaviour, ICutsceneActor
{
    // ...ICutsceneActor implementation...
    
    [CutsceneAction("Equip Weapon")]
    public void EquipWeapon(string weaponId)
    {
        inventory.EquipWeapon(weaponId);
    }
    
    [CutsceneAction("Say Dialogue")]
    public void SayDialogue(string dialogueKey)
    {
        dialogueSystem.ShowDialogue(dialogueKey);
    }
    
    [CutsceneAction("Look At Target")]
    public void LookAtTarget(GameObject target)
    {
        transform.LookAt(target.transform);
    }
}
```

---

## Usage Guide

### Setting Up a Cutscene

1. **Create Timeline**:
   - Add `PlayableDirector` to a GameObject
   - Create new Timeline asset
   - Assign Timeline to `PlayableDirector`

2. **Add CutsceneDirector**:
   - Add `CutsceneDirector` component to same GameObject
   - Assign `PlayableDirector` reference
   - (Optional) Assign `CinemachineCamera` for cinematic camera

3. **Create Actors**:
   - Implement `ICutsceneActor` on GameObjects
   - Add `[CutsceneAction]` methods for custom behaviors

4. **Add Tracks**:
   - In Timeline, right-click → `Extensions > Cutscene Engine > Cutscene Action Track`
   - Bind track to actor GameObject
   - Right-click track → Add Clip

5. **Configure Clips**:
   - Select clip in Timeline
   - In Inspector, choose action type
   - For `ActorMethodAction`: Select method from dropdown, configure parameters
   - For movement/rotation: Set target, duration, options

6. **Play Cutscene**:
   ```csharp
   cutsceneDirector.Play();
   ```

---

### Example: Boss Introduction Cutscene

```csharp
// BossActor.cs
public class BossActor : MonoBehaviour, ICutsceneActor
{
    private CutsceneActionAdapter adapter;
    private Animator animator;
    
    void Awake()
    {
        adapter = new CutsceneActionAdapter(this);
        animator = GetComponent<Animator>();
    }
    
    public CutsceneActionAdapter GetCutsceneAdapter() => adapter;
    public Transform GetTransform() => transform;
    
    public void OnCutsceneEnter()
    {
        GetComponent<AIController>().enabled = false;
    }
    
    public void OnCutsceneExit()
    {
        GetComponent<AIController>().enabled = true;
    }
    
    [CutsceneAction("Roar")]
    public void Roar()
    {
        animator.SetTrigger("Roar");
        AudioSystem.PlaySound("BossRoar");
    }
    
    [CutsceneAction("Summon Minions")]
    public void SummonMinions(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Instantiate(minionPrefab, spawnPoints[i]);
        }
    }
}
```

**Timeline Setup**:
```
Track 1 (Boss): [Move to Center] [Roar] [Summon Minions(3)]
Track 2 (Camera): [Focus on Boss] [Shake] [Dolly Zoom]
Track 3 (Player): [Move Back] [Play Reaction Anim]
```

---

## Missing Features & TODOs

### 1. Camera Actions (MISSING)

**Problem**: No camera-specific actions exist yet.

**Needed Actions**:
- `FocusCameraAction` - Focus camera on actor
- `MoveCameraAction` - Move camera to position
- `ShakeCameraAction` - Shake camera for impact
- `DollyZoomAction` - Cinematic dolly zoom effect

**Example Implementation**:
```csharp
[Serializable]
public class FocusCameraAction : ICutsceneAction
{
    public void Execute(ICutsceneActor actor, CutsceneContext context)
    {
        context.Camera.FocusOn(actor.GetTransform());
    }
}
```

---

### 2. Editor Tooling (MISSING)

**Problem**: No custom inspectors or editor windows exist.

**Needed Tools**:

#### A. Custom Clip Inspector
- Detect bound actor
- Populate dropdown with available actions from adapter
- Auto-generate parameter fields based on method signature
- Show method tooltips/descriptions
- Validate parameter types

#### B. Action Library Window
- Browse all reusable action assets
- Drag-and-drop into Timeline
- Filter by actor type, category
- Preview action details

#### C. Cutscene Editor Window
- High-level cutscene assembly interface
- List actors in scene
- Browse available actions per actor
- Quick-add clips to Timeline
- Batch editing of multiple clips

#### D. Method Signature Inspector
- Parse `MethodInfo` from `CutsceneActionAttribute`
- Generate UI fields for parameters:
  - `int` → IntField
  - `float` → FloatField
  - `bool` → Toggle
  - `string` → TextField
  - `Vector3` → Vector3Field
  - `GameObject` → ObjectField
  - `Enum` → EnumPopup
- Handle default parameter values

---

### 3. Animation Integration Improvements

**Problem**: Hardcoded "Walk" animation string in `MoveActorAction`.

**Solution**:
- Create `ICutsceneAnimationProvider` implementations per actor
- Query actor for available animations
- Populate animation dropdown in editor

---

### 4. Duration Synchronization

**Problem**: Action duration is separate from clip duration.

**Solution**:
- Auto-adjust clip duration to match action duration
- Show duration mismatch warnings
- Add "Sync Clip Length" button in inspector

---

### 5. Preview System

**Problem**: No way to preview actions without playing Timeline.

**Solution**:
- Add "Preview" button in clip inspector
- Execute action in editor (non-destructive)
- Reset actor state after preview

---

### 6. Action Validation

**Problem**: No validation of actor compatibility with actions.

**Solution**:
- Check if actor has required components
- Warn if method parameters are incompatible
- Highlight missing dependencies

---

### 7. Undo/Redo Support

**Problem**: Parameter changes may not register with Unity's undo system.

**Solution**:
- Wrap all parameter modifications in `Undo.RecordObject()`
- Ensure all custom inspectors use `EditorGUI.BeginChangeCheck()`

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      Unity Timeline                         │
│  ┌────────────────────────────────────────────────────┐     │
│  │         CutsceneActionTrack (bound to actor)       │     │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐         │     │
│  │  │  Clip 1  │  │  Clip 2  │  │  Clip 3  │         │     │
│  │  └────┬─────┘  └────┬─────┘  └────┬─────┘         │     │
│  │       │             │             │                │     │
│  └───────┼─────────────┼─────────────┼────────────────┘     │
└──────────┼─────────────┼─────────────┼──────────────────────┘
           │             │             │
           ▼             ▼             ▼
    ┌──────────────────────────────────────┐
    │   CutscenePlayableBehaviour          │
    │   • Resolves actor                   │
    │   • Gets CutsceneContext             │
    │   • Executes action                  │
    └──────────────┬───────────────────────┘
                   │
                   ▼
    ┌──────────────────────────────────────┐
    │      ICutsceneAction.Execute()       │
    │   ┌────────────────────────────┐     │
    │   │   ActorMethodAction        │     │
    │   │   • Invokes via adapter    │     │
    │   ├────────────────────────────┤     │
    │   │   MoveActorAction          │     │
    │   │   • Uses context.Motion    │     │
    │   ├────────────────────────────┤     │
    │   │   PlayAnimationAction      │     │
    │   │   • Uses context.Animation │     │
    │   └────────────────────────────┘     │
    └──────────────┬───────────────────────┘
                   │
         ┌─────────┴─────────┐
         ▼                   ▼
  ┌─────────────┐   ┌─────────────────┐
  │ Game Actor  │   │ CutsceneContext │
  │ (ICutscene  │   │  • Motion       │
  │  Actor)     │   │  • Animation    │
  │             │   │  • Camera       │
  │ • Methods   │   └─────────────────┘
  │   marked    │
  │   with      │
  │   [Cutscene │
  │    Action]  │
  └─────────────┘
```

---

## Performance Considerations

- **Reflection Caching**: Action methods are discovered once per actor and cached
- **Coroutine Usage**: Motion system uses coroutines (consider DOTween/PrimeTween for production)
- **One-Shot Actions**: Playable behaviours only execute once per playback
- **Context Lifetime**: Context is built once per cutscene, not per frame

---

## Best Practices

1. **Keep Actions Atomic**: Each action should do one thing
2. **Use System Abstractions**: Don't directly access components in actions (use context systems)
3. **Leverage Lifecycle Hooks**: Use `OnCutsceneEnter/Exit` for state management
4. **Create Reusable Actions**: Save common sequences as assets
5. **Test in Isolation**: Test individual actions before full cutscene assembly
6. **Document Methods**: Add XML comments to `[CutsceneAction]` methods for tooltips

---

## Troubleshooting

### "TargetParameterCountException: Number of parameters specified does not match"

**Cause**: Clip was created with wrong number of parameters for the method.

**Solution**:
1. Select the clip in Timeline
2. In Inspector, change the Method dropdown to a different method
3. Change it back to the original method
4. Parameters will be regenerated automatically
5. Fill in the parameter values

**Prevention**: Use the Cutscene Editor Window to add actions - it auto-generates correct parameters.

---

### "Cannot Configure Actor Method Action: No Actor Bound to Track"

**Cause**: The track doesn't have an actor bound in Timeline.

**Solution**:
1. Open Timeline window
2. Find the CutsceneActionTrack
3. Drag an ICutsceneActor GameObject to the track binding slot (left side of track)
4. The actor should appear in the Inspector

**Note**: The track binding slot accepts any MonoBehaviour, but it must implement ICutsceneActor to work.

---

### NullReferenceException in Edit Mode (Cutscene Editor Window)

**Cause**: CutsceneActionAdapter not initialized in edit mode.

**Solution**: Ensure your ICutsceneActor implementation follows this pattern:
```csharp
private CutsceneActionAdapter adapter;

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
    InitializeAdapter();  // ← Critical for edit mode!
    return adapter;
}
```

---

### Built-in Actions Not Visible

**Solution**: Built-in actions ARE visible! They appear in the "Action Type" dropdown in the Inspector:
1. Select any CutsceneActionClip in Timeline
2. Look at Inspector
3. Find "Action Type" dropdown
4. You'll see: Move Actor, Rotate Actor, Play Animation, Focus Camera, Move Camera, Shake Camera, Actor Method

To use a built-in action, just select it from the dropdown.

---

### Action Not Appearing in Dropdown

**Cause**: Method doesn't have `[CutsceneAction]` attribute or adapter not initialized.

**Solution**:
- Ensure method has `[CutsceneAction("Display Name")]` attribute
- Check that actor implements `ICutsceneActor`
- Verify adapter is created in `GetCutsceneAdapter()` (see pattern above)
- Click "Refresh" in Cutscene Editor Window

---

### Action Not Executing During Playback

**Cause**: CutsceneDirector not configured or actor not in scene.

**Solution**:
- Check that `CutsceneDirector` component is on same GameObject as `PlayableDirector`
- Verify track is bound to correct actor in Timeline
- Ensure actor GameObject is active in scene when Timeline plays
- Check Console for error messages

---

### Lag When Adding Actions to Timeline

**Cause**: Old version with unoptimized asset operations.

**Solution**: 
- Ensure you have the latest version with `EditorApplication.delayCall` optimization
- Use Cutscene Editor Window instead of manual clip creation
- If still laggy, close other heavy editor windows during cutscene editing

---

### Parameters Not Serializing
- Use supported parameter types only
- For custom types, add to `SerializedCutsceneParameter.ParamType`
- Consider creating custom action type for complex parameters

### Animation Not Playing
- Check that actor has animation component
- Verify animation ID matches asset names
- Ensure animation system is initialized in context

---

**Version**: 1.0  
**Last Updated**: February 18, 2026  
**Dependencies**: Unity 2022.3+, Timeline, Cinemachine (optional)

