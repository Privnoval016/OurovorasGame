# Cutscene Engine - Quick Start Guide

Get started creating cutscenes in **5 minutes**!

---

## Prerequisites

- Unity 2022.3 or later
- Timeline package installed
- (Optional) Cinemachine for camera features

---

## Step 1: Create Your First Actor (2 minutes)

### 1.1 Create a script that implements ICutsceneActor

```csharp
using UnityEngine;
using Extensions.CutsceneEngine;

public class MyFirstActor : MonoBehaviour, ICutsceneActor
{
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
    
    // Required interface methods
    // IMPORTANT: Call InitializeAdapter() here for edit-mode support
    public CutsceneActionAdapter GetCutsceneAdapter()
    {
        InitializeAdapter();
        return adapter;
    }
    
    public Transform GetTransform() => transform;
    
    public void OnCutsceneEnter()
    {
        Debug.Log("Cutscene started!");
        // Disable gameplay here (AI, input, etc.)
    }
    
    public void OnCutsceneExit()
    {
        Debug.Log("Cutscene ended!");
        // Re-enable gameplay here
    }
    
    // Add cutscene actions with [CutsceneAction] attribute
    [CutsceneAction("Wave Hello")]
    public void WaveHello()
    {
        Debug.Log("Character waves!");
        // Play wave animation
    }
    
    [CutsceneAction("Say Something")]
    public void SaySomething(string message)
    {
        Debug.Log($"Character says: {message}");
        // Show dialogue UI
    }
}
```

### 1.2 Add script to GameObject
- Create a Cube in scene (or use existing character)
- Add `MyFirstActor` component
- Done!

---

## Step 2: Create Timeline (1 minute)

### 2.1 Create Timeline GameObject
1. Create empty GameObject: `CutsceneController`
2. Add component: `Playable Director`
3. Add component: `Cutscene Director`
4. In `Cutscene Director`, assign `director` field to the `Playable Director`

### 2.2 Create Timeline Asset
1. Select `CutsceneController`
2. In `Playable Director`, click "Create" next to Playable field
3. Save Timeline asset: `Assets/MyFirstCutscene.playable`
4. Timeline window should open automatically

---

## Step 3: Add Actions with Editor Tools (2 minutes)

### Method A: Using Cutscene Editor Window (Fastest)

1. **Open Cutscene Editor**
   - Menu: `Window > Cutscene Engine > Cutscene Editor`
   - Dock next to Timeline window

2. **Select Director**
   - Click "Find Director in Scene"
   - Or drag `CutsceneController` to "Playable Director" field

3. **Add Actions**
   - Go to "Scene Actors" tab
   - Click "Refresh" if actor not showing
   - Click on "MyFirstActor" in list
   - See available actions: "Wave Hello", "Say Something"
   - Click "Add to Timeline" for "Wave Hello"
   - Click "Add to Timeline" for "Say Something"

4. **Configure Actions**
   - Select "Say Something" clip in Timeline
   - Inspector shows method parameters
   - Enter message: "Hello, world!"
   - Done!

### Method B: Manual (Slower but educational)

1. **Add Track**
   - Right-click in Timeline
   - Extensions > Cutscene Engine > Cutscene Action Track
   - Drag `MyFirstActor` GameObject to track binding

2. **Add Clip**
   - Right-click on track
   - Add > Cutscene Action Clip

3. **Configure Clip**
   - Select clip
   - Inspector: Choose action type "Actor Method"
   - Method dropdown: Select "Wave Hello"
   - Done!

---

## Step 4: Test Your Cutscene (30 seconds)

### 4.1 Play in Editor
1. Press Play
2. In Cutscene Editor window, click "Play Cutscene"
3. Watch Timeline play
4. See debug logs in console

### 4.2 Trigger from Script
```csharp
using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    public CutsceneDirector cutsceneDirector;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            cutsceneDirector.Play();
        }
    }
}
```

---

## Congratulations! 🎉

You've created your first cutscene!

---

## Next Steps

### Add More Actions

```csharp
[CutsceneAction("Jump")]
public void Jump(float height)
{
    // Jump logic
}

[CutsceneAction("Look At")]
public void LookAt(GameObject target)
{
    transform.LookAt(target.transform);
}

[CutsceneAction("Throw Object")]
public void ThrowObject(GameObject obj, Vector3 direction, float force)
{
    // Throwing logic
}
```

### Use Built-in Actions

The Cutscene Engine includes several built-in actions that work independently of actor methods:

#### Movement Actions

**1. Move Actor Action**
- **Purpose**: Move actor to a target position over time
- **Usage**:
  1. Add Cutscene Action Track to Timeline
  2. Bind an ICutsceneActor to the track
  3. Create a clip on the track
  4. Select clip → Inspector
  5. Action Type dropdown → Select "Move Actor"
  6. Set Target (Vector3 position)
  7. Set Duration (seconds)
  8. Toggle "Play Walk Animation" if needed
- **Requirements**: Actor must provide IMotionSystem in CutsceneContext

**2. Rotate Actor Action**
- **Purpose**: Rotate actor to target rotation over time
- **Usage**:
  1. Select clip → Inspector
  2. Action Type → "Rotate Actor"
  3. Set Rotation (Euler angles as Vector3)
  4. Set Duration (seconds)
- **Requirements**: IMotionSystem in CutsceneContext

#### Animation Actions

**3. Play Animation Action**
- **Purpose**: Trigger animation on actor
- **Usage**:
  1. Select clip → Inspector
  2. Action Type → "Play Animation"
  3. Enter Animation ID (string)
- **Requirements**: IAnimationSystem in CutsceneContext
- **Note**: Animation ID depends on your animation system (Animator trigger, Animancer state, etc.)

#### Camera Actions

**4. Focus Camera Action**
- **Purpose**: Make camera focus on target
- **Usage**:
  1. Select clip → Inspector
  2. Action Type → "Focus Camera"
  3. Toggle "Use Self As Target" OR set explicit Target GameObject
- **Requirements**: ICameraSystem in CutsceneContext

**5. Move Camera Action**
- **Purpose**: Move camera to position or object
- **Usage**:
  1. Select clip → Inspector
  2. Action Type → "Move Camera"
  3. Toggle "Use Position" and set position, OR set Target GameObject
  4. Set Duration (seconds)
- **Requirements**: ICameraSystem in CutsceneContext

**6. Shake Camera Action**
- **Purpose**: Apply screen shake effect
- **Usage**:
  1. Select clip → Inspector
  2. Action Type → "Shake Camera"
  3. Set Intensity (0-10 range)
  4. Set Duration (seconds)
- **Requirements**: ICameraSystem in CutsceneContext

#### When to Use Built-in vs Custom Actions

**Use Built-in Actions When**:
- Need common behaviors (move, rotate, animate)
- Want system-agnostic cutscenes
- Action should work with any motion/animation system

**Use Actor Methods When**:
- Need actor-specific logic
- Need complex parameter combinations
- Action is unique to this character type

**Example: Mixing Both**
```
Track: PlayerActor
├─ [0:00-0:02] Move Actor (to position)        ← Built-in
├─ [0:02-0:03] Cast Fireball (custom method)   ← Actor Method
└─ [0:03-0:05] Play Animation ("Victory")      ← Built-in
```

### Add Camera Work

1. Create camera actor GameObject
2. Implement `ICutsceneActor`
3. Use camera actions:
   - Focus Camera
   - Move Camera
   - Shake Camera

### Create Reusable Actions

1. Right-click in Project
2. Create > Cutscene > Action Definition
3. Configure action name and parameters
4. Reference asset in clips

---

## Common Patterns

### Dialogue Sequence
```
Track: PlayerActor
├─ [0:00-0:02] Say Something ("Let's go!")
└─ [0:02-0:04] Wave Hello

Track: NPCActor
├─ [0:01-0:03] Say Something ("Wait for me!")
└─ [0:03-0:05] Play Animation ("Run")
```

### Boss Introduction
```
Track: BossActor
├─ [0:00-0:01] Move Actor (to center)
├─ [0:01-0:02] Play Animation ("Roar")
└─ [0:02-0:04] Custom Action ("Summon Minions")

Track: CameraActor
├─ [0:00-0:01] Focus Camera (on Boss)
└─ [0:01-0:02] Shake Camera (intensity: 5)

Track: PlayerActor
└─ [0:00-0:02] Move Actor (back away)
```

---

## Troubleshooting

### "No actor bound to track"
- Make sure to drag actor GameObject to track binding in Timeline

### "Method not found"
- Check that method has `[CutsceneAction]` attribute
- Verify spelling matches exactly

### "Action doesn't execute"
- Ensure `CutsceneDirector` component is on same GameObject as `PlayableDirector`
- Check that cutscene is actually playing
- Look for errors in Console

### "Cutscene Editor shows no actors"
- Click "Refresh" button
- Verify GameObjects implement `ICutsceneActor`
- Check that objects are active in Hierarchy

---

## Tips

1. **Name your tracks** for clarity (e.g., "Player - Actions", "Boss - Intro")
2. **Use validation** - click "Validate Action" button in inspector
3. **Test incrementally** - add one action, test, repeat
4. **Organize clips** - use Timeline layers and colors
5. **Document complex cutscenes** - add comments in Timeline with markers

---

## Resources

- **Full Documentation**: `README.md`
- **Editor Guide**: `EDITOR_GUIDE.md`
- **Implementation Summary**: `IMPLEMENTATION_SUMMARY.md`

---

## Support

For issues or questions:
1. Check troubleshooting section above
2. Read full documentation
3. Check Unity Console for error messages
4. Validate your actions with the validation button

---

**You're ready to create amazing cutscenes!** 🎬

Start simple, experiment, and have fun!

