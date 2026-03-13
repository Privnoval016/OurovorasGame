using System;
using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Move directly away from the player at full speed.
 * </summary>
 */
[Serializable]
public class RetreatStrategy : IMovementStrategy
{
    [Tooltip("If true, the brain can interrupt this action to choose a new one. If false, the enemy will be committed to the back jump until it finishes.")]
    public bool isInterruptible;
    public bool IsInterruptible => isInterruptible;
    
    [Range(0.1f, 2f)] public float speedMultiplier = 1f;

    [Tooltip("Stop retreating when this far from the player.")]
    [Min(0.5f)] public float desiredDistance = 6f;

    [Tooltip("Max seconds to retreat before the brain re-evaluates anyway.")]
    [Min(0.1f)] public float maxDuration = 2f;

    private float _elapsed;
    private Transform _target;

    public string DisplayName => "Retreat";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _elapsed = 0f;
        _target  = context.GetTarget(EnemyContextKeys.Player);
        esm.ts.ea.RootMotionEnabled(false);
        var clip = animData?.RetreatClip();
        if (clip != null) esm.ts.ea.PlayEnemyAnimation(clip);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;
        if (_target == null) _target = context.GetTarget(EnemyContextKeys.Player);
        if (_target == null) return;

        esm.RetreatFrom(_target.position, speedMultiplier);

        // Scale walk animation speed to actual movement speed.
        var animData = esm.enemyAnimData;
        if (animData != null && animData.walkSpeedParam != null)
        {
            Vector3 vel = esm.ts.pe.rb.linearVelocity;
            float speed = new Vector3(vel.x, 0f, vel.z).magnitude;
            float ratio = animData.walkSpeedReference > 0f ? speed / animData.walkSpeedReference : 1f;
            esm.ts.ea.SetAnimancerParam(animData.walkSpeedParam, ratio);
        }
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm) => esm.Brake();

    public bool IsComplete(EnemyContext context, EnemyStateMachine esm)
    {
        if (_elapsed >= maxDuration || _target == null) return true;
        Vector3 delta = _target.position - esm.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude >= desiredDistance * desiredDistance;
    }
}