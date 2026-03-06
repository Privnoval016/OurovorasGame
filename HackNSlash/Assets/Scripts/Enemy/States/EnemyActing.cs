using Extensions.UtilityAI;
using Unity.Entities;

public class EnemyActing : EnemyState
{
    private readonly EnemyAIActionBase _action;
    private readonly EnemyContext _context;
    // Set by the action when it is done; consumed in LateUpdate so the state
    // pop never happens in the middle of OnUpdate.
    private bool _wantsToFinish;

    public EnemyActing(EnemyAIActionBase action, EnemyContext context)
    {
        _action = action;
        _context = context;
    }

    public override void OnEnter()
    {
        _wantsToFinish = false;
        esm.currentAction = _action;
        if (_action.AggroedAction) EntityManager.Instance.MarkEnemyAware(esm.ts);
        else EntityManager.Instance.MarkEnemyUnaware(esm.ts);
        _action.OnEnter(_context, esm);
    }

    public override void OnUpdate() => _action.OnUpdate(_context, esm);

    public override void OnLateUpdate()
    {
        if (_wantsToFinish)
            esm.sc.ResumePrevious();
    }

    /** <summary>
     * Called by <see cref="EnemyMovementAIAction"/> (and any other action) when the
     * action is complete. The actual state pop is deferred to LateUpdate so it never
     * occurs in the middle of an OnUpdate call, preventing intermittent idle stalls.
     * </summary>
     */
    public void RequestFinish() => _wantsToFinish = true;

    public override void OnExit()
    {
        _action.OnExit(_context, esm);
        esm.AllHitboxesDeactivate();
        esm.ParryWindowDeactivate();
    }
}