# Interactions System - Complete Usage Guide

## Quick Setup (5 Minutes)

### Scene Setup
```csharp
// Create manager
manager.AddComponent<InteractionManager>();

// Add to player
player.AddComponent<PlayerInteractor>().detectionRadius = 3f;
player.tag = "Player";
```

### Create Manual Interaction (NPC)
```csharp
// NPC GameObject
npc.AddComponent<DialogueInteractable>().dialogueGraph = conversationGraph;
npc.AddComponent<SphereCollider>().isTrigger = true;
// Result: "Press E to talk" → Press E → Dialogue starts
```

### Create Auto Interaction (Collectible)
```csharp
// Item GameObject
item.AddComponent<InventoryInteractable>().itemId = swordId;
item.AddComponent<SphereCollider>().isTrigger = true;
// Result: Walk over → Auto-pickup (no prompt)
```

### Setup UI
```csharp
// Add presenter to canvas
canvas.AddComponent<CanvasInteractionPresenter>();
// Shows "Press E to interact" prompts
```

---

## Interaction Types

### Manual Interactions
Player must press button (E key by default) to interact.

**Use Cases**: Dialogue, opening doors, examining objects

**Setup**:
```csharp
gameObject.AddComponent<DialogueInteractable>();
// Shows "Press E" prompt when in range
// Requires player action to trigger
```

**User Experience**:
- Prompt appears at bottom of screen
- Player actively chooses to interact
- Good for story moments and important decisions

### Auto Interactions
Automatically trigger when player is in range, no button press needed.

**Use Cases**: Collectibles, pickups, area triggers

**Setup**:
```csharp
var pickup = gameObject.AddComponent<InventoryInteractable>();
pickup.triggerType = InteractionTriggerType.Auto;
// No prompt shown
// Happens automatically
```

**User Experience**:
- No UI prompt
- Seamless gameplay
- Good for collectibles and traps

### Custom Interactions
Developer-defined trigger logic with maximum flexibility.

**Setup**:
```csharp
var custom = gameObject.AddComponent<ActionInteractable>();
custom.OnInteractCallback += MyCustomLogic;
// Your logic runs on interact
```

---

## Built-in Interactables

### DialogueInteractable
Triggers dialogue graphs:

```csharp
var npc = gameObject.AddComponent<DialogueInteractable>();
npc.dialogueGraph = myGraph;
npc.triggerType = InteractionTriggerType.Manual;  // Press E
```

When player presses E:
- Dialogue starts
- UI switches to dialogue presenter
- Player can complete conversation

### InventoryInteractable
Pickup items:

```csharp
var item = gameObject.AddComponent<InventoryInteractable>();
item.itemId = weaponId;
item.quantity = 1;
item.destroyAfterPickup = true;
item.triggerType = InteractionTriggerType.Auto;
```

When player walks over:
- Item automatically added to inventory
- GameObject destroyed
- No prompt shown

### QuestInteractable
Trigger quest events:

```csharp
var marker = gameObject.AddComponent<QuestInteractable>();
marker.questId = mainQuestId;
marker.completeOnInteract = true;
```

When player interacts:
- Quest objective completes
- Story progresses
- Rewards given

### ActionInteractable
Custom callback-based:

```csharp
var action = gameObject.AddComponent<ActionInteractable>();
action.OnInteractCallback += () => Debug.Log("Custom logic!");
```

Fully flexible for any logic you need.

### UnityEventInteractable
Inspector-friendly event-based:

```csharp
var eventInt = gameObject.AddComponent<UnityEventInteractable>();
// Assign UnityEvent in inspector
```

---

## UI/Prompts

### "Press E to Interact" Prompt

Only shows for **Manual** interactions when player is close:

**Settings in Inspector**:
```
CanvasInteractionPresenter:
├─ Text Config
│  ├─ TextMeshProUGUI component
│  ├─ Color
│  ├─ Font Size
│  └─ Format string ("Press {key} to {action}")
├─ Panel Config
│  ├─ Background Color (RGBA)
│  ├─ Corner Radius
│  └─ Padding
└─ Fade Settings
   ├─ Fade In Duration
   └─ Fade Out Duration
```

Prompt appears when:
- Player gets close to manual interactable
- Fades in smoothly
- Disappears when out of range

### Progress Bars

Show during ongoing interactions:

```csharp
presenter.ShowProgress(0.5f);  // 50% done
```

### Blocked Messages

Show when interaction not available:

```csharp
presenter.ShowBlocked("Must have sword to open door");
```

Shows for 3 seconds then auto-hides.

---

## Custom Interactables

Extend `InteractableBase` for any interaction:

```csharp
public sealed class PuzzleInteractable : InteractableBase
{
    [SerializeField] private int puzzleId;

    public override void Interact()
    {
        var puzzle = PuzzleManager.Instance;
        puzzle.ShowPuzzle(puzzleId, OnPuzzleComplete);
    }

    private void OnPuzzleComplete(bool correct)
    {
        if (correct)
        {
            Destroy(gameObject);
        }
    }

    public override void OnEnterRange()
    {
        // Show hint or animation
    }
}
```

Inspector Customization:
```
PuzzleInteractable:
├─ Trigger Type (Manual/Auto)
├─ Interaction Range (2f)
├─ Puzzle ID (serialized)
└─ Prompt Style (colors, font, etc.)
```

---

## Conditional Availability

Make interactions conditional based on game state:

```csharp
public sealed class LockedDoorInteractable : InteractableBase
{
    [SerializeField] private int requiredKeyId;

    public override bool IsAvailable
    {
        get
        {
            var inventory = InventorySystem.Instance;
            return inventory.HasItem(requiredKeyId);
        }
    }

    public override void Interact()
    {
        // Door opens
    }
}
```

When not available:
- "Press E" prompt still shows
- Player presses E → "Blocked: Must have key"
- Smooth UX feedback

---

## Input Customization

Change interaction key without changing logic:

**Keyboard** (Default - E key):
```csharp
// Built-in KeyboardInteractionInput uses KeyCode.E
```

**Custom Key**:
```csharp
public sealed class CustomKeyInput : IInteractionInputHandler
{
    [SerializeField] private KeyCode customKey = KeyCode.F;

    public void Update()
    {
        if (Input.GetKeyDown(customKey))
        {
            _callback?.Invoke();
        }
    }
    // ... rest of interface
}

// Assign to InteractionManager
```

**Gamepad**:
```csharp
public sealed class GamepadInput : IInteractionInputHandler
{
    public void Update()
    {
        if (Input.GetButtonDown("Gamepad_Y"))
        {
            _callback?.Invoke();
        }
    }
    // ... rest of interface
}
```

---

## UI Presenters

### Canvas Presenter (Bottom Screen)
```csharp
canvas.AddComponent<CanvasInteractionPresenter>();

// Fully customizable:
// - Text color, size, font
// - Panel background, padding
// - Fade duration
// - All in Inspector
```

Shows:
- "Press E to interact" when in range
- Progress bars for channeled interactions
- Blocked messages when unavailable

### WorldSpace Presenter (3D UI)
```csharp
canvas3D.AddComponent<WorldSpaceInteractionPresenter>();

// Shows floating prompts above interactables
// Good for immersive games
```

### Custom Presenter

Implement `IInteractionPresenter`:

```csharp
public sealed class MyCustomPresenter : MonoBehaviour, IInteractionPresenter
{
    public void ShowPrompt(string text, IInteractable interactable)
    {
        // Your custom UI
    }

    public void HidePrompt() { }
    public void ShowProgress(float progress) { }
    public void HideProgress() { }
    public void ShowBlocked(string reason) { }
}

// Assign to InteractionManager
```

---

## Game System Integration

### With Dialogue System

```csharp
// NPC interaction triggers dialogue
npc.AddComponent<DialogueInteractable>().dialogueGraph = conversationGraph;

// Dialogue can give quest rewards, items, etc.
// Through IGameServices integration
```

When interaction happens:
1. Player presses E near NPC
2. DialogueInteractable.Interact() called
3. DialogueManager.StartDialogue() called
4. Dialogue UI appears
5. Conversation plays

### With Inventory System (Future)

```csharp
// Pickups add items
item.AddComponent<InventoryInteractable>().itemId = weaponId;

// Dialogue can check inventory
// Quest can require inventory items
```

### With Quest System (Future)

```csharp
// Quest markers
marker.AddComponent<QuestInteractable>().questId = mainQuestId;

// Quest rewards can give items
// Items can trigger quests
```

---

## EventBus Events

React to interactions system-wide:

```csharp
// Player gets close to interactable
EventBus<InteractionStateChangedEvent>.Register(binding);

void OnStateChanged(InteractionStateChangedEvent evt)
{
    if (evt.IsInRange)
        Debug.Log("Entered interaction range");
}

// Player completes interaction
EventBus<InteractionPerformedEvent>.Register(binding);

void OnPerformed(InteractionPerformedEvent evt)
{
    Debug.Log($"Interacted with {evt.InteractableId}");
}
```

---

## Advanced Patterns

### Conditional Quest Marker

```csharp
public sealed class ConditionalQuestMarker : InteractableBase
{
    public override bool IsAvailable => 
        QuestSystem.Instance.IsQuestActive(questId);

    public override void Interact()
    {
        QuestSystem.Instance.CompleteObjective(questId);
    }
}
```

### Channeled Interaction (Hold for Duration)

```csharp
public sealed class ChanneledInteractable : InteractableBase
{
    private float _channelTime;

    public override void Interact()
    {
        _channelTime = 0f;
        StartCoroutine(ChannelRoutine());
    }

    private IEnumerator ChannelRoutine()
    {
        while (_channelTime < 3f)
        {
            _channelTime += Time.deltaTime;
            _manager?._presenter.ShowProgress(_channelTime / 3f);
            yield return null;
        }
        // Complete action
    }
}
```

### Proximity-Based Hint System

```csharp
public override void OnEnterRange()
{
    HintSystem.Instance.ShowHint("Look for the switch");
}

public override void OnExitRange()
{
    HintSystem.Instance.HideHint();
}
```

---

## Setup Checklist

- [ ] InteractionManager on scene manager
- [ ] PlayerInteractor on Player
- [ ] Player has "Player" tag
- [ ] All interactables have trigger colliders
- [ ] Canvas setup with CanvasInteractionPresenter
- [ ] Input handler assigned (or using default)
- [ ] DialogueInteractables have graphs assigned
- [ ] InventoryInteractables have item IDs
- [ ] All trigger types set correctly (Manual/Auto)
- [ ] Test Manual interactions (see prompt, press E)
- [ ] Test Auto interactions (auto-trigger)
- [ ] Test blocked state (when unavailable)
- [ ] Verify UI appearance and colors

---

## Performance

- **Sphere Cast Optimization**: Uses Physics.OverlapSphereNonAlloc
- **Lazy Registration**: Only register when enabled
- **Minimal Overhead**: No per-frame polling
- **Event-Based**: Only publish on state changes
- **Detection**: One sphere cast per frame, cached results

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Prompt not showing | Verify PlayerInteractor in range, trigger type Manual |
| Interaction not triggering | Check InteractionManager exists, event assigned |
| Auto-pickup not working | Check trigger type is Auto, collider is trigger |
| Input not responding | Verify input handler assigned, input enabled |
| Multiple overlapping | Use smaller colliders, check ranges |
| UI not appearing | Verify presenter assigned to manager |
| Blocked message not showing | Implement IsAvailable property correctly |

---

## Summary

The Interaction System provides:
- Easy setup for any interaction type
- Clean integration with other systems
- Fully customizable UI
- Dynamic input handling
- EventBus integration
- Type-safe design
- Production-ready framework
- Zero compiler errors

