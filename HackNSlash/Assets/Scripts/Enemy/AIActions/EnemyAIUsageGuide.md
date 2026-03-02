# Enemy Utility AI — Usage Guide

This document is the authoritative reference for setting up enemy AI actions and considerations
using the utility AI system. It covers every context key in detail, how to use each consideration
type correctly, common mistakes, navigation setup, and worked examples.

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Context Keys — Complete Reference](#context-keys)
3. [Consideration Types — When to Use Each](#consideration-types)
4. [Common Mistakes](#common-mistakes)
5. [Navigation Setup](#navigation-setup)
6. [Worked Examples](#worked-examples)
7. [Brain Debugger](#brain-debugger)

---

## System Overview

Each enemy has an `EnemyStateMachine` (inherits `AIBrainUser`) with:

- A list of `AIActionBase` ScriptableObjects (your enemy actions).
- A `TickTimer` at 0.1 s — `PerformBestAction()` fires every tick.
- An `EnemySensor` that tracks nearby transforms.
- An `EnemyContext` that holds all runtime values.

On each tick the brain:
1. Calls `OnContextUpdate()` to write fresh values into `EnemyContext`.
2. Calls `Evaluate()` on every action's `consideration` field.
3. Executes whichever action scored highest.

The `consideration` field on each action ScriptableObject is a
`[SerializeReference] Consideration` — assign it in the Inspector using the
**Change Type** button. The Brain Debugger window shows live scores at runtime.

---

## Context Keys

All keys live in `EnemyContextKeys`. They are discoverable from the Inspector
via the `ContextKeyField` dropdown. Keys are grouped by category.

### Target Keys — `ContextKey<Transform>`

These are resolved via the `EnemySensor` and return a `Transform` reference.
**Only use these with `InRangeConsideration` or `TargetExistsConsideration`.**
Do not use them with `CurveConsideration` or `ThresholdConsideration`.

| Key | What it is | How it is obtained |
|-----|------------|--------------------|
| `Player` | The player character Transform. | `EnemySensor.GetNearestTarget(Player)` — scans all detected colliders tagged as the player. |
| `OtherEnemy` | The nearest other enemy Transform. | `EnemySensor.GetNearestTarget(OtherEnemy)` — useful for flanking or spacing logic. |
| `Self` | This enemy's own Transform. | Always valid. Useful for self-distance checks in `InRangeConsideration`. |

---

### Float Keys — `ContextKey<float>`

These are normalised floats written by `OnContextUpdate()` every AI tick.
**Use these with `CurveConsideration` or `ThresholdConsideration`.**
Do not pass them to `InRangeConsideration.targetKey` — that field requires a Transform key.

#### `SelfHealthNorm`
- **Value:** `currentHealth / maxHealth` → `[0, 1]`.
- **0 = dead, 1 = full health.**
- **Use with:** `CurveConsideration` to make actions more or less likely as health drops.
- **Example curve:** Ascending left-to-right makes an action preferred when healthy.
  Descending makes it preferred when low health (good for desperation attacks or flee actions).
- **Prefer over:** Nothing — it is the only health signal.

#### `DistanceToPlayerNorm`
- **Value:** XZ distance to the player divided by `sensor.detectionRadius` → `[0, 1]`.
- **0 = player is on top of enemy, 1 = player is at the edge of sensor range.**
- **Use with:** `CurveConsideration`.
  - A curve that peaks at ~0.3 makes the action preferred at close range.
  - A curve that peaks at ~0.8 makes the action preferred at long range.
- **IMPORTANT — do not use with `InRangeConsideration`.**
  `InRangeConsideration` is for Transform keys only. Using `DistanceToPlayerNorm`
  there will always return 0. See [Common Mistakes](#common-mistakes).
- **Prefer over `InRangeConsideration`** for most distance checks because it is already
  computed each tick and costs nothing extra. Use `InRangeConsideration` only when you
  also need angle gating.

#### `AngleToPlayerNorm`
- **Value:** `(1 - dot(forward, toPlayer)) / 2` → `[0, 1]`.
- **0 = player is directly in front, 1 = player is directly behind.**
- **Use with:** `CurveConsideration` or `ThresholdConsideration`.
  - Pair with `DistanceToPlayerNorm` in a `CompositeConsideration` (Multiply) to only
    allow attacks when the player is both close AND in front.
- **Prefer over `InRangeConsideration.maxAngle`** when you want the angle to smoothly
  influence the score rather than hard-gate it.

#### `TimeSinceLastActionNorm`
- **Value:** Time since the last action completed, normalised against
  `EnemyStateMachine.actionCooldownWindow` → `[0, 1]`.
- **0 = action just finished, 1 = full cooldown window has elapsed.**
- **Use with:** `CurveConsideration` or `ThresholdConsideration`.
  - A threshold `> 0.8, scoreIfTrue = 1` makes an action only eligible after most of
    the cooldown has passed, preventing the same action from firing repeatedly.
  - An ascending curve makes actions progressively more likely over time — good for
    ensuring the enemy eventually acts even if other scores are low.
- **Pair with:** `SameActionStreakNorm` to prevent both spam and cooldown-waiting loops.

#### `SameActionStreakNorm`
- **Value:** Consecutive identical action count normalised against
  `EnemyStateMachine.streakCap` → `[0, 1]`.
- **0 = first time this action has been chosen, 1 = fired `streakCap` times in a row.**
- **Use with:** `ThresholdConsideration` (inverted: `scoreIfFalse = 1, scoreIfTrue = 0`)
  with a high threshold so the action is penalised when overused.
- **Best practice:** Add a `CompositeConsideration` (Multiply) combining the action's
  main logic score with a `ThresholdConsideration` on this key. This prevents patterns
  from becoming predictable.

---

### Bool Keys — `ContextKey<bool>`

Written each tick as `true`/`false`. **Use with `BoolConsideration`.**

| Key | Value | Notes |
|-----|-------|-------|
| `IsShielded` | Whether this enemy's shield component is active. | Always `false` until shield is implemented. |
| `PlayerIsAirborne` | `true` if the player is jumping or falling. | Use to enable anti-air attacks. |
| `IsStaggered` | `true` if the enemy is currently in a stagger state. | Use `BoolConsideration(invert=true)` to gate all actions while staggered. |
| `IsAggro` | `true` when `IsAttacking` is `true` on the state machine. | Gate non-combat actions: only wander/idle when not aggro. |
| `PlayerIsAttacking` | `true` while the player's `canAttack` flag is false. | Use for parry/counter actions. |
| `PlayerIsDodging` | `true` while the player's dodge timer is running. | Use to delay or cancel chase actions. |

---

### History Keys — for `StringMatchConsideration`

| Key | Value | Notes |
|-----|-------|-------|
| `LastActionName` | The asset name of the last completed action. | Match against a specific action name to chain combos. |
| `SecondLastActionName` | The action before that. | Detect two-step patterns. |
| `SameActionStreak` | Raw int count of consecutive identical actions. | Use `ThresholdConsideration` after casting to float via context. |

**Note on `LastActionName` / `SecondLastActionName`:** The value is the ScriptableObject
asset name (e.g. `"EnemyHeavyAttack"`). Set `matchValue` in `StringMatchConsideration`
to exactly that string. These keys enable combo sequencing — a heavy attack action can
score high only when the last action was a light attack.

---

## Consideration Types

### `CurveConsideration`
**Use for: any normalised float key.**

Evaluates the curve at the float value and returns the curve's Y at that point.
The X axis is the context value [0,1]. The Y axis is the output score [0,1].

- Set `contextKey` to any Float key (e.g. `DistanceToPlayerNorm`).
- Author the curve to describe how desirable the action is at each distance.
- A flat curve at Y=1 is equivalent to `ConstantConsideration(1)`.

---

### `InRangeConsideration`
**Use for: `ContextKey<Transform>` keys only (Player, OtherEnemy, Self).**

Asks the sensor for the target's Transform, computes XZ distance, optionally checks
angle, then evaluates the curve at `distance / maxDistance`.

- `targetKey` → must be a **Transform key** (`Player`, `OtherEnemy`, `Self`).
- `maxDistance` → hard cap in world units. Returns 0 outside this range.
- `maxAngle` → optional angle cone in degrees (0–360). 360 disables angle check.
- `curve` → evaluated at `dist / maxDistance`. A curve descending left-to-right
  scores 1 when close, 0 when at max range.

**Do not set `targetKey` to `DistanceToPlayerNorm`** — that is a float key, not a
Transform key. Doing so always returns 0. Use `CurveConsideration` with
`DistanceToPlayerNorm` for distance scoring without the Transform lookup overhead.

---

### `ThresholdConsideration`
**Use for: float keys where you want a binary or stepped response.**

Returns `scoreIfTrue` or `scoreIfFalse` based on a comparison against a fixed threshold.

- `GreaterThan 0.8, scoreIfTrue=1` → action enabled only after 80% of the cooldown.
- `LessThan 0.3, scoreIfTrue=1` → action preferred when health is below 30%.
- Compose with other considerations via `CompositeConsideration` to combine conditions.

---

### `BoolConsideration`
**Use for: bool keys.**

Returns `1` if the bool matches the expected value, `0` otherwise.
Set `invert=true` to return `1` when the bool is `false`.

**Common pattern — gate an action:**
Use in a `CompositeConsideration` with `allMustBeNonZero=true` and your main
scoring consideration. The whole composite returns 0 if the bool gate fails.

---

### `TargetExistsConsideration`
**Use for: Transform keys, to check sensor visibility.**

Returns `1` if the sensor can detect the target, `0` if not.
Use as a gate before any action that requires the player to be visible.

---

### `StringMatchConsideration`
**Use for: `LastActionName` / `SecondLastActionName` history keys.**

Returns `scoreIfMatch` when the string equals `matchValue`, otherwise `scoreIfNoMatch`.
Default is `scoreIfMatch=0, scoreIfNoMatch=1` — meaning this consideration normally
passes, and is used in a composite to penalise repeated actions.

---

### `CompositeConsideration`
**Use to combine multiple considerations.**

Click **`+ Compose`** on any existing consideration to wrap it. Then use
**`+ Add Child Consideration`** to add more conditions.

- `allMustBeNonZero=true` → short-circuit AND: if any child is 0, the whole composite is 0.
  Use this for hard gates (must have target, must not be staggered, etc.).
- `allMustBeNonZero=false` → combine with the chosen operation per child:
  - `Multiply` → both scores must be high (most common for combining distance + cooldown).
  - `Average` → softer combination.
  - `Min` → the weakest condition dominates (conservative).
  - `Max` → the strongest condition dominates (permissive).

---

### `ConstantConsideration`
Returns a fixed value. Use to set a baseline utility for fallback/idle actions so they
always have a non-zero chance of winning when nothing else is preferred.

---

### `LinkedAssetConsideration`
Click **`↗ Link`** or paste-link from clipboard. Points to a `ConsiderationAsset`
ScriptableObject. All actions sharing the same asset update together when you edit the asset.
Use for shared cooldown gates or health conditions reused across many actions.

---

## Common Mistakes

### ❌ Using `InRangeConsideration` with `DistanceToPlayerNorm`

`InRangeConsideration.targetKey` must be a `ContextKey<Transform>`. If you set it to
`DistanceToPlayerNorm` (which is a `ContextKey<float>`), the cast `as ContextKey<Transform>`
returns null, the sensor returns null, and the consideration **always returns 0**.

**Fix:** Use `CurveConsideration` with `contextKey = DistanceToPlayerNorm` for
distance-based scoring. Only use `InRangeConsideration` when you need the actual
Transform lookup from the sensor (e.g. to check angle as well as distance).

---

### ❌ Forgetting a fallback action

If all actions score 0 (e.g. all gates fail), the enemy does nothing.
Always have a fallback idle or wander action with a `ConstantConsideration(0.05)`
so the brain always has something to execute.

---

### ❌ No `TargetExistsConsideration` gate on attack actions

If the sensor has not detected the player, attack actions that call `GetTarget(Player)`
will get null. Add a `TargetExistsConsideration` (targetKey = `Player`) as the first
child in a composite with `allMustBeNonZero=true` to gate all offensive actions.

---

### ❌ Same consideration value on all actions

If all actions have `ConstantConsideration(1)`, the brain picks actions in an arbitrary
but deterministic order. Use different values or different consideration types to give
the brain meaningful differentiation.

---

### ❌ Considerations not showing up in Inspector

The `consideration` field uses `[SerializeReference]`. If it shows as empty, click
**Change Type** in the Inspector to assign a type. Null consideration = utility of 1.0
(the action always wins by default).

---

## Navigation Setup

The enemy uses two components: `NavPathPlanner` (A* path planning) and `NavMotor`
(rigidbody movement). Both require a `NavConfig` ScriptableObject assigned in the Inspector.

### Required Components on the Enemy GameObject

| Component | Purpose |
|-----------|---------|
| `NavPathPlanner` | Requests A* paths via `Seeker`, tracks waypoints, exposes `GetSteeringDirection()`. |
| `NavMotor` | Executes movement forces on the `Rigidbody`. |
| `Seeker` | A* Pathfinding package component. Required by `NavPathPlanner`. |
| `Rigidbody` | Required by `NavMotor`. |
| `NavConfig` (SO) | Assigned to both `NavPathPlanner.config` and `NavMotor.config`. |

### NavConfig Fields

| Field | Default | Description |
|-------|---------|-------------|
| `repathRate` | 0.35 s | How often the path is recalculated automatically. Lower = more responsive, higher = cheaper. |
| `waypointAcceptanceRadius` | 0.6 m | Distance to advance to the next waypoint. Increase if the enemy gets stuck on corners. |
| `arrivalRadius` | 0.25 m | Distance to the final destination to consider arrived. |
| `destinationChangeTolerance` | 0.25 m | Minimum destination shift before an immediate repath is triggered. Prevents repathing every frame for a moving target. |
| `maxSpeed` | 5 m/s | Movement speed ceiling. |
| `accelerationForce` | 40 | How quickly the enemy reaches max speed. |
| `decelerationForce` | 40 | How quickly the enemy stops. |
| `rotationSpeed` | 360 °/s | Turn speed. |

### A* Graph Setup

1. Add an **A\* Pathfinding Project** GameObject to the scene.
2. Add a **Grid Graph** (or Recast Graph for complex geometry).
3. Set **Collision Testing** to match your geometry layer.
4. Click **Scan** in the editor — this bakes the walkable area.
5. Make sure the enemy GameObject is **on the graph** at runtime.
   If the enemy starts off-graph, the seeker will fail silently.

### How `MoveToDestination` Works

```
EnemyStateMachine.MoveToDestination(position, speedMultiplier)
  → ts.nav.SetDestination(position)       // triggers repath if destination changed
  → dir = ts.nav.GetSteeringDirection()   // returns direction to next waypoint
  → ts.motor.Move(dir, speedMultiplier)   // applies forces via Rigidbody
```

`SetDestination` is safe to call every frame with a moving target. It only triggers
a new path request when the destination has moved more than `destinationChangeTolerance`
from the last known destination, or when no path exists yet.

---

## Worked Examples

### Example 1 — Basic Melee Attack

Goal: attack the player when close and facing them.

```
CompositeConsideration (allMustBeNonZero = true)
  ├─ FIRST: TargetExistsConsideration (targetKey = Player)         — gate: player must be visible
  ├─ OP Multiply: InRangeConsideration
  │     targetKey   = Player
  │     maxDistance = 3 m
  │     maxAngle    = 90°
  │     curve       = descending (1 at 0, 0 at 1)                  — close range preferred
  └─ OP Multiply: ThresholdConsideration
        contextKey  = TimeSinceLastActionNorm
        comparison  = GreaterThan
        threshold   = 0.6
        scoreIfTrue = 1, scoreIfFalse = 0                          — cooldown gate
```

---

### Example 2 — Retreat When Low Health

Goal: move away from the player when below 30% health.

```
CompositeConsideration (allMustBeNonZero = false)
  ├─ FIRST: ThresholdConsideration
  │     contextKey  = SelfHealthNorm
  │     comparison  = LessThan
  │     threshold   = 0.3
  │     scoreIfTrue = 1, scoreIfFalse = 0
  └─ OP Multiply: CurveConsideration
        contextKey  = DistanceToPlayerNorm
        curve       = ascending (0 at 0, 1 at 1)                   — prefer when player is far
```

---

### Example 3 — Combo Finisher

Goal: heavy attack scores high only after a light attack.

```
CompositeConsideration (allMustBeNonZero = true)
  ├─ FIRST: StringMatchConsideration
  │     contextKey    = LastActionName
  │     matchValue    = "EnemyLightAttack"
  │     scoreIfMatch  = 1
  │     scoreIfNoMatch = 0
  └─ OP Multiply: InRangeConsideration
        targetKey   = Player
        maxDistance = 4 m
        curve       = descending
```

---

### Example 4 — Idle/Wander Fallback

Goal: low-priority fallback so the enemy always does something.

```
ConstantConsideration (value = 0.05)
```

Set this on your `EnemyIdleAIAction` or `EnemyWanderAIAction`. Because it never exceeds
0.05, any real consideration on any other action will outcompete it.

---

## Brain Debugger

Open via **Window → UtilityAI → Brain Debugger**.

- Select an enemy GameObject in the Hierarchy while in Play mode.
- **Actions panel** shows each action's live utility score with a bar graph.
  The winning action is marked with ★.
- **Live Context panel** shows every key and its current value.
- **Consideration Tree panel** (click an action) shows the recursive breakdown of
  how the score was computed, with per-node values.
- **Simulate mode** (editor, not playing) lets you manually set context values and
  run the brain to predict which action would win — useful for tuning without
  needing to play the game.

