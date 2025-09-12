using UnityEngine;

public class EnemyIdle : EnemyState
{
    private float idleTimer;
    private float idleDuration;
    
    public override void OnEnter()
    {
        idleTimer = 0;
        idleDuration = Random.Range(ec.enemyData.wanderMoveChance.x, ec.enemyData.wanderMoveChance.y);
        doNotRemove = true;
        
        ec.ts.animListener.DeactivateAllHitboxes();

        ec.ts.ea.SwitchAnimState(ec.enemyAnimData.idleClip);
    }

    public override void OnUpdate()
    {
        ec.CheckToFollowPlayer(ec.enemyData.playerDetectionRadius, ec.enemyData.playerDetectionAngle);
        CheckToWander();
        ec.CheckToAttack();
        Debug.Log(idleTimer);
    }

    public override void OnResume()
    {
        idleTimer = 0;
        idleDuration = Random.Range(ec.enemyData.wanderMoveChance.x, ec.enemyData.wanderMoveChance.y);
        
        ec.ts.ea.SwitchAnimState(ec.enemyAnimData.idleClip);
    }

    private void CheckToWander()
    {
        idleTimer += Time.deltaTime;

        if (idleTimer >= idleDuration)
        {
            idleTimer = 0;
            idleDuration = Random.Range(ec.enemyData.wanderMoveChance.x, ec.enemyData.wanderMoveChance.y);
            ec.sc.ChangeState(new EnemyWander());
        }
    }
}
