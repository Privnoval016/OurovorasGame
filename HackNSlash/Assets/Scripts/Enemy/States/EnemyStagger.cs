using UnityEngine;

public class EnemyStagger : EnemyState
{
    public override void OnEnter()
    {
        base.OnEnter();
        esm.PauseUtilityAITimer(true);
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
