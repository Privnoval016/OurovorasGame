# Utility AI — System Reference

## Overview

The Utility AI system selects the best action for an AI agent each tick by scoring every candidate action with a **utility value** (a float in `[0, 1]`), then executing the one with the highest score. Unlike FSMs or behaviour trees, Utility AI produces emergent, contextually sensitive behaviour by continuously weighing competing factors against each other.

---

## Architecture

```
AIBrainUser              (MonoBehaviour — your enemy class)
│
├── AIBrain              — orchestrates context updates and action selection
│   └── EnemyContext     — key/value store updated each tick
│       └── Sensor       — physics overlap sphere for tracking nearby targets
│
└── List<AIActionBase>   — ScriptableObject actions, each with a Consideration
    └── Consideration    — ScriptableObject that scores [0,1]
        ├── ConstantConsideration
        ├── RandomConsideration
        ├── CurveConsideration      ← reads a float from EnemyContext
        ├── InRangeConsideration    ← reads a sensor target + distance
        └── CompositeConsideration  ← combines any of the above
```

### `AIBrain`
Pure C# class (not a MonoBehaviour). Owns the `EnemyContext` and drives two operations:
- **`UpdateContext()`** — calls `AIBrainUser.OnContextUpdate()` and writes returned payloads into the context.
- **`CalculateBestAction()`** — evaluates every action, caches scores in `LastUtilityScores`, calls `ExecuteNewAction()` with the winner.

### `AIBrainUser`
Abstract `MonoBehaviour` your enemy class inherits. Implement these members:

| Member | Responsibility |
|---|---|
| `GetActions()` | Return `List<AIActionBase>` assigned in the Inspector |
| `GetSensor()` | Return the `ISensor` component |
| `GetBrain()` | Return the live `AIBrain` instance |
| `OnContextUpdate()` | Return `ContextPayload[]` with all data for this tick |
| `ExecuteNewAction(...)` | React to the chosen action (e.g. change state) |
| `CurrentActionName` | Name of the currently executing action |

### `EnemyContext`
The runtime data store. Keyed by `ContextKey` object identity — no strings, no enums in the lookup path.

Preferred API:
```csharp
float hp         = context.Get(EnemyContextKeys.SelfHealthNorm);
bool aggro       = context.Get(EnemyContextKeys.IsAggro);
Transform player = context.GetTarget(EnemyContextKeys.Player);
context.Set(EnemyContextKeys.SelfHealthNorm, 0.8f);
```

### `Sensor`
Abstract `MonoBehaviour`. Uses a trigger `SphereCollider` to maintain a set of nearby transforms. Override `IsValidTarget` and `HasDetectionTag` to define what counts as a detection for each key.

---

## Context Keys — `ContextKey<TValue>`

Context keys are **strongly-typed object references**. The dictionary in `EnemyContext` uses reference equality on `ContextKey` objects — no string comparisons happen at runtime.

```csharp
[AIContextKey]
public static class EnemyContextKeys
{
    public static readonly ContextKey<float> SelfHealthNorm =
        new("float.self_health_norm", "Self Health (norm)", "Float");

    public static readonly ContextKey<Transform> Player =
        new("target.player", "Player", "Target");
}
```

- `ContextKey` — non-generic base, the actual dictionary key.
- `ContextKey<TValue>` — typed subclass, enforces the value type at compile time.
- `ContextKeyRefComparer` — reference-equality comparer used by `EnemyContext`'s dictionary.
- `[AIContextKey]` — tag your static key class with this; the inspector drawer will discover all its keys automatically.

### `ContextKeyField` — inspector picker for Considerations

A serialisable struct used inside Consideration ScriptableObjects. Shows a grouped dropdown of all discovered keys. Stores only the `InternalId` for serialisation, resolves to the live `ContextKey` object at runtime.

```csharp
public ContextKeyField contextKey;

// In Evaluate():
float v = context.GetData<float>(contextKey.ResolveId());
```

---

## Enemy Context Keys (`EnemyContextKeys`)

| Key | Type | Description |
|---|---|---|
| `Player` | `Transform` | Nearest detected player |
| `OtherEnemy` | `Transform` | Nearest other enemy |
| `Self` | `Transform` | This enemy's own transform |
| `SelfHealthNorm` | `float` | Current HP / max HP |
| `DistanceToPlayerNorm` | `float` | XZ distance to player / sensor detection radius |
| `AngleToPlayerNorm` | `float` | 0 = facing player, 1 = player is behind |
| `TimeSinceLastActionNorm` | `float` | Time since last action / `actionCooldownWindow` |
| `SameActionStreakNorm` | `float` | Consecutive repeat count / `streakCap` |
| `IsShielded` | `bool` | Whether the enemy has a shield active |
| `PlayerIsAirborne` | `bool` | Player is jumping or falling |
| `IsStaggered` | `bool` | Enemy is in the stagger state |
| `IsAggro` | `bool` | Enemy is marked as aware by `EntityManager` |
| `PlayerIsAttacking` | `bool` | Player's `canAttack` is false (mid-attack) |
| `PlayerIsDodging` | `bool` | Player's `DodgeTimer` is still running |
| `LastActionName` | `string` | Asset name of the last completed action |
| `SecondLastActionName` | `string` | Asset name of the action before that |
| `SameActionStreak` | `int` | Raw consecutive repeat count |

---

## Considerations

Create via `Assets > Create > UtilityAI > Considerations`.

| Type | Use case |
|---|---|
| `ConstantConsideration` | Always returns a fixed score. Use as a default fallback. |
| `RandomConsideration` | Returns a random value each tick. Adds unpredictability. |
| `CurveConsideration` | Maps a float context value through an `AnimationCurve`. |
| `InRangeConsideration` | Scores based on XZ distance to a sensor target, with optional angle cone. |
| `CompositeConsideration` | Combines multiple considerations: Multiply, Average, Min, Max. |

All considerations pick their input key via a `ContextKeyField` grouped dropdown — no raw strings.

---

## Built-in Enemy Actions

| Action | Description |
|---|---|
| `EnemyIdleAIAction` | Plays idle animation. Use as a low-utility fallback. |
| `EnemyMoveAIAction` | Chases nearest player. Exposes `speedMultiplier`. |
| `EnemyAttackAIAction` | Triggers an `EnemyAttack` via `OnEnemyEvents`. Faces target while attacking. |

### Adding a new action

1. Create a class inheriting `EnemyAIActionBase`.
2. Implement `OnEnemyEnter`, `OnEnemyUpdate`, `OnEnemyExit` — receive `EnemyContext` and `EnemyStateMachine`.
3. Add `[CreateAssetMenu]`.
4. Read context: `context.Get(EnemyContextKeys.X)`, `context.GetTarget(EnemyContextKeys.Player)`.
5. Drive movement via `EnemyStateMachine` methods: `MoveToDestination`, `TurnToPosition`, `Brake`.

---

## Action History & Combo Anti-Repeat

`EnemyStateMachine` automatically tracks action history:
- `_lastActionName` / `_secondLastActionName` — asset names of the last two completed actions.
- `_sameActionStreak` — how many times the same action has fired consecutively.
- `_timeSinceLastAction` — time elapsed since the last action completed.

These are written to context each tick. Use a `CurveConsideration` on `SameActionStreakNorm` with a descending curve to naturally penalise repeated actions and produce combo variety.

---

## Brain Debugger (Editor Window)

Open via **Window → UtilityAI → Brain Debugger**.

**Live mode (play mode):**
- Select a GameObject with an `EnemyStateMachine` (or any `AIBrainUser`) in the Inspector.
- See real-time utility score bars for every action, colour-coded green for the winner.
- See the full live context snapshot (all key/value pairs).
- Click an action row to inspect its consideration in detail.

**Simulate mode (edit mode):**
- Select the same GameObject outside play mode.
- All `ContextKey` fields across all discovered `[AIContextKey]` classes are listed as editable float sliders.
- Click **Run Simulation** to score all actions against your manually set values and preview the winner.

---

## Setup Checklist

1. Add `EnemyStateMachine` to the enemy root GameObject.
2. Add `EnemySensor` to the same root. Set `detectionRadius`. A `SphereCollider` (trigger) is added automatically.
3. Create action ScriptableObjects: `Assets > Create > Enemy > AIActions > ...`. Assign a `Consideration` to each.
4. Assign all action assets to the `Actions` list on `EnemyStateMachine` in the Inspector.
5. Set `actionCooldownWindow` (seconds) and `streakCap` on `EnemyStateMachine`.
6. Assign the `EnemySensor` reference on `EnemyStateMachine`.
7. Ensure an `AstarPath` component with a baked navmesh graph is present in the scene for movement actions.
