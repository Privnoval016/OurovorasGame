using UnityEngine;

public class EnemyAnimListener : MonoBehaviour
{
    [HideInInspector] public EnemyController ts;
    
    public void ActivateHitbox(int index = 0)
    {
        if (ts.lot is EnemyStateMachine ec)
        {
            if (!ec.sc.IsState<EnemyAttacking>()) return;

            ts.attackHitboxes[index].activeHitbox = true;
        }
        else if (ts.lot is BossStateMachine bc)
        {
            // Boss logic
        }
    }
    
    public void DeactivateHitbox(int index = 0)
    {
        if (ts.lot is EnemyStateMachine ec)
        {
            ts.attackHitboxes[index].activeHitbox = false;
        }
        else if (ts.lot is BossStateMachine bc)
        {
            // Boss logic
        }
    }
    
    public void ActivateAllHitboxes()
    {
        if (ts.lot is EnemyStateMachine ec)
        {
            if (!ec.sc.IsState<EnemyAttacking>()) return;

            foreach (EnemyHitbox eh in ts.attackHitboxes)
            {
                eh.activeHitbox = true;
            }
        }
        else if (ts.lot is BossStateMachine bc)
        {
            // Boss logic
        }
    }
    
    public void DeactivateAllHitboxes()
    {
        if (ts.lot is EnemyStateMachine ec)
        {
            foreach (EnemyHitbox eh in ts.attackHitboxes)
            {
                eh.activeHitbox = false;
            }
        }
        else if (ts.lot is BossStateMachine bc)
        {
            // Boss logic
        }
    }
    
    public void ActivateParryWindow()
    {
        if (ts.lot is EnemyStateMachine ec)
        {
            if (!ec.sc.IsState<EnemyAttacking>()) return;

            ts.parryWindowActive = true;
        }
        else if (ts.lot is BossStateMachine bc)
        {
            // Boss logic
        }
    }
    
    public void DeactivateParryWindow()
    {
        if (ts.lot is EnemyStateMachine ec)
        {
            ts.parryWindowActive = false;
        }
        else if (ts.lot is BossStateMachine bc)
        {
            // Boss logic
        }
    }
}
