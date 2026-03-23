# Interactions System

A modular, scalable interaction framework for handling all types of interactable objects. Supports dialogue, inventory pickups, quests, and custom actions through a clean adapter pattern.

**See [USAGE.md](USAGE.md) for step-by-step setup and examples.**

## Features

✅ **Three Trigger Types** - Manual (button), Auto (proximity), Custom (developer-defined)
✅ **Dynamic Input** - Subscribe/unsubscribe without hardcoding
✅ **Adapter Pattern** - Dialogue, inventory, quests, custom systems
✅ **UI Separation** - Through IInteractionPresenter interface
✅ **EventBus Integration** - Reactive interaction events
✅ **Inspector-Friendly** - Odin Inspector support with full customization
✅ **Zero Coupling** - All systems independent and pluggable

## Quick Architecture

```
PlayerInteractor (detects nearby)
         ↓
InteractionManager (coordinates)
         ↓
IInteractable (concrete types)
├─ DialogueInteractable
├─ InventoryInteractable
└─ Custom implementations
         ↓
Game Systems + UI
```

## Interaction Types

| Type | Use Case | Example |
|------|----------|---------|
| **Manual** | Player-triggered | Talk to NPC, open door, take item |
| **Auto** | Proximity-triggered | Walk over collectible, enter trap area |
| **Custom** | Developer logic | Conditional triggers, multi-step puzzles |

## System Adapters

**DialogueInteractable**
```csharp
npc.AddComponent<DialogueInteractable>().dialogueGraph = myGraph;
// Player presses E → dialogue starts
```

**InventoryInteractable**
```csharp
item.AddComponent<InventoryInteractable>().itemId = weaponId;
// Player walks over → item picked up
```

**QuestInteractable**
```csharp
marker.AddComponent<QuestInteractable>().questId = mainQuestId;
// Player interacts → quest progresses
```

**ActionInteractable**
```csharp
trigger.AddComponent<ActionInteractable>().OnInteractCallback += CustomLogic;
// Flexible custom handling
```

## UI Presenters

**CanvasInteractionPresenter** - Bottom UI
- "Press X to interact" prompts
- Progress bars
- Blocked messages
- Fully customizable colors, fonts, positions

**WorldSpaceInteractionPresenter** - 3D world UI
- Floating prompts above objects
- Great for immersive games

**Custom** - Implement IInteractionPresenter
- Hook any UI framework
- Complete control over presentation

## Integration with Other Systems

### Dialogue System
Interactions trigger dialogue automatically:
```
Player near NPC (DialogueInteractable)
    ↓
Presses E
    ↓
DialogueManager starts graph
    ↓
Dialogue UI shows (bottom or combat)
```

### Inventory System (Future)
Interactions add items automatically:
```
Player near item (InventoryInteractable)
    ↓
Auto-triggers on proximity
    ↓
Item added to inventory
```

### Quest System (Future)
Interactions progress quests:
```
Player at marker (QuestInteractable)
    ↓
Presses E
    ↓
Quest objective complete
```

## EventBus Events

```csharp
// React when player gets close
EventBus<InteractionStateChangedEvent>.Register(binding);

// React when interaction happens
EventBus<InteractionPerformedEvent>.Register(binding);
```

## Extension

### Custom Interactable
```csharp
public sealed class PuzzleInteractable : InteractableBase
{
    public override void Interact() { }
}
```

### Custom Input
```csharp
public sealed class GamepadInput : IInteractionInputHandler { }
```

### Custom Presenter
```csharp
public sealed class MyPresenter : IInteractionPresenter { }
```

## Performance

- Sphere cast optimization (Physics.OverlapSphereNonAlloc)
- Lazy registration on enable/disable
- No per-frame polling
- Event-based state changes

└──────────────────────────────────────────┘
           ↑
┌──────────────────────────────────────────┐
│    Manager Layer (Orchestration)        │
│    InteractionManager (Singleton)       │
│  ├─ Input Handling                     │
│  ├─ Interactable Registration          │
│  ├─ Focus Management                   │
│  └─ EventBus Publishing                │
└──────────────────────────────────────────┘
           ↑
┌──────────────────────────────────────────┐
│    Detection Layer (Proximity)          │
│    PlayerInteractor                     │
│  ├─ Sphere Cast Detection              │
│  ├─ Prioritization                     │
│  └─ Range Checking                     │
└──────────────────────────────────────────┘
           ↑
┌──────────────────────────────────────────┐
│   Interaction Layer (Concrete Types)    │
│  ├─ InteractableBase (Framework)       │
│  ├─ DialogueInteractable               │
│  ├─ InventoryInteractable              │
│  ├─ QuestInteractable                  │
│  └─ ActionInteractable                 │
└──────────────────────────────────────────┘
```

## Core Concepts

### Interaction Types

**Manual Interaction**
- Player must press button (E) to interact
- Shows prompt when in range
- Best for: Dialogue, NPCs, objects requiring intent

**Auto Interaction**
- Triggers automatically when player enters range
- No prompt needed
- Best for: Collectibles, traps, area effects

**Custom Interaction**
- Developer-defined trigger logic
- Maximum flexibility
- Best for: Complex conditions, line-of-sight, proximity-based

### Interactables

All interactables inherit from `InteractableBase` and implement `IInteractable`:

```csharp
public interface IInteractable
{
    Guid InteractableId { get; }
    InteractionTriggerType TriggerType { get; }
    bool IsAvailable { get; }
    void Interact();
    void OnEnterRange();
    void OnExitRange();
}
```

### Focus Management

The InteractionManager tracks:
- **Focused Interactable**: Manual interaction target (shows prompt)
- **In-Range Interactable**: Auto interaction triggers
- **Previous State**: For UI updates and transitions

### Input Handling

Dynamic input subscription allows custom key bindings:

```csharp
public interface IInteractionInputHandler
{
    void EnableInput();
    void DisableInput();
    void Subscribe(Action onInteractPressed);
    void Unsubscribe(Action onInteractPressed);
}
```

## Built-in Interactables

### DialogueInteractable

Triggers dialogue on interaction:

```csharp
var dialogueInteractable = npc.AddComponent<DialogueInteractable>();
dialogueInteractable.dialogueGraph = myGraph;
dialogueInteractable.triggerType = InteractionTriggerType.Manual;
```

### InventoryInteractable

Pickup items automatically or on interaction:

```csharp
var pickup = item.AddComponent<InventoryInteractable>();
pickup.itemId = 5;
pickup.quantity = 3;
pickup.destroyAfterPickup = true;
```

### QuestInteractable

Trigger quest state changes:

```csharp
var questObjective = marker.AddComponent<QuestInteractable>();
questObjective.questId = 10;
questObjective.completeOnInteract = true;
```

### ActionInteractable

Custom delegate-based interaction:

```csharp
var action = trigger.AddComponent<ActionInteractable>();
action.OnInteractCallback += () => Debug.Log("Interacted!");
```

### UnityEventInteractable

Inspector-friendly event-based interaction:

```csharp
var eventInteractable = button.AddComponent<UnityEventInteractable>();
// Assign event in inspector
```

## UI/Presenters

UI is completely separate from interaction logic through the `IInteractionPresenter` interface:

### CanvasInteractionPresenter

Standard canvas-based UI:

```
[E to Interact]  ← Prompt above interactable
[████████░░░░]   ← Progress bar (optional)
```

Setup:
1. Create Canvas with prompt/progress/blocked text
2. Add CanvasInteractionPresenter
3. Assign UI elements in inspector
4. Configure prompt style (color, format, etc)

### WorldSpaceInteractionPresenter

Floating 3D text above objects:

```csharp
var presenter = canvas.gameObject.AddComponent<WorldSpaceInteractionPresenter>();
presenter.worldCanvas = my3DCanvas;
presenter.promptPrefab = interactionPromptPrefab;
```

### Custom Presenter

Implement `IInteractionPresenter` for any UI system:

```csharp
public sealed class MyCustomPresenter : MonoBehaviour, IInteractionPresenter
{
    public void ShowPrompt(string promptText, IInteractable interactable)
    {
        // Show your custom UI
    }

    public void ShowProgress(float progress)
    {
        // Show progress bar
    }

    public void ShowBlocked(string reason)
    {
        // Show "unavailable" message
    }

    public void HidePrompt() { }
    public void HideProgress() { }
}
```

## Advanced Usage

### Custom Interactable Type

Create interaction for specific game systems:

```csharp
public sealed class MiniGameInteractable : InteractableBase
{
    [SerializeField] private int miniGameId;

    public override void Interact()
    {
        var miniGameManager = MiniGameManager.Instance;
        miniGameManager.StartMiniGame(miniGameId);
    }

    public override void OnEnterRange()
    {
        // Show hint or animation
    }
}
```

### Conditional Availability

Make interactions conditional:

```csharp
public sealed class RequireQuestInteractable : InteractableBase
{
    [SerializeField] private int requiredQuestId;

    public override bool IsAvailable
    {
        get
        {
            var questSystem = QuestSystem.Instance;
            return questSystem.IsQuestComplete(requiredQuestId);
        }
    }
}
```

### Ranged Input Handler

Custom input for gamepads or remote triggers:

```csharp
public sealed class GamepadInteractionInput : MonoBehaviour, IInteractionInputHandler
{
    [SerializeField] private GamepadButton interactButton = GamepadButton.Y;
    private Action _onInteractPressed;

    public void Subscribe(Action onInteractPressed) => _onInteractPressed += onInteractPressed;
    public void Unsubscribe(Action onInteractPressed) => _onInteractPressed -= onInteractPressed;
    public void EnableInput() { }
    public void DisableInput() { }

    private void Update()
    {
        if (Input.GetButtonDown("Gamepad_Y"))
            _onInteractPressed?.Invoke();
    }
}
```

### Channeled Interactions

Long-duration interactions with progress:

```csharp
public sealed class ChanneledInteractable : InteractableBase
{
    [SerializeField] private float channelDuration = 3f;
    private float _channelProgress = 0f;

    public override void Interact()
    {
        // Start channeling
        _channelProgress = 0f;
    }

    private void Update()
    {
        if (_isChanneling)
        {
            _channelProgress += Time.deltaTime;
            if (_channelProgress >= channelDuration)
            {
                CompleteInteraction();
            }
        }
    }
}
```

## Integration with Game Systems

### With Dialogue System

```csharp
var dialogueInteractable = npc.AddComponent<DialogueInteractable>();
dialogueInteractable.dialogueGraph = myDialogueGraph;

// Dialogue events will fire through EventBus
```

### With Inventory System (Future)

```csharp
var pickup = item.AddComponent<InventoryInteractable>();
pickup.itemId = weaponId;
pickup.quantity = 1;

// Will integrate with inventory when system is created
```

### With Quest System (Future)

```csharp
var objective = marker.AddComponent<QuestInteractable>();
objective.questId = mainQuestId;
objective.completeOnInteract = true;

// Will integrate with quest system when made generic
```

### Custom Game Systems

Hook into any system through adapters:

```csharp
public sealed class TeleporterInteractable : Runtime.InteractableBase
{
    [SerializeField] private string targetScene;

    public override void Interact()
    {
        var teleportManager = TeleportManager.Instance;
        teleportManager.Teleport(targetScene);
    }
}
```

## Events

The interaction system publishes EventBus events:

```csharp
// Interactable entered/exited range
var binding = new EventBinding<InteractionStateChangedEvent>(OnStateChanged);
EventBus<InteractionStateChangedEvent>.Register(binding);

// Interaction performed
var binding2 = new EventBinding<InteractionPerformedEvent>(OnInteractionPerformed);
EventBus<InteractionPerformedEvent>.Register(binding2);
```

## Performance

- **Minimal Overhead**: Singleton manager, no per-frame searches
- **Sphere Cast Caching**: Uses Physics.OverlapSphereNonAlloc
- **Lazy Registration**: Interactables register only when enabled
- **Event-Based**: No polling, only event publishing on state changes

## Best Practices

1. **Use Adapters**: Create adapters for game-specific interactions (DialogueInteractable, etc)
2. **Keep Logic Separate**: Put game logic in commands/events, not in interactable
3. **Custom Presenters**: Create presenters for your UI system
4. **Tag Everything**: Use "Player" tag for detection
5. **Colliders Are Triggers**: Always use IsTrigger = true
6. **Test Ranges**: Verify detection radius matches your game scale

## Setup Checklist

- [ ] InteractionManager on scene manager GameObject
- [ ] PlayerInteractor on Player GameObject
- [ ] Player has "Player" tag
- [ ] Interactables have trigger colliders
- [ ] Input handler assigned (or using default)
- [ ] Presenter assigned (or using debug)
- [ ] DialogueInteractables have graphs assigned
- [ ] Test interaction in play mode

## Summary

The Interaction System provides:
- Clean, modular architecture
- Extensible adapter pattern for any game system
- Decoupled UI through presenter pattern
- Dynamic input subscription
- EventBus integration for reactive systems
- Type-safe interaction handling
- Easy custom implementations

Use it for dialogue, pickups, quests, puzzles, and any other interactable objects in your game.

