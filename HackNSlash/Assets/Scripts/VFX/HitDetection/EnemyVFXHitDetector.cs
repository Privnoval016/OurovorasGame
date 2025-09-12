using UnityEngine;

public class EnemyVFXHitDetector : VFXHitDetector
{
    EnemyController ts;
    public EnemyAttack attack;
    
    public EnemyVFXHitDetector(VFXController vfx, EnemyController ts, EnemyAttack attack) : base(vfx, ts.transform)
    {
        this.ts = ts;
        this.attack = attack;
    }
    
    public override void HitboxTriggerEnter(Collider other)
    {
        
    }
    
    public override void HitboxTriggerStay(Collider other)
    {

    }
}
