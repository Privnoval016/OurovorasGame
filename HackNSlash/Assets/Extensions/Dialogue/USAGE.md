# Dialogue System - Complete Usage Guide

## Quick Setup

### 1. Interactive Dialogue (NPC Conversation)

```csharp
// Create NPC
var npc = new GameObject("NPC");
npc.AddComponent<DialogueInteractable>().dialogueGraph = conversationGraph;
npc.AddComponent<SphereCollider>().isTrigger = true;

// Player detects it
player.AddComponent<PlayerInteractor>();

// Result: Player presses E → Dialogue starts
```

### 2. Voiceover Dialogue (Auto-Play Story)

```csharp
// Create voiceover engine
voiceoverEngine.AddComponent<VoiceoverDialogueEngine>();

// Create zone
zone.AddComponent<VoiceoverDialogueTrigger>().dialogueGraph = storyGraph;
zone.AddComponent<SphereCollider>().isTrigger = true;

// Result: Player walks in → Dialogue auto-plays
```

### 3. Setup Dialogue UI

```csharp
// Bottom UI (default)
canvas.AddComponent<Scripts.UI.Dialogue.BottomDialoguePresenter>();

// Combat UI (voiceover)
canvas.AddComponent<Scripts.UI.Dialogue.CombatDialoguePresenter>();

// Manager for switching
uiManager.AddComponent<Scripts.UI.Dialogue.DialogueUIManager>();
```

---

## Creating Dialogue Graphs

### Node Types

**LineNode** - Single line of dialogue
```csharp
new LineNode {
    TextKey = new TextKey(1),      // Text reference
    Speaker = new SpeakerId(1),    // Who's speaking
    Style = new StyleId(0),        // UI style
    Commands = new CommandData[] {}, // Actions to execute
    NextNodes = new[] { new NodeId(1) } // Connections
}
```

**ChoiceNode** - Player choices
```csharp
new ChoiceNode {
    Options = new[] {
        new ChoiceOption {
            TextKey = new TextKey(10),
            NextNode = new NodeId(2)
        },
        new ChoiceOption {
            TextKey = new TextKey(11),
            NextNode = new NodeId(3)
        }
    }
}
```

### Creating Graphs in Code

```csharp
var graph = ScriptableObject.CreateInstance<DialogueGraph>();
graph.GraphName = "MyDialogue";
graph.StartNode = new NodeId(0);
graph.Nodes = new[]
{
    new LineNode { /* ... */ },
    new ChoiceNode { /* ... */ },
    // ...
};
```

### Using Graph Editor

```
Assets > Create > Dialogue > Dialogue Graph
Tools > Dialogue System > Dialogue Graph Editor
```

---

## State Management

Track variables without coupling:

```csharp
// Define keys
StateKey<bool> hasMetNPC = new StateKey<bool>(1);
StateKey<int> reputation = new StateKey<int>(2);
StateKey<float> timeElapsed = new StateKey<float>(3);

// Set in commands
new SetBoolCommand(hasMetNPC, true)
new SetIntCommand(reputation, 50)
new IncrementIntCommand(reputation, 10)

// Check in conditions
if (context.State.Get(hasMetNPC)) { }
```

---

## Executing Game Actions

Commands let dialogue trigger game logic:

### Built-in Commands
- `SetBoolCommand` - Set boolean state
- `SetIntCommand` - Set integer state
- `IncrementIntCommand` - Increment counter
- `DebugLogCommand` - Console output

### Custom Commands

1. Create CommandData:
```csharp
[Serializable]
public sealed class GiveItemCommandData : CommandData
{
    public int ItemId;
    public int Amount = 1;
}
```

2. Create Command:
```csharp
public sealed class GiveItemCommand : IDialogueCommand
{
    private int _itemId, _amount;
    
    public GiveItemCommand(int itemId, int amount)
    {
        _itemId = itemId;
        _amount = amount;
    }
    
    public void Execute(in DialogueContext context)
    {
        var services = context.GameServices as GameServices;
        services?.Inventory.AddItem(_itemId, _amount);
    }
}
```

3. Register in DialogueManager:
```csharp
_commandFactory.Register<GiveItemCommandData, GiveItemCommand>(
    data => new GiveItemCommand(data.ItemId, data.Amount)
);
```

---

## Branching with Conditions

Show different dialogue based on game state:

```csharp
public sealed class HasItemCondition : DialogueConditionBase
{
    private int _itemId;
    private int _minCount;

    public override bool Evaluate(DialogueContext context)
    {
        var services = context.GameServices as GameServices;
        return services?.Inventory.GetItemCount(_itemId) >= _minCount;
    }
}
```

Use in nodes to conditionally show lines.

---

## Localization

Support multiple languages:

1. Create LocalizationTable asset
2. Add entries with translations
3. Reference by TextKey ID
4. System auto-uses Application.systemLanguage

Supported languages:
- English
- Japanese
- Korean
- Spanish
- French

---

## UI Customization

All dialogue UI is fully customizable in Inspector:

### Bottom Dialogue UI

```
DialogueTextConfig:
  ├─ Text Color (RGB)
  ├─ Font Size
  ├─ Typewriter Speed (chars/second)
  └─ Use Typewriter (toggle)

SpeakerNameConfig:
  ├─ Show Speaker Name
  ├─ Name Color
  └─ Font Size

DialoguePanelConfig:
  ├─ Background Color (RGBA)
  ├─ Corner Radius
  ├─ Padding
  ├─ Fade In Duration
  └─ Fade Out Duration
```

### Combat Dialogue UI

```
CombatDialoguePresenter:
  ├─ Display Duration (auto-hide time)
  └─ Same text/panel/speaker configs
```

### Switch Between UIs

```csharp
var uiManager = GetComponent<DialogueUIManager>();
uiManager.SwitchToBottom();  // Normal dialogue
uiManager.SwitchToCombat();  // Combat/voiceover
```

---

## Voiceover System

Auto-playing dialogue that's externally traversable:

### Basic Setup

```csharp
// Engine
voiceoverEngine.AddComponent<VoiceoverDialogueEngine>();

// Trigger zones
zone.AddComponent<VoiceoverDialogueTrigger>().dialogueGraph = graph;
```

### External Control

```csharp
// Advance to next node
voiceoverEngine.AdvanceToNext();

// Jump to specific node (branching)
voiceoverEngine.JumpToNode(nodeId);

// Pause/Resume
voiceoverEngine.Pause();
voiceoverEngine.Resume();

// Stop all playback
voiceoverEngine.StopPlayback();
```

---

## Game System Integration

### Access Inventory from Dialogue

```csharp
var services = context.GameServices as GameServices;
services?.Inventory.AddItem(itemId, amount);
```

### Access Quests from Dialogue

```csharp
services?.Quests.StartQuest(questId);
services?.Quests.CompleteObjective(questId, objectiveId);
```

### Access Custom Systems

Extend IGameServices with any system:

```csharp
public class GameServices : IGameServices
{
    public IInventory Inventory { get; }
    public IQuestSystem Quests { get; }
    public ICombatSystem Combat { get; }
    // Add yours
}
```

---

## EventBus Events

React to dialogue system-wide:

```csharp
// Dialogue starts
EventBus<DialogueStartedEvent>.Register(binding);

// Advances to next node
EventBus<DialogueAdvancedEvent>.Register(binding);

// Choice selected
EventBus<ChoiceSelectedEvent>.Register(binding);

// Dialogue ends
EventBus<DialogueEndedEvent>.Register(binding);
```

---

## Common Patterns

### Reputation System

```csharp
StateKey<int> reputation = new StateKey<int>(1);

// Increase on positive choice
new IncrementIntCommand(reputation, 10)

// Show different dialogue based on reputation
if (context.State.Get(reputation) >= 100) {
    // Show VIP dialogue
}
```

### Quest Integration

```csharp
// Start quest from dialogue
[Serializable]
public sealed class StartQuestCommandData : CommandData
{
    public int QuestId;
}

// Complete objective from voiceover
public sealed class CompleteObjectiveCommand : IDialogueCommand
{
    public void Execute(in DialogueContext context)
    {
        var services = context.GameServices as GameServices;
        services?.Quests.CompleteObjective(_questId, _objectiveId);
    }
}
```

### Inventory Rewards

```csharp
// Give item on dialogue choice
new GiveItemCommand(weaponId, 1)

// Check inventory before showing dialogue
public sealed class HasWeaponCondition : DialogueConditionBase
{
    public override bool Evaluate(DialogueContext context)
    {
        var services = context.GameServices as GameServices;
        return services?.Inventory.HasItem(weaponId) ?? false;
    }
}
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Dialogue won't start | Verify DialogueManager exists, graph assigned |
| Text blank | Check TextKey IDs match LocalizationTable |
| No UI visible | Verify presenter assigned, canvas active |
| Commands not executing | Check factory registration |
| Voiceover doesn't auto-advance | Verify UI presenter assigned |
| Switching UIs not working | Use DialogueUIManager component |

---

## Summary

The dialogue system provides:
- Interactive dialogue for NPCs (manual trigger)
- Voiceover dialogue for narration (auto-play)
- Full UI customization in Inspector
- Deep game system integration via IGameServices
- State management and conditional branching
- EventBus integration for reactive systems
- Production-ready and fully documented

