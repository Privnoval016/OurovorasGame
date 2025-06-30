using Pathfinding;
using UnityEngine;

public class EnemyHit : EnemyState
{
    public override void OnEnter()
    {
        ec.SwapToIdle(true);
    }

    public override void OnUpdate()
    {
        if (!ec.IsMidAttack && ec.IsGrounded)
        {
            ec.sc.ResumePrevious();
        }
    }
}
