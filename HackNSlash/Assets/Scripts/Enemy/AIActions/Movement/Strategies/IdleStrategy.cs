using System;
using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Stand still for a fixed duration — useful as an idle before committing to an attack.
 * </summary>
 */
[Serializable]
public class IdleStrategy : IMovementStrategy
{
    [Tooltip("If true, the brain can interrupt this action to choose a new one. If false, the enemy will be committed to the back jump until it finishes.")]
    public bool isInterruptible;
    public bool IsInterruptible => isInterruptible;
    
    [Tooltip("How long to idle before releasing back to the brain.")]
    [Min(0.1f)] public float duration = 1f;

    [Tooltip("If true, face the player while idling.")]
    public bool facePlayer = true;

    private float _elapsed;
    private Transform _target;

    public string DisplayName => "Idle";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _elapsed = 0f;
        _target  = context.GetTarget(EnemyContextKeys.Player);
        esm.Brake();
        if (animData != null && animData.idleClip != null && animData.idleClip.Clip != null)
            esm.ts.ea.PlayEnemyAnimation(animData.idleClip);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;
        if (!facePlayer) return;
        if (_target == null) _target = context.GetTarget(EnemyContextKeys.Player);
        if (_target != null) esm.TurnToPosition(_target.position);
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm) { }
    public bool IsComplete(EnemyContext context, EnemyStateMachine esm) => _elapsed >= duration;
}