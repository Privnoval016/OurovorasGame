using UnityEngine;

public class EnemyFollow : EnemyState
{
    public override void OnEnter()
    {
        ec.ea.SwitchAnimState(ec.enemyAnimData.walkCycle);
        ec.currentWanderPoint = ec.pc.transform.position;
    }

    public override void OnUpdate()
    {
        ec.currentWanderPoint = ec.pc.transform.position;
        
        ec.MoveInDirection(ec.nav.CalculateDirectionToTarget(ec.pc.transform.position));
        CheckIfCloseToPlayer();
    }

    public override void OnResume()
    {
        base.OnResume();
        ec.ea.SwitchAnimState(ec.enemyAnimData.walkCycle);
        ec.currentWanderPoint = ec.pc.transform.position;
    }

    private void CheckIfCloseToPlayer()
    {
        if (!ec.TargetInRange(ec.pc.transform.position, ec.enemyData.playerDetectionRadius,
                ec.enemyData.playerDetectionAngle))
        {
            ec.sc.ResumePrevious();
        }
        
        var a = ec.CheckForAvailableAttack(ec.pc);

        if (a != null)
        {
            ec.sc.Interrupt(new EnemyAttacking((EnemyAttack) a));
        }
    }
}
