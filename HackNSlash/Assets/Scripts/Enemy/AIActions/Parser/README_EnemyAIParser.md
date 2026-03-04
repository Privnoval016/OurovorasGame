# Enemy AI Definition Language — User Guide

`Scripts/Enemy/AIActions/Parser/`

This document covers everything you need to write, import, and export enemy AI
definitions for this project. It is complementary to the generic parser README
at `Extensions/UtilityAI/Parser/README_Parser.md`.

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Import / Export Tool](#import--export-tool)
3. [Action Types — Reference](#action-types)
4. [Movement Strategies](#movement-strategies)
5. [Context Keys for Considerations](#context-keys)
6. [DamageableComponent Consideration](#damageablecomponent-consideration)
7. [Complete Example: Calliope](#complete-example-calliope)
8. [Adding New Action Types](#adding-new-action-types)
9. [Animation Decoupling](#animation-decoupling)

---

## Quick Start

1. Create a text file anywhere in the project (e.g. `Enemy/AIData/BasicEnemy.aidef`).
2. Write your actions using the grammar described below.
3. In Unity, create a GameObject and add the **AI Def Import Export Tool** component.
4. Set **Source File** to your `.aidef` text asset.
5. Set **Output Directory** to where the assets should land (e.g. `Assets/Enemy/AIActions/Generated`).
6. Press **Import Actions**.
7. Drag the generated assets into your `EnemyStateMachine.actions` list.

**Reimporting / Overwriting:** If assets already exist at the output path with the same names,
the importer will overwrite them in-place using `EditorUtility.CopySerialized`. All scene
references to those assets will automatically reflect the new data — no need to reassign.

---

## Import / Export Tool

The `AiDefImportExportTool` MonoBehaviour lives at the root of this folder.
It is editor-only (`#if UNITY_EDITOR`).

### Fields

| Field | Description |
|-------|-------------|
| **Source File** | TextAsset (`.aidef` or `.txt`) to parse on import. |
| **Output Directory** | Project-relative folder for generated ScriptableObject assets. |
| **Last Import Result** | Read-only status shown after the last import attempt. |
| **Actions To Export** | List of `AIActionBase` assets to serialise on export. |
| **Export Path** | Project-relative path for the output `.aidef` file. |
| **Last Export Result** | Read-only status shown after the last export attempt. |

### Buttons

| Button | What it does |
|--------|-------------|
| **Import Actions** | Parse Source File, generate or overwrite ScriptableObject assets. |
| **Export Actions** | Serialise Actions To Export to a text file at Export Path. |
| **Validate Source File Only** | Lex + parse without creating any assets. Shows action count on success. |

---

## Action Types

### `EnemyIdle`
Plays the idle animation. No parameters beyond the consideration.

### `EnemyMovement`
All locomotion — chase, strafe, orbit, retreat, back-jump, wander, idle wait, charge.
See [Movement Strategies](#movement-strategies) for the full parameter list.

### `EnemyAttack`
Creates an `EnemyAttackAIAction` asset.
**Attack data (animations, hitboxes, damage) is not set by the parser — assign it in the Inspector after generation.**

### `EnemyStunned`
Fully immobilises the enemy. No movement, velocity zeroed every frame.
Used for shield-break vulnerability windows and similar states.

```
action "EnemyShieldBreakStunned" : EnemyStunned {
  exitCondition = ShieldRestored   // Duration | ShieldRestored | Never
  duration      = 2.0              // only used when exitCondition = Duration

  consideration composite {
    allMustBeNonZero = true
    first bool { key = "bool.is_shielded"  invert = true }
    rest [
      multiply: constant { value = 10 }   // overwhelming score — always wins while stunned
    ]
  }
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `exitCondition` | ShieldRestored | When the stun self-completes. Usually `Never` or `ShieldRestored` — the brain re-evaluates and picks a new action the moment the consideration drops to 0. |
| `duration` | 2.0 | Used only when `exitCondition = Duration`. |

**Note on constants > 1:** `ConstantConsideration.value` is not capped at 1.
A value of 10 ensures the stun action always wins while the shield is broken,
regardless of what other actions score.

---

## Movement Strategies

Set `strategy = <name>` inside an `EnemyMovement` block.

### `Chase`
Follows the A* path to the player. Completes when within `stopDistance`.
| Property | Default | Description |
|----------|---------|-------------|
| `speed` | 1.0 | Multiplier on NavConfig.maxSpeed. |
| `stopDistance` | 2.0 | XZ distance at which the action completes. |
| `faceTarget` | true | Rotate to face the player while chasing. |

### `Strafe`
Move perpendicular to the player while facing them.
| Property | Default | Description |
|----------|---------|-------------|
| `strafeSign` | 1.0 | +1 = right, -1 = left. |
| `speed` | 0.7 | Speed multiplier. |
| `duration` | 1.5 | Seconds before brain re-evaluates. |

### `Orbit`
Circle the player at `NavConfig.circleRadius`.
| Property | Default | Description |
|----------|---------|-------------|
| `angularSpeed` | 60.0 | Degrees per second. Negative = clockwise. |
| `duration` | 2.0 | Seconds before brain re-evaluates. |

### `Retreat`
Move directly away from the player.
| Property | Default | Description |
|----------|---------|-------------|
| `speed` | 1.0 | Speed multiplier. |
| `desiredDist` | 6.0 | Stop retreating when this far from the player. |
| `maxDuration` | 2.0 | Hard cap on duration. |

### `BackJump`
Physical leap backward.
| Property | Default | Description |
|----------|---------|-------------|
| `settleTime` | 0.1 | Seconds to wait after landing before completing. |

### `Wander`
Pick random nearby points and walk to them.
| Property | Default | Description |
|----------|---------|-------------|
| `duration` | 4.0 | Seconds before brain re-evaluates. |

### `Idle` (wait in place)
Stand still, optionally facing the player.
| Property | Default | Description |
|----------|---------|-------------|
| `duration` | 1.0 | Seconds to wait. |
| `facePlayer` | true | Rotate to face the player while waiting. |

### `Charge`
Direct line rush, no pathfinding.
| Property | Default | Description |
|----------|---------|-------------|
| `speed` | 1.5 | Speed multiplier. |
| `stopDistance` | 1.2 | Stop when within this distance. |
| `maxDuration` | 1.5 | Hard cap. |

### Common EnemyMovement parameters (all strategies)
| Property | Default | Description |
|----------|---------|-------------|
| `stuckTimeout` | 1.2 | Seconds below `speedThreshold` before triggering an unstuck side-step. |
| `speedThreshold` | 0.3 | XZ speed (m/s) below which the enemy is considered stuck. |
| `unstuckDist` | 1.5 | How far to side-step when unsticking. |
| `unstuckDuration` | 0.3 | How long the unstuck step lasts. |
| `maxActionDur` | 6.0 | Hard cap on total action duration. 0 = no cap. |

---

## Context Keys

### Transform keys (use with `inrange`, `targetexists`)
| InternalId | Description |
|------------|-------------|
| `target.player` | The player character Transform. |
| `target.other_enemy` | Nearest other enemy Transform. |
| `target.self` | This enemy's own Transform. |

### Float keys (use with `curve`, `threshold`)
| InternalId | Range | Description |
|------------|-------|-------------|
| `float.self_health_norm` | [0,1] | currentHealth / maxHealth. |
| `float.dist_to_player_norm` | [0,1] | XZ distance / sensor radius. |
| `float.angle_to_player_norm` | [0,1] | 0 = facing, 1 = behind. |
| `float.time_since_last_action_norm` | [0,1] | Time since last action / actionCooldownWindow. |
| `float.same_action_streak_norm` | [0,1] | Consecutive identical action count / streakCap. |

### Bool keys (use with `bool`)
| InternalId | Description |
|------------|-------------|
| `bool.is_shielded` | Enemy shield is active (ShieldComponent.CurrentShieldPercentage > 0). |
| `bool.player_is_airborne` | Player is jumping or falling. |
| `bool.is_staggered` | Enemy is currently staggered. |
| `bool.is_aggro` | Enemy is in attacking/aggro state. |
| `bool.player_is_attacking` | Player is currently in an attack. |
| `bool.player_is_dodging` | Player is in a dodge/i-frame window. |

### String/history keys (use with `stringmatch`)
| InternalId | Description |
|------------|-------------|
| `history.last_action_name` | Asset name of the last completed action. |
| `history.second_last_action_name` | Asset name of the action before that. |

---

## DamageableComponent Consideration

Beyond the context key approach, you can query any `IDamageableComponent` directly
in a consideration via the `DamageableComponentConsideration` type.

The component is picked via a **dropdown** in the Inspector (rendered by
`DamageableComponentTypeDrawer`). The dropdown shows all concrete `IDamageableComponent`
types in the project — no string typing required.

In `.aidef` files, `DamageableComponentConsideration` is **not parseable from text** because
it requires an assembly-qualified type name that is too fragile to hardcode manually.
For text-driven setups, use the `bool.is_shielded` context key (which reads `ShieldComponent`
automatically). Use the inspector consideration directly when you want fine-grained access
to a custom component not covered by a context key.

Every `IDamageableComponent` you create must implement `Evaluate() : float` to expose
its state. Return 1 for "fully active" and 0 for "inactive". The consideration passes this
through an optional curve and an invert toggle.

---

## Complete Example: Calliope

`AiDefs/Calliope.aidef` is a fully documented example implementing the Calliope miniboss:

- `EnemyShieldBreakStunned` — shield-break override with score 10
- `EnemyApproachPlayer` — Chase at far range
- `EnemyStrafeReposition` — Strafe at mid range
- `EnemySweepAttack` — primary melee attack with enrage scaling
- `EnemyOverheadStrike` — facing-sensitive vertical slam
- `EnemyForwardLunge` — gap-closer for retreating players
- `EnemyIdleFallback` — constant 0.05 safety net

After importing, assign attack data (hitboxes, animations, damage) to the three
attack assets in the Inspector.

---

## Adding New Action Types

1. Create your `ScriptableObject` action class inheriting `EnemyAIActionBase`.
2. Create a class implementing `IActionCodeGen` (see `EnemyActionCodeGens.cs`).
3. Register it in `EnemyAiCodeGeneratorFactory.Create()`.
4. Existing `.aidef` files are not affected.

---

## Animation Decoupling

`EnemyMovementAIAction` has an **Anim Override** field in the Inspector.
- When **null**, strategies use the `EnemyAnimData` assigned on the `EnemyStateMachine`.
- When set, strategies use that override instead.

This means one `EnemyMovementAIAction` asset can be shared across enemy types
with different rigs.


`Scripts/Enemy/AIActions/Parser/`

This document covers everything you need to write, import, and export enemy AI
definitions for this project. It is complementary to the generic parser README
at `Extensions/UtilityAI/Parser/README_Parser.md`.

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Import / Export Tool](#import--export-tool)
3. [Action Types — Reference](#action-types)
4. [Movement Strategies](#movement-strategies)
5. [Context Keys for Considerations](#context-keys)
6. [Complete Example File](#complete-example-file)
7. [Adding New Action Types](#adding-new-action-types)
8. [Animation Decoupling](#animation-decoupling)

---

## Quick Start

1. Create a text file anywhere in the project (e.g. `Enemy/AIData/BasicEnemy.aidef`).
2. Write your actions using the grammar described below.
3. In Unity, create a GameObject and add the **AI Def Import Export Tool** component.
4. Set **Source File** to your `.aidef` text asset.
5. Set **Output Directory** to where the assets should land (e.g. `Assets/Enemy/AIActions/Generated`).
6. Press **Import Actions**.
7. Drag the generated assets into your `EnemyStateMachine.actions` list.

---

## Import / Export Tool

The `AiDefImportExportTool` MonoBehaviour lives at the root of this folder.
It is editor-only (`#if UNITY_EDITOR`).

### Fields

| Field | Description |
|-------|-------------|
| **Source File** | TextAsset (`.aidef` or `.txt`) to parse on import. |
| **Output Directory** | Project-relative folder for generated ScriptableObject assets. |
| **Last Import Result** | Read-only status shown after the last import attempt. |
| **Actions To Export** | List of `AIActionBase` assets to serialise on export. |
| **Export Path** | Project-relative path for the output `.aidef` file. |
| **Last Export Result** | Read-only status shown after the last export attempt. |

### Buttons

| Button | What it does |
|--------|-------------|
| **Import Actions** | Parse Source File, generate ScriptableObject assets. |
| **Export Actions** | Serialise Actions To Export to a text file at Export Path. |
| **Validate Source File Only** | Lex + parse without creating any assets. Shows action count on success. |

---

## Action Types

### `EnemyIdle`
Plays the idle animation. No parameters beyond the consideration.
```
action "Idle_Fallback" : EnemyIdle {
  consideration constant { value = 0.05 }
}
```

### `EnemyMovement`
All locomotion — chase, strafe, orbit, retreat, back-jump, wander, idle wait, charge.
See [Movement Strategies](#movement-strategies) for the full parameter list.

### `EnemyAttack`
Creates an `EnemyAttackAIAction` asset.
**Attack data (animations, hitboxes, damage) is not set by the parser — assign it in the Inspector after generation.**
Only the consideration is wired up.
```
action "MeleeAttack" : EnemyAttack {
  consideration inrange {
    key     = "target.player"
    maxdist = 2.5
    points  = [[0, 1], [1, 0]]
  }
}
```

---

## Movement Strategies

Set `strategy = <name>` inside an `EnemyMovement` block.

### `Chase`
Follows the A* path to the player. Completes when within `stopDistance`.
| Property | Default | Description |
|----------|---------|-------------|
| `speed` | 1.0 | Multiplier on NavConfig.maxSpeed. |
| `stopDistance` | 2.0 | XZ distance at which the action completes. |
| `faceTarget` | true | Rotate to face the player while chasing. |

### `Strafe`
Move perpendicular to the player while facing them.
| Property | Default | Description |
|----------|---------|-------------|
| `strafeSign` | 1.0 | +1 = right, -1 = left. |
| `speed` | 0.7 | Speed multiplier. |
| `duration` | 1.5 | Seconds before brain re-evaluates. |

### `Orbit`
Circle the player at `NavConfig.circleRadius`.
| Property | Default | Description |
|----------|---------|-------------|
| `angularSpeed` | 60.0 | Degrees per second. Negative = clockwise. |
| `duration` | 2.0 | Seconds before brain re-evaluates. |

### `Retreat`
Move directly away from the player.
| Property | Default | Description |
|----------|---------|-------------|
| `speed` | 1.0 | Speed multiplier. |
| `desiredDist` | 6.0 | Stop retreating when this far from the player. |
| `maxDuration` | 2.0 | Hard cap on duration. |

### `BackJump`
Physical leap backward (uses `NavMotor.BackJump`).
| Property | Default | Description |
|----------|---------|-------------|
| `settleTime` | 0.1 | Seconds to wait after landing before completing. |

### `Wander`
Pick random nearby points and walk to them.
| Property | Default | Description |
|----------|---------|-------------|
| `duration` | 4.0 | Seconds before brain re-evaluates. |

### `Idle` (wait in place)
Stand still, optionally facing the player.
| Property | Default | Description |
|----------|---------|-------------|
| `duration` | 1.0 | Seconds to wait. |
| `facePlayer` | true | Rotate to face the player while waiting. |

### `Charge`
Direct line rush, no pathfinding.
| Property | Default | Description |
|----------|---------|-------------|
| `speed` | 1.5 | Speed multiplier. |
| `stopDistance` | 1.2 | Stop when within this distance. |
| `maxDuration` | 1.5 | Hard cap. |

### Common EnemyMovement parameters (all strategies)
| Property | Default | Description |
|----------|---------|-------------|
| `stuckTimeout` | 1.2 | Seconds below `speedThreshold` before triggering an unstuck side-step. |
| `speedThreshold` | 0.3 | XZ speed (m/s) below which the enemy is considered stuck. |
| `unstuckDist` | 1.5 | How far to side-step when unsticking. |
| `unstuckDuration` | 0.3 | How long the unstuck step lasts. |
| `maxActionDur` | 6.0 | Hard cap on total action duration. 0 = no cap. |

---

## Context Keys

These are the `InternalId` strings to use in consideration `key = "..."` fields.

### Transform keys (use with `inrange`, `targetexists`)
| InternalId | Description |
|------------|-------------|
| `target.player` | The player character Transform. |
| `target.other_enemy` | Nearest other enemy Transform. |
| `target.self` | This enemy's own Transform. |

### Float keys (use with `curve`, `threshold`)
| InternalId | Range | Description |
|------------|-------|-------------|
| `float.self_health_norm` | [0,1] | currentHealth / maxHealth. |
| `float.dist_to_player_norm` | [0,1] | XZ distance / sensor radius. |
| `float.angle_to_player_norm` | [0,1] | 0 = facing, 1 = behind. |
| `float.time_since_last_action_norm` | [0,1] | Time since last action / actionCooldownWindow. |
| `float.same_action_streak_norm` | [0,1] | Consecutive identical action count / streakCap. |

### Bool keys (use with `bool`)
| InternalId | Description |
|------------|-------------|
| `bool.is_shielded` | Enemy shield is active. |
| `bool.player_is_airborne` | Player is jumping or falling. |
| `bool.is_staggered` | Enemy is currently staggered. |
| `bool.is_aggro` | Enemy is in attacking/aggro state. |
| `bool.player_is_attacking` | Player is currently in an attack. |
| `bool.player_is_dodging` | Player is in a dodge/i-frame window. |

### String/history keys (use with `stringmatch`)
| InternalId | Description |
|------------|-------------|
| `history.last_action_name` | Asset name of the last completed action. |
| `history.second_last_action_name` | Asset name of the action before that. |

---

## Complete Example File

See `BasicEnemySample.aidef` in this folder for a commented example covering
idle, wander, chase, strafe, orbit, back-jump, and melee attack.

---

## Adding New Action Types

1. Create your `ScriptableObject` action class inheriting `EnemyAIActionBase`.
2. Create a class implementing `IActionCodeGen` (see `EnemyActionCodeGens.cs` for examples).
3. Register it in `EnemyAiCodeGeneratorFactory.Create()`.
4. Existing `.aidef` files are not affected — they simply won't use the new keyword.

---

## Animation Decoupling

`EnemyMovementAIAction` has an **Anim Override** field in the Inspector.
- When **null**, strategies use the `EnemyAnimData` assigned on the `EnemyStateMachine`.
- When set, strategies use that override instead.

This means one `EnemyMovementAIAction` asset can be shared across enemy types
with different rigs — just assign a different `EnemyAnimData` per brain, or leave
the override null and each brain's own data handles it.


