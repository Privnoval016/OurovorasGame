CALLIOPE — PROLOGUE MINIBOSS

Archetype: Sweeping Striker
Role: First recurring Muse miniboss
Purpose: Teach spacing, facing, dodge timing, and enrage escalation
Complexity: Low
Mobility: Moderate
Phases:

Phase 1 (100–50% HP)

Phase 2 (Enrage, 50–0% HP)

Shield Break State (when shield depleted)

GLOBAL DESIGN RULES

7 total actions.

All non-stunned actions are gated by:

IsShielded = true

IsStaggered = false

Shield break overrides all behavior.

All attack actions are gated by:

TargetExists (Player)

Cooldown via TimeSinceLastActionNorm

Anti-spam via SameActionStreakNorm

Idle fallback always exists (Constant = 0.05).

ACTION LIST

EnemyShieldBreakStunned

EnemyApproachPlayer

EnemyStrafeReposition

EnemySweepAttack

EnemyOverheadStrike

EnemyForwardLunge

EnemyIdleFallback

1. EnemyShieldBreakStunned
Purpose

Full immobilization state when shield is broken.

Behavior

No movement.

No attacks.

Rotation locked.

Large vulnerability window.

Ends when shield restores.

Consideration

Composite
allMustBeNonZero = true

Children:

BoolConsideration

contextKey: IsShielded

invert: true

ConstantConsideration

value: 10

This ensures this action always wins when shield is broken.

2. EnemyApproachPlayer
Purpose

Close distance when player is far.

Behavior

Moves toward player using nav system.

Consideration

Composite
allMustBeNonZero = true

Children:

BoolConsideration

IsShielded (invert = false)

BoolConsideration

IsStaggered (invert = true)

TargetExistsConsideration

targetKey: Player

CurveConsideration

contextKey: DistanceToPlayerNorm

Curve:

(0, 0)

(0.4, 0)

(0.6, 0.6)

(1, 1)

Result:

High utility when player is far.

Zero when already in melee range.

3. EnemyStrafeReposition
Purpose

Maintain motion at mid-range.

Behavior

Moves laterally relative to player.

Consideration

Composite
allMustBeNonZero = true

Children:

BoolConsideration

IsShielded

TargetExistsConsideration

Player

CurveConsideration

DistanceToPlayerNorm

Curve:

(0, 0)

(0.25, 1)

(0.6, 0.2)

(1, 0)

Peaks at ideal melee distance.

ThresholdConsideration

TimeSinceLastActionNorm

GreaterThan 0.3

scoreIfTrue = 1

scoreIfFalse = 0

4. EnemySweepAttack (Primary Attack)
Purpose

Main readable horizontal attack.

Behavior

Wide arc. Moderate forward step. Clear wind-up.

Consideration

Composite
allMustBeNonZero = true

Children:

BoolConsideration

IsShielded

TargetExistsConsideration

Player

InRangeConsideration

targetKey: Player

maxDistance: 3.5

maxAngle: 110

curve: descending (1 at 0, 0 at 1)

ThresholdConsideration

TimeSinceLastActionNorm

GreaterThan 0.5

ThresholdConsideration

SameActionStreakNorm

GreaterThan 0.7

scoreIfTrue = 0

scoreIfFalse = 1

CurveConsideration (Enrage Scaling)

contextKey: SelfHealthNorm

Curve:

(1, 0.8)

(0.5, 1)

(0.2, 1.2)

5. EnemyOverheadStrike
Purpose

Slower vertical punish attack.

Behavior

Back step → pause → vertical slam.

Consideration

Composite
allMustBeNonZero = true

Children:

BoolConsideration

IsShielded

TargetExistsConsideration

Player

InRangeConsideration

maxDistance: 2.8

maxAngle: 80

descending curve

CurveConsideration

contextKey: AngleToPlayerNorm

Curve:

(0, 1)

(0.5, 0.5)

(1, 0)

ThresholdConsideration

TimeSinceLastActionNorm

GreaterThan 0.7

CurveConsideration (Enrage Scaling)

Same SelfHealthNorm curve as Sweep.

6. EnemyForwardLunge
Purpose

Gap-closing punish when player retreats.

Behavior

Wind-up stance → long forward thrust.

Consideration

Composite
allMustBeNonZero = true

Children:

BoolConsideration

IsShielded

TargetExistsConsideration

Player

CurveConsideration

DistanceToPlayerNorm

Curve:

(0, 0)

(0.4, 0)

(0.6, 0.7)

(0.9, 1)

ThresholdConsideration

TimeSinceLastActionNorm

GreaterThan 0.6

ThresholdConsideration

SameActionStreakNorm

GreaterThan 0.7

scoreIfTrue = 0

CurveConsideration (Enrage Scaling)

Same SelfHealthNorm curve.

7. EnemyIdleFallback

ConstantConsideration
value = 0.05

No gates.

Ensures the brain never stalls.

ENRAGE RULES (Below 50% HP)

No new actions.

Effects:

Attack scoring increases via SelfHealthNorm curves.

Animation speed multiplier increased (~1.15x).

Action cooldown window reduced ~20%.

Result:

Same patterns.

Faster cadence.

No added complexity.

SHIELD BREAK BEHAVIOR SUMMARY

When IsShielded = false:

All normal actions return 0.

ShieldBreakStunned returns utility = 10.

Enemy becomes immobile and fully punishable.

Once shield restores, behavior resumes normally.

EXPECTED COMBAT FLOW

Far Distance

Approach wins.

Mid Range

Strafe or Sweep wins.

Close & Facing

Sweep preferred.

Overhead occasionally.

Player Retreats

Lunge becomes preferred.

Below 50% HP

Attacks chain more frequently.

Shield Break

Full immobile punish window.