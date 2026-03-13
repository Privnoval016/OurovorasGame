using System;
using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Charge straight at the player without following the A* path — for short-range
 * dashes and quick aggressive rushes.
 * The direction toward the player is snapshotted at entry so the charge never
 * reverses if the enemy overshoots past the target.
 * </summary>
 */
[Serializable]
public class ChargeStrategy : IMovementStrategy
{
    [Tooltip("If true, the brain can interrupt this action to choose a new one. If false, the enemy will be committed to the back jump until it finishes.")]
    public bool isInterruptible;
    public bool IsInterruptible => isInterruptible;
    
    [Range(0.1f, 3f)] public float speedMultiplier = 1.5f;

    [Tooltip("Stop when within this distance.")]
    [Min(0.1f)] public float stopDistance = 1.2f;

    [Tooltip("Max duration before the brain re-evaluates.")]
    [Min(0.1f)] public float maxDuration = 1.5f;

    private Transform _target;
    private float _elapsed;
    // Direction is locked at entry so the charge never reverses if the enemy overshoots.
    private Vector3 _lockedDir;

    public string DisplayName => "Charge";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _target  = context.GetTarget(EnemyContextKeys.Player);
        _elapsed = 0f;
        _lockedDir = Vector3.zero;

        // Snapshot direction immediately if the target is already known.
        if (_target != null)
        {
            Vector3 toTarget = _target.position - esm.transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
                _lockedDir = toTarget.normalized;
        }

        esm.ts.ea.RootMotionEnabled(false);
        var clip = animData?.WalkClip();
        if (clip != null) esm.ts.ea.PlayEnemyAnimation(clip);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;

        if (_target == null) _target = context.GetTarget(EnemyContextKeys.Player);
        if (_target == null) return;

        // Lock direction on first valid frame if it wasn't set in OnEnter.
        if (_lockedDir == Vector3.zero)
        {
            Vector3 toTarget = _target.position - esm.transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
                _lockedDir = toTarget.normalized;
        }

        if (_lockedDir == Vector3.zero) return;

        // Check arrival before applying more force — if already within stopDistance, stop immediately.
        Vector3 delta = _target.position - esm.transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude <= stopDistance * stopDistance)
        {
            esm.Brake();
            return;
        }

        esm.MoveInDirection(_lockedDir, speedMultiplier);
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm) => esm.Brake();

    public bool IsComplete(EnemyContext context, EnemyStateMachine esm)
    {
        if (_elapsed >= maxDuration || _target == null) return true;
        Vector3 delta = _target.position - esm.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= stopDistance * stopDistance;
    }
}