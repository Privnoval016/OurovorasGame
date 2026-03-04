using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * A single reusable movement action for the enemy utility AI.
 * Rather than creating a separate <see cref="ScriptableObject"/> for every motion type,
 * this action delegates all per-frame behaviour to an <see cref="IMovementStrategy"/>
 * selected via the inspector. All motion parameters live on the chosen strategy.
 *
 * <para>Features:</para>
 * <list type="bullet">
 *   <item>One asset covers all locomotion types (chase, strafe, orbit, retreat, etc.).</item>
 *   <item>Stuck detection: if the enemy barely moves for <see cref="stuckTimeout"/> seconds
 *         it performs a short side-step to clear the obstacle, then resumes.</item>
 *   <item>Strategy completion: the action releases to the brain when
 *         <see cref="IMovementStrategy.IsComplete"/> returns true.</item>
 *   <item>Max duration safety valve prevents the enemy from looping in one motion forever.</item>
 * </list>
 * </summary>
 */
[CreateAssetMenu(fileName = "EnemyMovementAction", menuName = "Enemy/AIActions/EnemyMovementAction")]
public class EnemyMovementAIAction : EnemyAIActionBase
{
    #region Inspector

    [Header("Movement Strategy")]
    [Tooltip("Select which motion type to perform and tweak its parameters.")]
    [SerializeReference] public IMovementStrategy strategy;

    [Header("Animation Override")]
    [Tooltip("Optional: override the EnemyAnimData that this action uses for locomotion animations. " +
             "When null, the brain's own EnemyAnimData is used. " +
             "Assign a different asset here to share one action asset across different enemy models.")]
    public EnemyAnimData animOverride;

    [Header("Stuck Detection")]
    [Tooltip("Seconds of not moving before the enemy is considered stuck.")]
    [Min(0.2f)] public float stuckTimeout = 1.2f;

    [Tooltip("Minimum XZ speed (m/s) to be considered 'moving'. Below this and the stuck timer runs.")]
    [Min(0f)] public float movingSpeedThreshold = 0.3f;

    [Tooltip("How far to back up when unsticking (metres).")]
    [Min(0.1f)] public float unstuckStepDistance = 1.5f;

    [Tooltip("How long the unstuck nudge lasts (seconds).")]
    [Min(0.05f)] public float unstuckStepDuration = 0.3f;

    [Header("Safety")]
    [Tooltip("Hard cap on how long this action can run regardless of strategy completion. " +
             "0 = no cap.")]
    [Min(0f)] public float maxDuration = 6f;

    /** <summary>
     * Returns the anim data this action should use — the per-action override if set,
     * otherwise the brain's own EnemyAnimData. Strategies should call this rather than
     * accessing <c>esm.enemyAnimData</c> directly so the action is portable across models.
     * </summary>
     */
    public EnemyAnimData ResolveAnimData(EnemyStateMachine esm)
        => animOverride != null ? animOverride : esm.enemyAnimData;

    #endregion

    #region Runtime State

    // Fields are reset in OnEnter so the ScriptableObject clone is clean each execution.
    private float _elapsed;
    private float _stuckTimer;
    private bool _unstucking;
    private float _unstuckTimer;
    private Vector3 _unstuckDir;

    #endregion

    #region EnemyAIActionBase

    protected override void OnEnemyEnter(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed = 0f;
        _stuckTimer = 0f;
        _unstucking = false;
        _unstuckTimer = 0f;

        strategy?.OnEnter(context, esm, ResolveAnimData(esm));
    }

    protected override void OnEnemyUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;

        // Safety valve — release back to brain.
        if (maxDuration > 0f && _elapsed >= maxDuration)
        {
            FinishAction(context, esm);
            return;
        }

        // Strategy already finished.
        if (strategy != null && strategy.IsComplete(context, esm))
        {
            FinishAction(context, esm);
            return;
        }

        // --- Stuck handling ---
        if (_unstucking)
        {
            _unstuckTimer += Time.deltaTime;
            esm.MoveInDirection(_unstuckDir);
            if (_unstuckTimer >= unstuckStepDuration)
            {
                _unstucking = false;
                _stuckTimer = 0f;
            }
            return;
        }

        TickStuckDetection(esm);

        // Normal strategy tick.
        strategy?.OnUpdate(context, esm);
    }

    protected override void OnEnemyExit(EnemyContext context, EnemyStateMachine esm)
    {
        strategy?.OnExit(context, esm);
    }

    #endregion

    #region Private Helpers

    private void TickStuckDetection(EnemyStateMachine esm)
    {
        float xzSpeedSqr = new Vector3(
            esm.ts.pe.rb.linearVelocity.x, 0f,
            esm.ts.pe.rb.linearVelocity.z).sqrMagnitude;

        bool moving = xzSpeedSqr > movingSpeedThreshold * movingSpeedThreshold;

        if (moving)
        {
            _stuckTimer = 0f;
            return;
        }

        _stuckTimer += Time.deltaTime;

        if (_stuckTimer >= stuckTimeout)
            BeginUnstuck(esm);
    }

    /** <summary>
     * Kicks off a short side-step away from the nearest obstacle or toward open space.
     * We pick the direction perpendicular to the current facing that has more free space,
     * falling back to a direct retreat from the last known player position.
     * </summary>
     */
    private void BeginUnstuck(EnemyStateMachine esm)
    {
        _unstucking = true;
        _unstuckTimer = 0f;

        // Try perpendicular directions first — pick the one with more clear space.
        Vector3 fwd = esm.transform.forward;
        fwd.y = 0f;
        fwd.Normalize();

        Vector3 left = new Vector3(-fwd.z, 0f, fwd.x);
        Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);
        float checkDist = unstuckStepDistance * 2f;

        bool leftClear = !Physics.Raycast(
            esm.transform.position + Vector3.up * 0.5f, left, checkDist,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);
        bool rightClear = !Physics.Raycast(
            esm.transform.position + Vector3.up * 0.5f, right, checkDist,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);

        if (leftClear && !rightClear) _unstuckDir = left;
        else if (rightClear && !leftClear) _unstuckDir = right;
        else if (leftClear) _unstuckDir = left;
        else _unstuckDir = -fwd; // both blocked — step backward
    }

    private void FinishAction(EnemyContext context, EnemyStateMachine esm)
    {
        strategy?.OnExit(context, esm);
        esm.SetIsAttacking(false);
    }

    #endregion
}




