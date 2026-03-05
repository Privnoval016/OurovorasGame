# Calliope — Prologue Miniboss Design Document

## Overview

| Property | Value |
|---|---|
| **Archetype** | Sweeping Striker |
| **Role** | First recurring Muse miniboss |
| **Phases** | 2 (Phase 1: 100–50% HP / Phase 2 Enrage: 50–0% HP) |
| **Special State** | Shield Break (triggered whenever the shield is depleted) |
| **Teaching Purpose** | Spacing awareness, orbit tension, attack telegraph reading, dodge timing |
| **Difficulty** | Low — introduction to boss patterns |

---

## Design Intent

Calliope's fight is structured around a recognisable rhythm: circle → commit → attack → reset. The player can read when she is about to attack because the orbit phase is visually distinct from the dash-in, and both are distinct from the melee swing animations.

She teaches three things:
1. **Spacing** — she behaves differently at melee range versus orbit range.
2. **Wind-up reading** — the lunge has a clear animation before the dash. Sweep and Overhead have distinct telegraphs. Players are expected to fail, observe, and react.
3. **Shield mechanics** — breaking her shield produces an explicit frozen punish window.

She is intentionally **never idle**. Between attacks she is always either orbiting, closing distance, or swinging. The only valid "pause" is a brief CloseDash that fills the cooldown gap between actions.

---

## Timing Reference

All cooldown thresholds map to `_timeSinceLastAction / actionCooldownWindow`. Set `actionCooldownWindow = 1.0` on `EnemyStateMachine`.

| Threshold | Real cooldown |
|---|---|
| 0.35 | 0.35s |
| 0.40 | 0.40s |
| 0.45 | 0.45s |
| 0.70 | 0.70s |

Anti-spam: `SameActionStreakNorm` with `streakCap = 4`. Streak 2 = 0.50. All attack actions block at threshold > 0.40, so any attack repeated twice back-to-back is suppressed.

`EnemySensor.detectionRadius = 10`, so `norm 1.0 = 10m`.

| norm | metres |
|---|---|
| 0.20 | 2m — tight melee |
| 0.40 | 4m — attack range boundary |
| 0.60 | 6m — Calliope's orbit radius |
| 0.75 | 7.5m — outer approach zone |

---

## How the AI Makes Decisions

Calliope uses a **Utility AI** system. Every ~0.1s the brain evaluates all 9 actions and selects the highest scorer. Scores are computed by multiplying considerations together.

`allMustBeNonZero = true` means if any single consideration returns 0, the action scores 0 and cannot be selected — effectively an AND gate. This is how cooldown thresholds and range gates work.

### Idle Deadlock Fix

After any action completes, `_timeSinceLastAction` resets to 0 → `TimeSinceLastActionNorm = 0`. All cooldown-gated actions score 0 immediately. Without a fill action, only Idle (0.01) could win.

Two actions have **no cooldown gate** to prevent this:
- **OrbitFar** — scores 0.45 in the 0.50–0.75 norm band. If she's at orbit range after an action, she circles while the lunge timer counts up.
- **CloseDash** — scores 0.30 flat at ≤ 0.25 norm. If she's at melee range after an action, she nudges in and the brain re-evaluates into Sweep/Overhead on the next tick.

---

## Action-by-Action Breakdown

### 1. CalliopeShieldBreakStunned

**Score: 10** — unconditionally wins over everything while `IsShielded = false`.

Calliope freezes. The stun exits when the shield regenerates above 0. Players have a free punish window.

---

### 2. CalliopeApproachPlayer

**What it does:** Walks toward the player at `speed=1.2`, stopping at 1.0m.

**When it fires:** Only when player is genuinely far — the curve is flat 0 below 0.60 norm (6m). This means if Calliope is already at orbit range (6m), OrbitFar (0.45) wins over Approach (0.0). Approach only kicks in when the player has backed away beyond 6m.

---

### 3. CalliopeOrbitFar

**What it does:** Circles the player at `radiusOverride = 6.0m` (overrides `NavConfig.circleRadius`), facing the player at all times. Duration 1.2s.

**Why 6m:** Default `NavConfig.circleRadius = 4m` places orbit exactly at Sweep's attack boundary. Any overshoot leaves her out of range and attacks never fire. 6m is clearly outside the 4m zone — the orbit is visually readable as a tension phase, not an attack hover.

**When it fires:** Scores 0.45 flat in the 0.50–0.75 norm band. No cooldown gate, so it fills the gap immediately after any action if she is at orbit range.

**The handoff to Lunge:** OrbitFar scores 0.45. Lunge scores 0 when `TimeSinceLastActionNorm < 0.45`. After 0.45s of orbiting, Lunge's score rises to 0.72 at 0.62 norm — beating OrbitFar — and she commits inward.

---

### 4. CalliopeCloseDash

**What it does:** A short Charge at `speed=1.6`, capped at 0.4s, stopping at 1.5m. Physically nudges her into the player.

**When it fires:** No cooldown gate. Scores 0.30 flat at ≤ 0.25 norm (2.5m). After any melee attack finishes and the timer resets to 0, CloseDash wins over OrbitFar (0.00 at close range) and Idle (0.01). She slides in tighter and the brain re-evaluates into Sweep or Overhead once the 0.35s/0.40s cooldown elapses.

---

### 5. CalliopeForwardLunge

**What it does:** A true attack — plays the lunge animation (readable telegraph), then at `animDelay` sets `rb.linearVelocity = dashDir * 10` directly for 0.3s. Hitboxes activate via `EnemyAnimListener` as normal. Uses `DashLungeEnemyAttackStrategy`.

**Why velocity not force:** `AddForce(VelocityChange)` in a per-frame loop compounds every frame. Direct velocity assignment produces a clean, constant-speed dash.

**When it fires:** Peaks at 0.55 norm (5.5m) — precisely the orbit hand-off distance. After 0.45s of orbiting at 6m, lunge scores 0.72 and beats OrbitFar's 0.45. Cooldown 0.45s, blocked after back-to-back.

**Inspector setup:**
- Assign the lunge `EnemyAttack` asset on `CalliopeForwardLunge`
- Set `enemyAttack = DashLungeEnemyAttackStrategy`
- `dashSpeed = 10`, `dashDuration = 0.3`, `lockDirectionOnDash = true`
- Set `animDelay` on the `EnemyAttack` to the lunge clip's commit frame

---

### 6. CalliopeStrafeReposition

**What it does:** Lateral sidestep for 0.8s at mid-range. Fires at most once per 3–4 attack cycles (cooldown 0.7s, anti-spam streak gate).

**Purpose:** Prevents her from being a fully predictable forward-moving target. Adds visual variety to the fight without dominating the action budget.

---

### 7. CalliopeSweepAttack

**What it does:** Wide horizontal arc. 4.0m range, 120° arc. Most frequent attack.

**When it fires:** Cooldown 0.35s — the shortest cooldown in the kit. After Lunge arrives and CloseDash fills the gap (~0.35s), Sweep fires. Blocked after 2 consecutive uses, giving Overhead a turn.

**Enrage:** 0.8× at full HP → 1.2× near death via `SelfHealthNorm` curve.

---

### 8. CalliopeOverheadStrike

**What it does:** Deliberate vertical slam. 4.0m range, 150° arc. Cooldown 0.40s.

**Previous problem:** Old version required `maxangle = 75°` AND `AngleToPlayerNorm` curve — silently blocked unless Calliope was perfectly centered on the player. Both removed.

**When it fires:** Whenever Sweep is streak-blocked (after 2 uses) and the player is within 4m. The cycle is: Sweep × 2 → Sweep blocked → Overhead × 2 → Overhead blocked → Sweep → ...

---

### 9. CalliopeIdleFallback

**Score: 0.01** — lowest possible. Only wins if the sensor has no target and every other action scores 0.

---

## Expected Combat Flow

### Player at 8m+ (far)
Approach scores ~0.90, walks her in. OrbitFar scores 0 (too far). Once she reaches ~7m, OrbitFar begins scoring.

### Player at 6–7m (orbit zone)
OrbitFar (0.45) beats Approach (0.0–0.10). She circles. After 0.45s, Lunge (0.72) beats OrbitFar (0.45). She lunges in. Lunge arrives → melee range.

### Player at 0–4m (melee)
Sweep (1.0 × enrage × health) wins whenever the 0.35s cooldown has elapsed. After 2 Sweeps, Overhead wins. CloseDash fills gaps between attacks. Lunge scores low (curve near 0 at close range) so melee attacks dominate.

### Shield Break
All normal actions gate on `IsShielded = true` → score 0. Stun scores 10. Full punish window until shield regenerates.

### Enrage (below 50% HP)
Same action set, attack score multiplier rises to 1.2× from 1.0×. Attacks chain slightly more aggressively.

---

## Parameters to Set in the Inspector

**EnemyStateMachine:**
- `actionCooldownWindow = 1.0`
- `streakCap = 4`

**EnemySensor:**
- `detectionRadius = 10`

**Generated ScriptableObjects (parser does not set attack/animation data):**

| Action | Fields to Set |
|---|---|
| `CalliopeForwardLunge` | `attack` asset → set `enemyAttack = DashLungeEnemyAttackStrategy`, `dashSpeed = 10`, `dashDuration = 0.3`, `lockDirectionOnDash = true`. Set `animDelay` to match lunge commit frame. |
| `CalliopeSweepAttack` | `attack` asset with sweep animation clips and hitbox data |
| `CalliopeOverheadStrike` | `attack` asset with overhead animation clips and hitbox data |
| `CalliopeShieldBreakStunned` | Optional stun animation clip |
| All movement actions | `EnemyAnimData` pulled from `EnemyStateMachine.enemyAnimData` |

---

## Code Changes Made

| File | Change |
|---|---|
| `NavMotor.cs` | Added `Orbit(Vector3, float, float)` overload with explicit radius. `Move()` calls `ProjectOnGround()` to project the steering direction onto the ground surface normal — rigidbody rides over small bumps. Slope probe fields are now serialized inspector fields (`slopeProbeOriginOffset`, `slopeProbeDistance`, `slopeProbeMaxAngle`). |
| `MovementStrategies.cs` | Added `radiusOverride` to `OrbitStrategy`. When > 0 calls the new `NavMotor.Orbit` overload instead of the config-driven one. |
| `EnemyMovementCodeGen.cs` | Parser reads `radius` property into `OrbitStrategy.radiusOverride`. Serialiser emits `radius` when non-zero. |
| `EnemyAIActionBase.cs` | Added virtual `ResetsActionTimer` property (default `true`). Movement fill actions override it `false` to prevent resetting the attack cooldown timer or streak on completion. |
| `EnemyMovementAIAction.cs` | Overrides `ResetsActionTimer = false`. `FinishAction` calls `esm.sc.ResumePrevious()` to properly pop `EnemyActing`. |
| `EnemyStateMachine.ExecuteNewAction` | Guards `if (sc.IsState<EnemyActing>()) return`. Uses `enemyAction.ResetsActionTimer` (virtual — no concrete type check) to decide timer/streak reset. |
| `EnemyStateMachine.SetIsAttacking` | Only records `_lastActionName` / `_sameActionStreak` for committed actions (`ResetsActionTimer = true`). Fixed bug where CloseDash finishing overwrote `_lastActionName`, corrupting Sweep's anti-spam streak gate. |
| `IEnemyAttackStrategy.LaunchClipAttacks` | Always reaches `ResumeMoving` regardless of whether `attackClips` is populated, fixing permanent `IsAttacking = true` lockout for attacks with no clips. |
| `DashLungeEnemyAttackStrategy.cs` | Sets `rb.linearVelocity` directly for constant-speed dash. Locks motor during dash, faces player at commit frame, brakes on completion. |
| `PlayerMoving.Run` | Projects the player's flat move direction onto the ground surface normal (same pattern as `NavMotor`) so the player also rides over small terrain bumps. Only applied when grounded. |
| `PlayerStateMachine` | Added serialized slope probe fields under the "Checks" header: `slopeProbeOriginOffset`, `slopeProbeDistance`, `slopeProbeMaxAngle`. |
| `Calliope.aidef` | Removed `CalliopeApproachPlayer` — never fires in practice since the player is rarely beyond 7m. 8 actions remain. |
