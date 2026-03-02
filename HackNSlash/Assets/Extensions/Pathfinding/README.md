# Extensions.Navigation

A generic, Rigidbody-based navigation system for hack-and-slash enemies (and any other physical agent). Built on top of the [Aron Granberg A\* Pathfinding Project](https://arongranberg.com/astar/).

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Components](#components)
   - [NavConfig](#navconfig)
   - [NavPathPlanner](#navpathplanner)
   - [NavMotor](#navmotor)
3. [Motion Primitives](#motion-primitives)
4. [Gizmos & Visual Debugging](#gizmos--visual-debugging)
5. [Editor Setup](#editor-setup)
6. [Integration with EnemyStateMachine](#integration-with-enemystatemachine)
7. [Adding New Behaviours](#adding-new-behaviours)

---

## Architecture Overview

The system is split into **three responsibilities** kept in separate components, each independently testable and reusable in any project:

```
NavConfig          – ScriptableObject tuning data (speeds, radii, intervals).
NavPathPlanner     – Owns the A* Seeker. Plans paths, tracks waypoints, reports arrival.
NavMotor           – Owns the Rigidbody. Executes all physical locomotion primitives.
```

`NavPathPlanner` and `NavMotor` are intentionally decoupled:

- The planner **never moves anything** — it only exposes `GetSteeringDirection()`.
- The motor **never knows about pathfinding** — it only knows "move in this direction".
- The AI (UtilityAI actions, state machines) sits above both and decides *what* to do each frame.

This means you can use the motor without pathfinding (e.g. for charges or back-jumps), and swap the pathfinding backend without touching the motor.

---

## Components

### NavConfig

`ScriptableObject` — create via `Assets > Create > Navigation > NavConfig`.

| Field | Description |
|---|---|
| `repathRate` | Seconds between A\* path re-requests while navigating. |
| `waypointAcceptanceRadius` | XZ distance to an intermediate waypoint before skipping to the next. |
| `arrivalRadius` | XZ distance to the final destination to consider navigation complete. |
| `maxSpeed` | Maximum XZ speed in m/s the motor will target. |
| `accelerationForce` | `rb.AddForce` multiplier when accelerating toward target speed. |
| `decelerationForce` | `rb.AddForce` multiplier when braking. |
| `rotationSpeed` | Degrees per second for smooth rotation via `RotateTowards`. |
| `strafeSpeedMultiplier` | Fraction of `maxSpeed` used during strafing (0–1). |
| `circleRadius` | Preferred orbit distance when using `Orbit()`. |
| `circleRadiusSpring` | How aggressively the agent corrects back to orbit radius. |
| `backJumpHorizontalSpeed` | Horizontal velocity impulse on a back-jump. |
| `backJumpVerticalSpeed` | Vertical velocity impulse on a back-jump. |
| `backJumpLockDuration` | Seconds of horizontal motion lock after launching. |
| `wanderIntervalMin/Max` | Random interval between new wander destinations. |
| `wanderRadius` | Radius around the wander origin to pick random destinations. |

One `NavConfig` asset is shared by both `NavPathPlanner.config` and `NavMotor.config` on the same enemy.

---

### NavPathPlanner

`RequireComponent(Seeker)` — the A\* `Seeker` must also be present on the same GameObject.

**Public API:**

```csharp
void  SetDestination(Vector3 destination)   // Start navigating. Issues an immediate path request.
void  Stop()                                // Cancel navigation and release the path.
Vector3 GetSteeringDirection()              // Normalised XZ direction to next waypoint. Zero if arrived/no path.
float DistanceToDestination()              // Straight-line XZ distance to the current destination.

bool  ReachedDestination   { get; }        // True once within arrivalRadius.
bool  PathFailed           { get; }        // True if the last path request errored.
bool  IsCalculatingPath    { get; }        // True while a path request is in flight.
Vector3 Destination        { get; }        // Last destination set.
Path  CurrentPath          { get; }        // The active A* Path object (null until first success).

event Action OnPathUpdated;               // Fired when a new valid path is received.
event Action OnArrived;                   // Fired when the agent arrives at the destination.
```

**How path following works:**

1. `SetDestination` records the target and sets `_repathTimer` to `RepathRate` so the first request fires on the next `Update`.
2. Each `Update`, if the timer has elapsed and the `Seeker` is idle, a new `ABPath` is requested.
3. `OnPathComplete` claims the returned path and resets the waypoint index to 0.
4. `GetSteeringDirection` calls `AdvanceWaypoints` each frame — it skips waypoints within `waypointAcceptanceRadius` and returns the direction to the current one.
5. When the agent is within `arrivalRadius` of the **last** waypoint, `ReachedDestination` is set and `OnArrived` is fired.

Path objects are reference-counted (`Claim`/`Release`) to prevent premature pooling by A\*.

---

### NavMotor

`RequireComponent(Rigidbody)` — the Rigidbody must be non-kinematic and managed externally (gravity, knockback, etc. applied elsewhere as in `PhysicsEnemy`).

**Core movement:**

```csharp
void Move(Vector3 direction, float speedMultiplier = 1f, bool faceDirection = true)
// Apply acceleration force toward direction * maxSpeed * multiplier. Safe to call every frame.

void Brake()
// Smooth deceleration to zero using decelerationForce.

void Stop()
// Immediate zero of horizontal velocity.

void RotateToward(Vector3 direction)
// Smooth rotation toward direction using rotationSpeed.

void FacePosition(Vector3 position)
// Convenience: RotateToward the direction to a world position.
```

**Motion primitives:**

See [Motion Primitives](#motion-primitives) below.

**Utilities:**

```csharp
void  LockMotion(float duration)    // Prevent horizontal movement for N seconds (wind-ups, hits).
void  UnlockMotion()                // Cancel a motion lock early.
bool  IsEffectivelyStationary(float threshold = 0.1f)
float CurrentSpeed()
```

---

## Motion Primitives

All primitives are safe to call every frame from an AI update loop.

### `Move(direction, speedMultiplier, faceDirection)`
Core force-based movement. Adds `(targetVelocity - currentVelocity) * accelForce` to the Rigidbody each frame — the same formulation as `PlayerMoving` for consistent feel.

### `Strafe(pivot, sign, speedMultiplier)`
Moves perpendicular to the direction toward `pivot` (positive sign = right strafe, negative = left). Always faces `pivot`. Speed scaled by `strafeSpeedMultiplier * speedMultiplier`.

### `Orbit(center, angularSpeed)`
Advances an internal `OrbitAngle` by `angularSpeed * dt` each frame. Computes the desired position on the orbit ring at `circleRadius` from `center` and spring-steers toward it. Always faces `center`. Call `ResetOrbitAround(center)` before starting to avoid a snap.

### `BackJump(threatPosition)`
One-shot: zeroes horizontal velocity, applies `(awayDir * backJumpHorizontalSpeed, backJumpVerticalSpeed)` as a `VelocityChange` impulse, locks horizontal input for `backJumpLockDuration`. Safe to call every frame — internally guards against re-triggering.

### `RetreatFrom(threatPosition, speedMultiplier)`
Moves directly away from a threat position at full speed. No pathfinding — good for immediate evasion in open areas.

### `ChargeToward(targetPosition, speedMultiplier)`
Moves directly toward a position at full speed, ignoring the navmesh. Stops within `arrivalRadius`. Use for short-range dashes where pathfinding latency would be noticeable.

### `TickWander()`
Call every frame. Internally picks a random point within `wanderRadius` of the spawn origin when the interval expires or on first call. Returns the normalised direction to that point. Returns `Vector3.zero` when idle between destinations. Feed the result into `Move()` or into `EnemyStateMachine.MoveInDirection()`.

---

## Gizmos & Visual Debugging

**NavPathPlanner** (shown always in play mode, selected in editor):
- **Cyan lines** — current A\* path segments.
- **Yellow sphere** — current active waypoint.
- **White/green sphere** — destination marker (green when arrived).

**NavMotor** (shown always):
- **Green arrow** — current move direction.
- **Orange circle + line** — active wander target and radius.
- **Blue circle** — configured orbit radius (centred on the agent — offset when orbiting).

---

## Editor Setup

For each enemy that should use this system:

1. Add `NavPathPlanner` to the enemy root GameObject. A `Seeker` component will be required automatically — add one if Unity prompts you.
2. Add `NavMotor` to the same GameObject.
3. Create a `NavConfig` asset (`Assets > Create > Navigation > NavConfig`) and assign it to both `NavPathPlanner.config` and `NavMotor.config`.
4. Configure the A\* graph in the scene (AstarPath component) as usual — RecastGraph or GridGraph, baked against your level geometry.
5. Set `NavPathPlanner.config.repathRate` — 0.2–0.5 s is typical.
6. Set `NavConfig.maxSpeed` to match your enemy's intended movement speed.
7. Set `NavConfig.arrivalRadius` to roughly the enemy's capsule radius.
8. `NavMotor.config.accelerationForce` controls how quickly the enemy reaches max speed. Values of 30–60 work well for snappy feel.

The `EnemyController` auto-fetches both components in `Awake` — no manual assignment needed beyond the config.

---

## Integration with EnemyStateMachine

`EnemyStateMachine` wraps all navigation primitives so AI actions don't need to touch the components directly:

```csharp
esm.MoveToDestination(target.position, esm.MoveSpeed)  // Path-planned movement to a position.
esm.MoveInDirection(direction, speedMultiplier)         // Raw direction movement (no pathfinding).
esm.StrafeAround(pivot, sign, speedMultiplier)          // Strafe around a pivot.
esm.OrbitAround(center, angularSpeed)                   // Orbit at configured radius.
esm.BackJump(threatPosition)                            // One-shot back-jump impulse.
esm.RetreatFrom(threatPosition, speedMultiplier)        // Direct retreat.
esm.ChargeToward(targetPosition, speedMultiplier)       // Direct charge ignoring navmesh.
esm.Wander()                                            // Random wander near spawn.
esm.Brake()                                             // Smooth stop.
esm.StopMovement()                                      // Hard stop.
esm.TurnToLook()                                        // Face current moveDirection.
esm.TurnToPosition(position)                            // Face a world position.
```

AI actions (`EnemyAIActionBase` subclasses) should call these methods rather than touching `ts.nav` or `ts.motor` directly, keeping AI logic decoupled from navigation implementation.

---

## Adding New Behaviours

To add a new motion type (e.g. "dash to flank position"):

1. Add a method to `NavMotor` that applies the appropriate forces/impulses.
2. Add a thin wrapper in `EnemyStateMachine` that guards with `CanMove` and delegates to the motor.
3. Create an `EnemyAIActionBase` subclass that calls the wrapper in `OnEnemyUpdate`.
4. Expose any tuning parameters as fields in `NavConfig` (or in the action's ScriptableObject if specific to that action).

The planner and motor have no knowledge of each other — either can be replaced or extended without touching the other.

