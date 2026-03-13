using System;
using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Leap backward away from the player.
 * The action completes as soon as the back-jump motor flag clears.
 * </summary>
 */
[Serializable]
public class BackJumpStrategy : IMovementStrategy
{
    [Tooltip("If true, the brain can interrupt this action to choose a new one. If false, the enemy will be committed to the back jump until it finishes.")]
    public bool isInterruptible;
    public bool IsInterruptible => isInterruptible;
    
    [Tooltip("Seconds to wait after landing before releasing the action.")]
    [Min(0f)] public float settleTime = 0.1f;

    private Transform _target;
    private float _settleTimer;
    private bool _jumpFired;

    public string DisplayName => "Back Jump";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _target    = context.GetTarget(EnemyContextKeys.Player);
        _jumpFired = false;
        _settleTimer = 0f;
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        if (!_jumpFired)
        {
            Vector3 threat = _target != null ? _target.position : esm.transform.position - esm.transform.forward;
            esm.BackJump(threat);
            _jumpFired = true;
        }
        if (_jumpFired && !esm.ts.motor.IsBackJumping) _settleTimer += Time.deltaTime;
    }

    public void OnExit(EnemyContext context, EnemyStateMachine esm) { }
    public bool IsComplete(EnemyContext context, EnemyStateMachine esm)
        => _jumpFired && !esm.ts.motor.IsBackJumping && _settleTimer >= settleTime;
}