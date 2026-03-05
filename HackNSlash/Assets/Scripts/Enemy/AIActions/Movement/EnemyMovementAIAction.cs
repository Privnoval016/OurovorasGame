using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * A single reusable movement action for the enemy utility AI.
 * Delegates all per-frame behaviour to an <see cref="IMovementStrategy"/>.
 * Animation data is always sourced from the enemy's own <see cref="EnemyAnimData"/>,
 * keeping this asset model-agnostic and reusable across different enemy prefabs.
 * </summary>
 */
[CreateAssetMenu(fileName = "EnemyMovementAction", menuName = "Enemy/AIActions/EnemyMovementAction")]
public class EnemyMovementAIAction : EnemyAIActionBase
{
    #region Inspector

    [Header("Movement Strategy")]
    [Tooltip("Select which motion type to perform and tweak its parameters.")]
    [SerializeReference] public IMovementStrategy strategy;

    // Movement fill actions (orbit, close-dash, approach) must NOT reset the attack cooldown
    // timer, otherwise Sweep/Lunge can never fire because the timer keeps resetting every time
    // a movement action completes before an attack threshold is reached.
    public override bool ResetsActionTimer => false;

    [Header("Stuck Detection")]
    [Min(0.2f)] public float stuckTimeout = 1.2f;
    [Min(0f)]   public float movingSpeedThreshold = 0.3f;
    [Min(0.1f)] public float unstuckStepDistance = 1.5f;
    [Min(0.05f)]public float unstuckStepDuration = 0.3f;

    [Header("Safety")]
    [Min(0f)] public float maxDuration = 6f;

    /** <summary>Animation data is always pulled from the enemy instance, never from the action asset.</summary> */
    public EnemyAnimData ResolveAnimData(EnemyStateMachine esm) => esm.enemyAnimData;

    #endregion

    #region Runtime State

    private float _elapsed;
    private float _stuckTimer;
    private bool  _unstucking;
    private float _unstuckTimer;
    private Vector3 _unstuckDir;

    #endregion

    #region EnemyAIActionBase

    protected override void OnEnemyEnter(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed     = 0f;
        _stuckTimer  = 0f;
        _unstucking  = false;
        _unstuckTimer = 0f;

        strategy?.OnEnter(context, esm, ResolveAnimData(esm));
    }

    protected override void OnEnemyUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;

        if (maxDuration > 0f && _elapsed >= maxDuration)  { FinishAction(context, esm); return; }
        if (strategy != null && strategy.IsComplete(context, esm)) { FinishAction(context, esm); return; }

        if (_unstucking)
        {
            _unstuckTimer += Time.deltaTime;
            esm.MoveInDirection(_unstuckDir);
            if (_unstuckTimer >= unstuckStepDuration) { _unstucking = false; _stuckTimer = 0f; }
            return;
        }

        TickStuckDetection(esm);
        strategy?.OnUpdate(context, esm);
    }

    protected override void OnEnemyExit(EnemyContext context, EnemyStateMachine esm)
        => strategy?.OnExit(context, esm);

    #endregion

    #region Private Helpers

    private void TickStuckDetection(EnemyStateMachine esm)
    {
        float xzSpeedSqr = new Vector3(esm.ts.pe.rb.linearVelocity.x, 0f, esm.ts.pe.rb.linearVelocity.z).sqrMagnitude;
        if (xzSpeedSqr > movingSpeedThreshold * movingSpeedThreshold) { _stuckTimer = 0f; return; }
        _stuckTimer += Time.deltaTime;
        if (_stuckTimer >= stuckTimeout) BeginUnstuck(esm);
    }

    private void BeginUnstuck(EnemyStateMachine esm)
    {
        _unstucking  = true;
        _unstuckTimer = 0f;

        Vector3 fwd   = esm.transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 left  = new Vector3(-fwd.z, 0f,  fwd.x);
        Vector3 right = new Vector3( fwd.z, 0f, -fwd.x);
        float   dist  = unstuckStepDistance * 2f;

        bool lc = !Physics.Raycast(esm.transform.position + Vector3.up * 0.5f, left,  dist, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        bool rc = !Physics.Raycast(esm.transform.position + Vector3.up * 0.5f, right, dist, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        if      (lc && !rc) _unstuckDir = left;
        else if (rc && !lc) _unstuckDir = right;
        else if (lc)        _unstuckDir = left;
        else                _unstuckDir = -fwd;
    }

    private void FinishAction(EnemyContext context, EnemyStateMachine esm)
    {
        // ResumePrevious pops EnemyActing, which calls EnemyActing.OnExit → _action.OnExit → strategy.OnExit.
        // Do NOT call strategy.OnExit here directly to avoid a double-exit.
        esm.sc.ResumePrevious();
    }

    #endregion
}



