using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * All context keys used by the enemy utility AI.
 * Each key is a <see cref="ContextKey{TValue}"/> — a typed, named handle.
 * Users never write raw strings; they reference these static fields directly.
 * </summary>
 * <remarks>
 * The <c>[AIContextKey]</c> attribute makes all fields discoverable by the
 * <c>ContextKeyField</c> inspector drawer, allowing considerations to pick keys
 * from a grouped dropdown.
 * </remarks>
 */
[AIContextKey]
public static class EnemyContextKeys
{
    #region Targets

    /** <summary>The player character Transform.</summary> */
    public static readonly ContextKey<Transform> Player =
        new("target.player", "Player", "Target");

    /** <summary>The nearest other enemy Transform.</summary> */
    public static readonly ContextKey<Transform> OtherEnemy =
        new("target.other_enemy", "Other Enemy", "Target");

    /** <summary>This enemy's own Transform (for self-range checks).</summary> */
    public static readonly ContextKey<Transform> Self =
        new("target.self", "Self", "Target");

    #endregion

    #region Float Values

    /** <summary>Current health / max health [0,1].</summary> */
    public static readonly ContextKey<float> SelfHealthNorm =
        new("float.self_health_norm", "Self Health (norm)", "Float");

    /** <summary>XZ distance to player normalised against sensor detection radius [0,1].</summary> */
    public static readonly ContextKey<float> DistanceToPlayerNorm =
        new("float.dist_to_player_norm", "Distance to Player (norm)", "Float");

    /** <summary>Angle to player relative to forward, normalised [0=facing, 1=behind].</summary> */
    public static readonly ContextKey<float> AngleToPlayerNorm =
        new("float.angle_to_player_norm", "Angle to Player (norm)", "Float");

    /** <summary>
     * Time since the last action completed, normalised against a configurable window.
     * Lets action-cooldown considerations prevent spamming.
     * </summary>
     */
    public static readonly ContextKey<float> TimeSinceLastActionNorm =
        new("float.time_since_last_action_norm", "Time Since Last Action (norm)", "Float");

    /** <summary>How many times the same action has fired consecutively, normalised [0,1] against a cap.</summary> */
    public static readonly ContextKey<float> SameActionStreakNorm =
        new("float.same_action_streak_norm", "Same-Action Streak (norm)", "Float");

    /** <summary>
     * Total number of consecutive attack actions fired (any attack type), normalised [0,1].
     * Increments for every EnemyAttackAIAction committed regardless of attack name.
     * Resets to 0 when any non-attack committed action fires (movement, idle, stun).
     * Use this to trigger BackOff after a burst of close-range melee hits.
     * </summary>
     */
    public static readonly ContextKey<float> ConsecutiveMeleeCountNorm =
        new("float.consecutive_melee_count_norm", "Consecutive Melee Count (norm)", "Float");

    #endregion

    #region Bool Flags

    /** <summary>True when the enemy's shield component is currently active.</summary> */
    public static readonly ContextKey<bool> IsShielded =
        new("bool.is_shielded", "Is Shielded", "Bool");

    /** <summary>True when the player is currently airborne.</summary> */
    public static readonly ContextKey<bool> PlayerIsAirborne =
        new("bool.player_is_airborne", "Player Is Airborne", "Bool");

    /** <summary>True when this enemy is in a staggered state.</summary> */
    public static readonly ContextKey<bool> IsStaggered =
        new("bool.is_staggered", "Is Staggered", "Bool");

    /** <summary>True when this enemy has been marked aggro by the EntityManager.</summary> */
    public static readonly ContextKey<bool> IsAggro =
        new("bool.is_aggro", "Is Aggro", "Bool");

    /** <summary>True when the player is currently in an attack state.</summary> */
    public static readonly ContextKey<bool> PlayerIsAttacking =
        new("bool.player_is_attacking", "Player Is Attacking", "Bool");

    /** <summary>True when the player is currently in a dodge / i-frame window.</summary> */
    public static readonly ContextKey<bool> PlayerIsDodging =
        new("bool.player_is_dodging", "Player Is Dodging", "Bool");

    #endregion

    #region History

    /** <summary>Asset name of the most recently completed action.</summary> */
    public static readonly ContextKey<string> LastActionName =
        new("history.last_action_name", "Last Action", "History");

    /** <summary>Asset name of the action before that.</summary> */
    public static readonly ContextKey<string> SecondLastActionName =
        new("history.second_last_action_name", "Second-Last Action", "History");

    /** <summary>Number of consecutive identical actions (raw int).</summary> */
    public static readonly ContextKey<int> SameActionStreak =
        new("history.same_action_streak", "Same-Action Streak", "History");

    #endregion
}



