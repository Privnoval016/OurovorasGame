using UnityEngine;

public class EnemyStagger : EnemyState
{
    public override void OnEnter()
    {
        base.OnEnter();
        esm.PauseUtilityAITimer(true);
        
        EntityManager.Instance.MarkEnemyAware(esm.ts);
        
        esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.staggerClip, ExitStagger);
    }

    public override void OnExit()
    {
        base.OnExit();
        esm.PauseUtilityAITimer(false);
    }

    private void ExitStagger()
    {
        esm.sc.ResumePrevious();
    }
}
