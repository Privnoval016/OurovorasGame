using System;
using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Strafe perpendicular to the player — the enemy moves sideways while
 * keeping its facing locked on the target.
 * </summary>
 */
[Serializable]
public class StrafeStrategy : IMovementStrategy
{
    [Tooltip("If true, the brain can interrupt this action to choose a new one. If false, the enemy will be committed to the back jump until it finishes.")]
    public bool isInterruptible;
    public bool IsInterruptible => isInterruptible;
    
    [Tooltip("Positive = strafe right, negative = strafe left.")]
    [Range(-1f, 1f)] public float strafeSign = 1f;

    [Tooltip("Speed multiplier relative to NavConfig.maxSpeed.")]
    [Range(0.1f, 2f)] public float speedMultiplier = 0.7f;

    [Tooltip("Duration (seconds) before the action completes and the brain re-evaluates.")]
    [Min(0.1f)] public float duration = 1.5f;

    private float _elapsed;
    private Transform _target;

    public string DisplayName => $"Strafe {(strafeSign >= 0 ? "Right" : "Left")}";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _elapsed = 0f;
        _target  = context.GetTarget(EnemyContextKeys.Player);
        esm.ts.ea.RootMotionEnabled(false);
        var clip = animData?.StrafeClip();
        if (clip != null) esm.ts.ea.PlayEnemyAnimation(clip);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;
        if (_target == null) _target = context.GetTarget(EnemyContextKeys.Player);
        if (_target == null) return;

        esm.StrafeAround(_target.position, strafeSign, speedMultiplier);

        // Drive the 2D blend tree parameters with the local-space strafe direction.
        var animData = esm.enemyAnimData;
        if (animData != null && animData.strafeMoveXParam != null && animData.strafeMoveZParam != null)
        {
            Vector3 worldMove = esm.ts.motor.CurrentMoveDirection;
            Vector3 localMove = esm.transform.InverseTransformDirection(worldMove);
            esm.ts.ea.SetAnimancerParam(animData.strafeMoveXParam, localMove.x);
            esm.ts.ea.SetAnimancerParam(animData.strafeMoveZParam, localMove.z);
        }
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm) => esm.Brake();
    public bool IsComplete(EnemyContext context, EnemyStateMachine esm) => _elapsed >= duration;
}