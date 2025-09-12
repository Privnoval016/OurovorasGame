using UnityEngine;

public class EnemyStagger : EnemyState
{
    public override void OnEnter()
    {
        base.OnEnter();
        ec.ts.ea.SwitchAnimState(ec.enemyAnimData.staggerClip, ExitStagger);
    }
    
    private void ExitStagger()
    {
        ec.sc.ResumePrevious();
    }
}
