using Extensions.UtilityAI;
using Unity.Entities;

public class EnemyActing : EnemyState
{
    private readonly EnemyAIActionBase _action;
    private readonly EnemyContext _context;

    public EnemyActing(EnemyAIActionBase action, EnemyContext context)
    {
        _action = action;
        _context = context;
    }

    public override void OnEnter()
    {
        esm.currentAction = _action;
        if (_action.AggroedAction) EntityManager.Instance.MarkEnemyAware(esm.ts);
        else EntityManager.Instance.MarkEnemyUnaware(esm.ts);
        _action.OnEnter(_context, esm);
    }

    public override void OnUpdate() => _action.OnUpdate(_context, esm);

    public override void OnExit()
    {
        _action.OnExit(_context, esm);
        esm.AllHitboxesDeactivate();
        esm.ParryWindowDeactivate();
    }
}