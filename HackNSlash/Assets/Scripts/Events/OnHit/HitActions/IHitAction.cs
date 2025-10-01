using UnityEngine;

public abstract class IHitAction
{
    protected OnHitEvents ohe;
    protected PlayerController pc;
    protected PhysicsEnemy ec;
    protected Attack a;
    protected Transform t;
    
    public virtual void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        ohe = onHitEvents;
        pc = ohe.pc;
        a = attack;
        ec = enemy;
        
        t = attackerTransform;
    }


}
