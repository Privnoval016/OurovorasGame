using Extensions.Utils;
using Pathfinding;
using UnityEngine;

public class EnemyWander : EnemyState
{
    public override void OnEnter()
    {
        ec.ts.ea.SwitchAnimState(ec.enemyAnimData.walkCycle);
        UpdateWanderPoint();
        ec.ts.animListener.DeactivateAllHitboxes();

    }

    public override void OnUpdate()
    {
        ec.CheckToFollowPlayer(ec.enemyData.playerDetectionRadius, ec.enemyData.playerDetectionAngle);
        ec.CheckToAttack();
        SetWanderingMovement();
    }

    public override void OnResume()
    {
        base.OnResume();
        ec.ts.ea.SwitchAnimState(ec.enemyAnimData.walkCycle);
        ec.currentWanderPoint = ec.ts.pc.transform.position;
    }

    private void SetWanderingMovement()
    {
        CheckDestinationReached();
        
        ec.MoveInDirection(ec.ts.nav.CalculateDirectionToTarget(ec.currentWanderPoint));
    }
    
    
    private Vector3 PickRandomPoint() 
    {
        ConstantPath path = ConstantPath.Construct(ec.transform.position, ec.enemyData.wanderSearchLength);

        AstarPath.StartPath(path);
        path.BlockUntilCalculated();
        var point = PathUtilities.GetPointsOnNodes(path.allNodes, 1)[0];
        return point;
    }

    private void CheckDestinationReached()
    {
        if (Vector3.Distance(ec.transform.position.ZeroVector3Axis(), ec.currentWanderPoint.ZeroVector3Axis()) < ec.enemyData.targetClosenessDistance)
            ec.sc.ResumePrevious();
    }

    private void UpdateWanderPoint()
    {
        if (ec.wanderPoints.Length == 0)
        {
            ec.wanderIndex = -1;
            ec.currentWanderPoint = PickRandomPoint();
        }
        else
        {
            ec.wanderIndex = (ec.wanderIndex + 1) % ec.wanderPoints.Length;
            ec.currentWanderPoint = ec.wanderPoints[ec.wanderIndex].position;
        }
    }
}
