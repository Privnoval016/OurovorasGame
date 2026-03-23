# Dialogue System

A modular, type-safe dialogue management framework for Unity games. Build complex branching conversations with conditions, state management, and seamless game integration.

**See [USAGE.md](USAGE.md) for step-by-step guides and examples.**

## Features at a Glance

✅ **Type-Safe Design** - No strings for core IDs; NodeId, TextKey, SpeakerId prevent errors  
✅ **Modular Architecture** - 8 cleanly separated layers with single responsibilities  
✅ **Visual Graph Editor** - Drag-and-drop dialogue composition with node connections  
✅ **Condition System** - Reuses existing ICondition<T> framework for branching logic  
✅ **Command Execution** - Schedule and execute arbitrary game actions from dialogue  
✅ **State Management** - Type-safe dialogue state without boxing  
✅ **Localization Ready** - Multi-language support with TextKey-based text lookup  
✅ **EventBus Integration** - Dialogue events publish through central EventBus  
✅ **Voiceover Support** - Auto-playing dialogue with external graph traversal  
✅ **UI System** - Bottom and combat dialogue presenters with full customization  

## Quick Start

### Interactive Dialogue (Player Choice)
1. Create DialogueGraph asset (Assets > Create > Dialogue > Dialogue Graph)
2. Attach DialogueInteractable to NPC
3. Player presses E to start dialogue

### Voiceover Dialogue (Auto-Play)
1. Create DialogueGraph with line nodes
2. Add VoiceoverDialogueTrigger to area trigger
3. Dialogue auto-plays when player enters area
4. Automatically transitions between dialogue areas

## Core Systems

### 1. Manual Interaction Dialogue
- Player manually triggers with E key
- Used for conversations with NPCs
- Shows "Press E to interact" prompt

### 2. Voiceover Dialogue  
- Automatically plays when entering areas
- Smoothly transitions between dialogue zones
- Perfect for story narration or area descriptions

### 3. Type-Safe State Management
```csharp
StateKey<bool> hasMetNPC = new StateKey<bool>(1);
StateKey<int> reputation = new StateKey<int>(2);

context.State.Set(hasMetNPC, true);
bool met = context.State.Get(hasMetNPC);
```

### 4. Game System Integration
Access any game system through IGameServices:
```csharp
var services = context.GameServices as GameServices;
services?.Inventory.AddItem(itemId, amount);
services?.Quests.StartQuest(questId);
```

### 5. EventBus Publishing
React to dialogue events system-wide:
```csharp
EventBus<DialogueStartedEvent>.Register(binding);
EventBus<ChoiceSelectedEvent>.Register(binding);
EventBus<DialogueEndedEvent>.Register(binding);
```

## Architecture

```
Manual Interaction              Voiceover Dialogue
    ↓                                  ↓
DialogueInteractable      VoiceoverDialogueEngine
    ↓                                  ↓
Player presses E              Auto-play on trigger
    ↓                                  ↓
DialogueManager           VoiceoverDialogueTrigger
    ↓                                  ↓
Dialogue UI                      Dialogue UI
    ↓                                  ↓
    └──────────────┬──────────────┘
                   ↓
            EventBus Events
├── Data/
│   ├── NodeId.cs                      # Type-safe identifiers
│   ├── CommandData.cs                 # Command data structures
│   └── DialogueNode.cs                # Dialogue nodes (Line, Choice)
├── Localization/
│   └── LocalizedEntry.cs              # Multi-language text support
├── Runtime/
│   ├── DialogueState.cs               # Type-safe state (no boxing)
│   ├── DialogueContext.cs             # Execution context
│   ├── DialogueScheduler.cs           # Timed command execution
│   ├── IDialoguePresenter.cs          # Presentation interface & styling
│   ├── DialogueEngine.cs              # Core engine & factories
│   └── Events.cs                      # EventBus events
├── Conditions/
│   └── DialogueCondition.cs           # Condition implementations
├── Commands/
│   └── DialogueCommands.cs            # Built-in commands
├── Presentation/
│   └── DebugDialoguePresenter.cs      # Debug console presenter
├── Integration/
│   ├── DialogueManager.cs             # Main manager (Singleton)
│   ├── DialogueAssetManager.cs        # Asset registry
│   └── DialogueTrigger.cs             # Collision-based triggers
├── Editor/
│   ├── DialogueGraphWindow.cs         # Visual graph editor
│   └── DialogueEditorMenuItems.cs     # Menu items & shortcuts
├── ARCHITECTURE.md                    # Full technical documentation
└── USAGE.md                           # Practical usage guide
```

## Getting Started (5 Minutes)

### 1. Create DialogueManager

```csharp
// In your scene, add a GameObject with DialogueManager component
var manager = gameObject.AddComponent<DialogueManager>();
```

### 2. Create Dialogue Asset

```
Right-click > Assets/Create/Dialogue/Dialogue Graph
```

### 3. Set Up Asset Manager

```
Right-click > Assets/Create/Dialogue/Asset Manager
Assign your graphs and localization table
```

### 4. Add Trigger to NPC

```csharp
npc.AddComponent<DialogueTrigger>();
// Assign graph and trigger conditions in inspector
```

### 5. Test

Play the game and trigger dialogue. Console output shows dialogue progression.

## Core Concepts

### Type-Safe IDs

```csharp
NodeId nodeId = new NodeId(42);           // Strongly typed
TextKey textKey = new TextKey(100);       // No string confusion
SpeakerId speaker = new SpeakerId(5);    // Compile-time safe
StateKey<int> counter = new StateKey<int>(10); // Generic
```

### Dialogue Graph

A graph is a collection of nodes with connections:

```
LineNode (NPC speaks)
    ↓
ChoiceNode (Player chooses)
    ├─→ LineNode (Response A)
    └─→ LineNode (Response B)
```

### State Management

Track variables during conversation (no boxing):

```csharp
StateKey<bool> hasMet = new StateKey<bool>(1);
StateKey<int> reputation = new StateKey<int>(2);

context.State.Set(hasMet, true);
int favor = context.State.Get(reputation);
```

### Commands

Execute game actions from dialogue:

```csharp
// In dialogue node
new SetBoolCommand(hasMetKey, true)
new IncrementIntCommand(reputationKey, 10)
new GiveItemCommand(itemId, amount)
```

### Conditions

Control dialogue branching:

```csharp
// Show this line only if player level >= 5
if (condition.Evaluate(context)) {
    // Show line
}
```

## Usage Examples

### Basic Dialogue

```csharp
var lineNode = new LineNode
{
    Id = new NodeId(0),
    TextKey = new TextKey(1),
    Speaker = new SpeakerId(1),
    NextNodes = new[] { new NodeId(1) }
};
```

### Dynamic Choices

```csharp
var choiceNode = new ChoiceNode
{
    Options = new[]
    {
        new ChoiceOption { 
            TextKey = new TextKey(10),
            NextNode = new NodeId(2)
        },
        new ChoiceOption { 
            TextKey = new TextKey(11),
            NextNode = new NodeId(3)
        }
    }
};
```

### Reputation System

```csharp
StateKey<int> reputationKey = new StateKey<int>(20);

// Increase on positive response
new IncrementIntCommand(reputationKey, 10)

// Check in conditions
if (context.State.Get(reputationKey) >= 100) {
    // Show VIP dialogue
}
```

### Quest Integration

```csharp
[Serializable]
public sealed class StartQuestCommandData : CommandData
{
    public int QuestId;
}

// Execute in dialogue
new StartQuestCommandData { QuestId = 5 }
```

## Customization

### Custom Commands

Create command classes and register them:

```csharp
public sealed class PlayAnimationCommand : IDialogueCommand
{
    public void Execute(in DialogueContext context)
    {
        // Custom logic
    }
}

_commandFactory.Register<AnimationCommandData, PlayAnimationCommand>(
    data => new PlayAnimationCommand(...)
);
```

### Custom Presenter

Implement IDialoguePresenter for custom UI:

```csharp
public sealed class MyUIPresenter : IDialoguePresenter
{
    public void ShowLine(FormattedText text, SpeakerId speaker, ...) { }
    public void ShowChoices(TextKey[] texts, int[] indices) { }
    public void Hide() { }
    public void Clear() { }
}
```

### Extend GameServices

Add custom game system integration:

```csharp
public sealed class GameServices : IGameServices
{
    public IInventory Inventory { get; }
    public IQuestSystem Quests { get; }
    // Add your systems
}
```

## Event System

React to dialogue state changes:

```csharp
var binding = new EventBinding<DialogueStartedEvent>(OnDialogueStarted);
EventBus<DialogueStartedEvent>.Register(binding);

void OnDialogueStarted(DialogueStartedEvent evt)
{
    Debug.Log($"Dialogue started at node {evt.StartNode}");
}
```

Available events:
- `DialogueStartedEvent` - Dialogue begins
- `DialogueAdvancedEvent` - Next node reached
- `ChoiceSelectedEvent` - Choice made
- `DialogueEndedEvent` - Dialogue complete

## Save and Load

Persist dialogue state mid-conversation:

```csharp
// Save
var saveData = new DialogueSaveData
{
    CurrentNode = dialogueManager.CurrentNode,
    State = dialogueManager._engine.State
};

// Load
dialogueManager._engine.State = saveData.State;
```

## Common Patterns

See **[USAGE.md](USAGE.md#common-patterns)** for:
- Greeting with memory
- Dynamic branches
- Reputation systems
- Quest integration

## Troubleshooting

### Dialogue Won't Start
- Check DialogueManager exists and has DialogueEngine initialized
- Verify graph is assigned to trigger
- Check Console for errors

### Text Not Showing
- Verify TextKey IDs match LocalizationTable entries
- Ensure localization table has translations
- Check presenter is assigned

### Choices Not Appearing
- Verify current node is ChoiceNode
- Check presenter implements ShowChoices()
- Verify availableIndices array is not empty

See **[USAGE.md](USAGE.md#troubleshooting)** for more solutions.

## Performance

- **Type-Safe IDs**: Stack allocation, zero overhead
- **State Management**: No boxing, O(1) lookups
- **Graph Caching**: On-demand node lookup caching
- **EventBus**: Batch event publishing
- **Condition Evaluation**: Compile-time specialization

## Best Practices

1. Use type-safe IDs (never strings for core IDs)
2. Separate concerns (don't couple presentation to logic)
3. Compose services through interfaces
4. Cache presenter instances
5. Validate graphs in editor
6. Profile state growth
7. Avoid complex condition nesting
8. Batch related commands
9. Test all dialogue paths
10. Localize text from the start

## Integration with Existing Systems

The dialogue system integrates cleanly with:
- **EventBus** - Publish/subscribe for reactive systems
- **ICondition<T>** - Existing condition framework
- **SaveLoadSystem** - Mid-dialogue state persistence
- **Odin Inspector** - Full inspector support
- **Custom Game Services** - Through IGameServices interface

## Extensions and Examples

The system is designed for easy extension:
- **New Commands** - Extend IDialogueCommand
- **New Conditions** - Extend IDialogueCondition
- **New Presenters** - Implement IDialoguePresenter
- **New Services** - Extend IGameServices

All extension points use composition and interfaces, avoiding tight coupling.

## Documentation

- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Complete technical specification, design patterns, and architectural decisions
- **[USAGE.md](USAGE.md)** - Step-by-step guides, examples, and troubleshooting
- **[Dialogue.txt](Dialogue.txt)** - Original design specification

## Summary

The Dialogue System provides a robust, extensible foundation for narrative interactions in games. Its type-safe design, layered architecture, and clean integration patterns make it suitable for everything from simple NPC greetings to complex branching quest narratives with dynamic state management.

Start with the quick start guide, then explore USAGE.md for detailed patterns and ARCHITECTURE.md for technical deep-dives.

---

**Version**: 1.0  
**Last Updated**: March 2026  
**Namespace**: Extensions.Dialogue  
**Dependencies**: Extensions (EventBus, CustomMath), Sirenix OdinInspector, Unity 2022+

