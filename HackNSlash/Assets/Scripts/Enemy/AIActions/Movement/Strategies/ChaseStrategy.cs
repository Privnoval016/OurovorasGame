using System;
using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Chase the player along the A* path.
 * Automatically re-evaluates arrival and releases the action once within
 * <see cref="stopDistance"/> of the target.
 * </summary>
 */
[Serializable]
public class ChaseStrategy : IMovementStrategy
{
    [Tooltip("If true, the brain can interrupt this action to choose a new one. If false, the enemy will be committed to the back jump until it finishes.")]
    public bool isInterruptible;
    public bool IsInterruptible => isInterruptible;
    
    [Tooltip("Speed multiplier on NavConfig.maxSpeed while chasing.")]
    [Range(0.1f, 2f)] public float speedMultiplier = 1f;

    [Tooltip("XZ distance at which chasing is considered complete.")]
    [Min(0.1f)] public float stopDistance = 2f;

    [Tooltip("If true, the enemy faces the player while approaching.")]
    public bool faceTarget = true;

    private Transform _target;

    public string DisplayName => "Chase";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _target = context.GetTarget(EnemyContextKeys.Player);
        esm.ts.ea.RootMotionEnabled(false);
        var clip = animData?.WalkClip();
        if (clip != null) esm.ts.ea.PlayEnemyAnimation(clip);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        if (_target == null)
        {
            _target = context.GetTarget(EnemyContextKeys.Player);
            return;
        }

        if (faceTarget) esm.TurnToPosition(_target.position);
        esm.MoveToDestination(_target.position, speedMultiplier);

        // Scale walk animation playback rate to actual movement speed so feet don't slide.
        var animData = esm.enemyAnimData;
        if (animData != null && animData.walkSpeedParam != null)
        {
            Vector3 vel = esm.ts.pe.rb.linearVelocity;
            float speed = new Vector3(vel.x, 0f, vel.z).magnitude;
            float ratio = animData.walkSpeedReference > 0f ? speed / animData.walkSpeedReference : 1f;
            esm.ts.ea.SetAnimancerParam(animData.walkSpeedParam, ratio);
        }
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm)
        => esm.Brake();

    public bool IsComplete(EnemyContext context, EnemyStateMachine esm)
    {
        if (_target == null) return true;
        Vector3 delta = _target.position - esm.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= stopDistance * stopDistance;
    }
}