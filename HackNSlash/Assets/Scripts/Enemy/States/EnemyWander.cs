using Pathfinding;
using UnityEngine;

public class EnemyWander : EnemyState
{
    private float wanderTimer;
    
    public override void OnEnter()
    {
        doNotRemove = true;
        UpdateWanderPoint();
    }

    public override void OnUpdate()
    {
        ec.CheckForPlayer(ec.enemyData.playerDetectionRadius, ec.enemyData.playerDetectionAngle);
        SetWanderingMovement();
    }

    private void SetWanderingMovement()
    {
        wanderTimer += Time.deltaTime;
        
        if (!ec.wanderIdling)
        {            
            if (Vector3.Distance(ec.transform.position, ec.currentWanderPoint) <
                         ec.enemyData.targetClosenessDistance)
            {
                ChangeWaypointOrIdle();
            }
            
        }
        else
        {
            if (wanderTimer >= 1f)
            {
                wanderTimer = 0f;
                CheckResumeWandering();
            }
        }
        
        
        if (!ec.wanderIdling) ec.MoveInDirection(ec.nav.CalculateDirectionToTarget(ec.currentWanderPoint));
    }

    private void ChangeWaypointOrIdle()
    {
        bool swap = Random.Range(0f, 1f) < ec.enemyData.wanderIdleChance;
        if (swap)
        {
            ec.wanderIdling = true;
        }
        else
        {
            ec.wanderIdling = false;
            UpdateWanderPoint();
        }
    }
    
    private void CheckResumeWandering()
    {
        bool swap = Random.Range(0f, 1f) < ec.enemyData.wanderMoveChance;
        if (swap)
        {
            ec.wanderIdling = false;
            UpdateWanderPoint();
        }
        else
        {
            ec.wanderIdling = true;
        }
    }
    
    private Vector3 PickRandomPoint() 
    {
        ConstantPath path = ConstantPath.Construct(ec.transform.position, ec.enemyData.wanderSearchLength);

        AstarPath.StartPath(path);
        path.BlockUntilCalculated();
        var point = PathUtilities.GetPointsOnNodes(path.allNodes, 1)[0];
        return point;
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
