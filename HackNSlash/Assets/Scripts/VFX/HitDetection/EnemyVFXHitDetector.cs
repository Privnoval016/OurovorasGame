using UnityEngine;

public class EnemyVFXHitDetector : VFXHitDetector
{
    public EnemyController ts;
    public EnemyAttackInfo attackInfo;
    
    public EnemyVFXHitDetector(VFXController vfx, EnemyController ts, EnemyAttackInfo attackInfo) : base(vfx, ts.transform)
    {
        this.ts = ts;
        this.attackInfo = attackInfo;
    }
    
    public override void HitboxTriggerEnter(Collider other)
    {
        if (!vfx.activeHitbox || !vfx.vfxEnabled) return;
        
        if (ts == null || attackInfo == null) return;
        
        if (other.TryGetComponent(out PlayerController pc))
        {
            pc.psm.CheckEnemyProjectileCollision(this);
        }
    }
    
    public override void HitboxTriggerStay(Collider other)
    {
        if (!vfx.activeHitbox || !vfx.vfxEnabled) return;
        
        if (ts == null || attackInfo == null) return;
        
        if (other.TryGetComponent(out PlayerController pc))
        {
            pc.psm.CheckEnemyProjectileCollision(this);
        }
    }
}
