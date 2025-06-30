using UnityEngine;

public class EnemyFollow : EnemyState
{
    public override void OnEnter()
    {
        ec.currentWanderPoint = ec.pc.transform.position;
        ec.wanderIdling = false;
    }

    public override void OnUpdate()
    {
        ec.MoveInDirection(ec.nav.CalculateDirectionToTarget(ec.pc.transform.position));
    }
}
