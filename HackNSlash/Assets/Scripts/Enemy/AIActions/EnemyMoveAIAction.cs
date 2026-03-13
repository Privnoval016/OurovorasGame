using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Moves the enemy toward the nearest detected player using A* path planning.
 * Calls <see cref="EnemyStateMachine.MoveToDestination"/> each Update tick.
 * The action is considered complete once the enemy arrives within
 * <see cref="Extensions.Navigation.NavConfig.arrivalRadius"/> or the action is interrupted.
 * </summary>
 */
[CreateAssetMenu(fileName = "EnemyMoveAIAction", menuName = "Enemy/AIActions/EnemyMoveAIAction", order = 0)]
public class EnemyMoveAIAction : EnemyAIActionBase
{
    [Tooltip("Speed multiplier applied to NavMotor.maxSpeed while chasing.")]
    [Range(0.1f, 2f)] public float speedMultiplier = 1f;

    public override bool IsInterruptible { get; } = true;

    private Transform _target;

    protected override void OnEnemyEnter(EnemyContext context, EnemyStateMachine esm)
    {
        _target = context.GetTarget(EnemyContextKeys.Player);
        esm.ts.ea.RootMotionEnabled(false);
        esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.walkCycle);
    }

    protected override void OnEnemyUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        if (_target != null) esm.MoveToDestination(_target.position, speedMultiplier);
    }

    protected override void OnEnemyExit(EnemyContext context, EnemyStateMachine esm)
        => esm.Brake();
}