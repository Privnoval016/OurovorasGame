
using Extensions.Timers;
using Extensions.UtilityAI;
using Unity.Entities;

public class EnemyActing : EnemyState
{
    private EnemyAIActionBase action;
    private Context<EnemyAIContextKey> context;
    
    public EnemyActing(EnemyAIActionBase action, Context<EnemyAIContextKey> context)
    {
        this.action = action;
        this.context = context;
    }
    
    public override void OnEnter()
    {
        esm.currentAction = action;
        
        if (action.AggroedAction)
        {
            EntityManager.Instance.MarkEnemyAware(esm.ts);
        }
        else
        {
            EntityManager.Instance.MarkEnemyUnaware(esm.ts);
        }
        
        action.OnEnter(context, esm);
    }

    public override void OnUpdate()
    {
        action.OnUpdate(context, esm);
    }
    
    public override void OnExit()
    {
        action.OnExit(context, esm);
        esm.AllHitboxesDeactivate();
        esm.ParryWindowDeactivate();
    }
}