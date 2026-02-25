# Utility AI — System Reference

## Overview

The Utility AI system selects the best action for an AI agent each tick by scoring every candidate action with a **utility value** (a float in `[0, 1]`), then executing the one with the highest score. Unlike finite state machines, which jump between hard-coded transitions, or behaviour trees, which rely on explicit priority ordering, Utility AI produces emergent, contextually sensitive behaviour by continuously weighing competing factors (health, distance, angle, randomness, etc.) against each other.

---

## Architecture

```
AIBrainUser<TKey>  (MonoBehaviour — your enemy class)
│
├── AIBrain<TKey>          — orchestrates update + selection
│   └── Context<TKey>      — key/value store updated each tick
│       └── Sensor<TKey>   — physics overlap sphere for targets
│
└── List<AIAction<TKey>>   — ScriptableObject actions, each with a Consideration
    └── Consideration      — ScriptableObject that scores [0,1]
        ├── ConstantConsideration
        ├── RandomConsideration
        ├── CurveConsideration      ← reads a float from Context
        ├── InRangeConsideration    ← reads a sensor target + distance
        └── CompositeConsideration  ← combines any of the above
```

### `AIBrain<TKey>`
Pure C# class (not a MonoBehaviour). Owns the `Context` and drives two operations:
- **`UpdateContext()`** — calls `AIBrainUser.OnContextUpdate()` and writes the returned payloads into the context dictionary.
- **`CalculateBestAction()`** — evaluates every action, caches per-action utility scores in `LastUtilityScores`, then calls `AIBrainUser.ExecuteNewAction()` with the winner.

### `AIBrainUser<TKey>`
Abstract `MonoBehaviour` your enemy class inherits. You must implement:
| Method | Responsibility |
|---|---|
| `GetActions()` | Return the list of `AIAction<TKey>` assigned in the Inspector |
| `GetSensor()` | Return the `Sensor<TKey>` component |
| `GetBrain()` | Return the `AIBrain<TKey>` instance |
| `OnContextUpdate()` | Return `ContextPayload<TKey>[]` with the data the brain needs this tick |
| `ExecuteNewAction(...)` | React to the chosen action (e.g. change state) |
| `CurrentActionName` | Return the name of the currently executing action |

Also implements `IAIBrainAccessor` — a non-generic interface used by editor tooling to read brain state without knowing `TKey`.

### `Context<TKey>`
Dictionary of `TKey → object`. Considerations read values with `GetData<TValue>(key)` and the sensor with `GetSensorTarget(key)`. The context is updated every brain tick by `UpdateContext()`. Call `GetSnapshot()` to get all entries as `(string key, string value)` pairs (used by the Brain Debugger window).

### `Sensor<TKey>`
Abstract `MonoBehaviour`. Uses a `SphereCollider` trigger to maintain a `HashSet<Transform>` of nearby objects. Override `HasDetectionTag(T actionKey, Collider other)` to define what constitutes a valid detection for each key value (e.g. `EnemyAIContextKey.Player` → check `CompareTag("Player")`). `GetNearestDetectedObject(T key)` returns the closest matching transform.

---

## Context Keys

Context keys identify both data values (floats written by `OnContextUpdate`) and sensor targets (transforms looked up from the sensor). Every `ConsiderationKey` field in considerations and the `actionKey` field on each action uses the same enum.

### `EnumContextKey` — the new way
A serialisable struct that stores any enum value without requiring a concrete wrapper subclass per enum type. In the Inspector it renders as a two-column popup: first choose the enum type (auto-discovered from all project assemblies), then choose the member.

```csharp
// In CurveConsideration
public EnumContextKey contextKey;  // pick EnemyAIContextKey.SelfHealth in Inspector

// At runtime
float hp = context.GetData<float>(contextKey.GetKey());
```

### Adding a new key
1. Add a member to your `TKey` enum (e.g. `EnemyAIContextKey`).
2. In `OnContextUpdate()` return a `ContextPayload` for it.
3. In the consideration's `EnumContextKey` field, pick the new member from the dropdown.

---

## Considerations

Each `Consideration` is a `ScriptableObject` that evaluates the context and returns a `[0, 1]` score. Create them via `Assets → Create → UtilityAI → Considerations → ...`.

| Type | What it scores |
|---|---|
| `ConstantConsideration` | Always returns `value`. Useful as a base priority. |
| `RandomConsideration` | Returns `Random.Range(minMax.x, minMax.y)` each evaluation. |
| `CurveConsideration` | Reads a float from context via `contextKey`, feeds it through an `AnimationCurve`. |
| `InRangeConsideration` | Reads the nearest sensor target for `targetKey`. Returns 0 if not in range/angle, otherwise maps normalised distance through a curve. |
| `CompositeConsideration` | Chains any number of considerations using arithmetic operations (Average, Multiply, Add, Subtract, Divide, Max, Min). Enable `allMustBeNonZero` to short-circuit to 0 if any child scores 0. |

---

## Actions

Each `AIAction<TKey>` is a `ScriptableObject` with:
- `actionKey` — which context key this action "belongs to" (used e.g. to look up the sensor target in `EnemyAttackAIAction`).
- `consideration` — the `Consideration` that scores this action.
- `CalculateUtility(context)` — calls `consideration.Evaluate(context)` and clamps to `[0, 1]`. If no consideration is assigned, returns 1 (always eligible).

### Enemy actions
All inherit `EnemyAIActionBase` which wraps three lifecycle hooks:

| Method | When called |
|---|---|
| `OnEnter(context, esm)` | Once when this action is selected and `EnemyActing` state is entered |
| `OnUpdate(context, esm)` | Every frame while `EnemyActing` is the current state |
| `OnExit(context, esm)` | Once when a new action pre-empts this one (hitboxes auto-deactivated after) |

Concrete actions: `EnemyIdleAIAction`, `EnemyMoveAIAction`, `EnemyAttackAIAction`.

---

## How `EnemyStateMachine` uses the brain

```
Awake  → AIBrain = new AIBrain<EnemyAIContextKey>(this)
Start  → AIBrain.Initialize()   (creates Context + attaches Sensor)
         TickTimer.OnTick += PerformBestAction   (fires every 0.1 s by default)

PerformBestAction():
  1. AIBrain.UpdateContext()     → payloads written (e.g. SelfHealth)
  2. AIBrain.CalculateBestAction()
       → each action.CalculateUtility(context) called
       → winner: sc.ChangeState(new EnemyActing(actionClone, context))

When attacking:
  SetIsAttacking(true) → ThinkTimer.Pause()   (brain stops selecting new actions)
  SetIsAttacking(false) → ThinkTimer.Resume()
```

The action ScriptableObject is **cloned** before being passed to `EnemyActing` so that per-instance mutable state (e.g. a cached target position) does not bleed between evaluations.

---

## Editor Tooling

### Brain Debugger Window (`Window → UtilityAI → Brain Debugger`)

Select any GameObject with an `AIBrainUser` component. The window has two modes:

**Runtime mode (Play Mode):**
- Left panel — live utility bar per action. Green star = current winner. Click a row to inspect its consideration tree.
- Right panel — recursive consideration tree. Each node shows a coloured score bar (green = high, orange = low). Click *Select* to ping the ScriptableObject.
- Bottom-left — live context key/value table updated every frame.

**Simulation mode (Edit Mode):**
- All `EnumContextKey` fields across every assigned consideration are auto-discovered and listed as editable float sliders (clamped 0–1).
- Click **▶ Run Simulation** to score every action against those values. Results are ranked with a green winner row and score bars.
- The winning action's consideration tree renders on the right with per-node simulated scores.
- Context values persist while you tweak and re-run — no play mode required.

### Consideration ScriptableObject Inspectors

Each consideration type has a custom editor that makes it immediately readable:

| Type | What the editor shows |
|---|---|
| `CurveConsideration` | Context key picker, the AnimationCurve field at full size, and a coloured gradient bar previewing the curve across [0,1]. |
| `InRangeConsideration` | Target key picker, max distance/angle fields, the distance→score curve, and the same gradient preview bar. |
| `ConstantConsideration` | A large slider for the fixed value [0,1] with a live coloured fill bar. |
| `RandomConsideration` | A min/max range slider with a visual strip showing the active range. |
| `CompositeConsideration` | A numbered chain of child considerations with inline operation dropdowns and a `+ Add Operation` button. |

### AIAction ScriptableObject Inspector

Every `AIAction` subclass gets a unified inspector that:
- Draws all fields normally except `consideration`.
- Shows `consideration` as a named asset reference with the resolved type name and direct links to both inspect the consideration and open the Brain Debugger.
- In play mode, searches the scene for any brain that owns this action and renders a live utility score bar.

### `EnumContextKey` property drawer

Any `EnumContextKey` field renders as a two-column inline popup: left selects the enum type (auto-discovered from all user assemblies on domain reload), right selects the member. No `[SerializeReference]` or concrete wrapper subclass needed.

---

## Adding a New Enemy Brain

1. **Define your context key enum** (or reuse `EnemyAIContextKey`):
   ```csharp
   public enum MyEnemyKey { Player, SelfHealth }
   ```

2. **Create a sensor** inheriting `Sensor<MyEnemyKey>`:
   ```csharp
   public class MyEnemySensor : Sensor<MyEnemyKey>
   {
       protected override bool HasDetectionTag(MyEnemyKey key, Collider other)
       {
           return key == MyEnemyKey.Player && other.CompareTag("Player");
       }
   }
   ```

3. **Create action ScriptableObjects** inheriting `AIAction<MyEnemyKey>` (or a game-specific base):
   ```csharp
   [CreateAssetMenu(...)]
   public class MyAttackAction : AIAction<MyEnemyKey>
   {
       // assign consideration in Inspector
   }
   ```

4. **Create your brain user** inheriting `AIBrainUser<MyEnemyKey>`:
   ```csharp
   public class MyEnemy : AIBrainUser<MyEnemyKey>
   {
       public AIBrain<MyEnemyKey> brain;
       public List<AIAction<MyEnemyKey>> actions;
       public MyEnemySensor sensor;

       void Awake() { brain = new AIBrain<MyEnemyKey>(this); }
       void Start()  { brain.Initialize(); }

       public override AIBrain<MyEnemyKey>      GetBrain()     => brain;
       public override List<AIAction<MyEnemyKey>> GetActions() => actions;
       public override Sensor<MyEnemyKey>        GetSensor()   => sensor;
       public override string CurrentActionName                 => /* your current action name */;

       public override ContextPayload<MyEnemyKey>[] OnContextUpdate() => new[]
       {
           (MyEnemyKey.SelfHealth, (object)(hp / maxHp))
       };

       public override void ExecuteNewAction(AIAction<MyEnemyKey> action,
                                              Context<MyEnemyKey> context, float utility)
       {
           // e.g. enter a new state, play animation, etc.
       }
   }
   ```

5. In the Inspector: assign the `Sensor`, add `AIAction` assets to the `actions` list, and set up each action's `Consideration`.

---

## Known Issues / Design Notes

- **`distanceToTarget` bug (fixed):** The old `InRangeConsideration` computed `directionToTarget.ZeroVector3Axis().magnitude` on an already-normalised vector, giving a value near 1 always. It now computes distance from the raw offset before normalisation.
- **`ConsiderationKey` (obsolete):** The old `[SerializeReference] ConsiderationKey` pattern with per-enum concrete subclasses (`EnemyConsiderationKey`) is marked `[Obsolete]`. Existing assets continue to work during migration. New considerations should use `EnumContextKey` directly. Once `EnemyConsiderationKey` is removed from your code the `ConsiderationKey` base class and its shim can be deleted.
- **Action cloning:** `ExecuteNewAction` on `EnemyStateMachine` calls `Instantiate(action)` to clone the ScriptableObject. This is intentional — it prevents cached per-execution state (target positions, timers) from persisting across selections. The clone is discarded when `EnemyActing` exits.
- **Timer tick rate:** The `TickTimer` in `EnemyStateMachine` fires at 0.1 s (10 Hz). Adjust this to trade responsiveness against CPU cost.

