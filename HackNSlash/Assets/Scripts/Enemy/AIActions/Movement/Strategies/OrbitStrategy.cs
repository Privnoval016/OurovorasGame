using System;
using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Orbit around the player at <see cref="Extensions.Pathfinding.NavConfig.circleRadius"/>.
 * </summary>
 */
[Serializable]
public class OrbitStrategy : IMovementStrategy
{
    [Tooltip("If true, the brain can interrupt this action to choose a new one. If false, the enemy will be committed to the back jump until it finishes.")]
    public bool isInterruptible;
    public bool IsInterruptible => isInterruptible;
    
    [Tooltip("Angular speed in degrees per second. Negative = clockwise.")]
    public float angularSpeed = 60f;

    [Tooltip("Duration (seconds) before the brain re-evaluates.")]
    [Min(0.1f)] public float duration = 2f;

    [Tooltip("Orbit radius in metres. When > 0 this overrides NavConfig.circleRadius, " +
             "letting you set a per-action orbit distance without touching the shared config.")]
    [Min(0f)] public float radiusOverride = 0f;

    private float _elapsed;
    private Transform _target;

    public string DisplayName => "Orbit";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _elapsed = 0f;
        _target  = context.GetTarget(EnemyContextKeys.Player);
        esm.ts.ea.RootMotionEnabled(false);
        var clip = animData?.OrbitClip();
        if (clip != null) esm.ts.ea.PlayEnemyAnimation(clip);
        if (_target != null) esm.ts.motor.ResetOrbitAround(_target.position);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;
        if (_target == null) _target = context.GetTarget(EnemyContextKeys.Player);
        if (_target == null) return;

        if (radiusOverride > 0f)
            esm.ts.motor.Orbit(_target.position, angularSpeed, radiusOverride);
        else
            esm.OrbitAround(_target.position, angularSpeed);

        // Drive strafe blend params using the local-space movement direction.
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