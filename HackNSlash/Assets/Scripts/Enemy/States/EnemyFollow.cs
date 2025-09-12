using UnityEngine;

public class EnemyFollow : EnemyState
{
    public override void OnEnter()
    {
        ec.ts.ea.SwitchAnimState(ec.enemyAnimData.walkCycle);
        ec.currentWanderPoint = ec.ts.pc.transform.position;
        
        ec.ts.animListener.DeactivateAllHitboxes();

    }

    public override void OnUpdate()
    {
        ec.currentWanderPoint = ec.ts.pc.transform.position;
        
        ec.MoveInDirection(ec.ts.nav.CalculateDirectionToTarget(ec.ts.pc.transform.position));
        CheckIfCloseToPlayer();
        ec.CheckToAttack();
    }

    public override void OnResume()
    {
        base.OnResume();
        ec.ts.ea.SwitchAnimState(ec.enemyAnimData.walkCycle);
        ec.currentWanderPoint = ec.ts.pc.transform.position;
    }

    private void CheckIfCloseToPlayer()
    {
        if (!ec.TargetInRange(ec.ts.pc.transform.position, ec.enemyData.playerChaseRadius,
                ec.enemyData.playerChaseAngle))
        {
            ec.sc.ResumePrevious();
        }
    }
}
