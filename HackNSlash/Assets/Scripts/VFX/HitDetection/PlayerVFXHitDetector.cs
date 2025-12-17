using UnityEngine;

public class PlayerVFXHitDetector : VFXHitDetector
{
    public PlayerController player;
    public Attack attack;
    public WeaponType followedWeaponType = WeaponType.None;
    
    public PlayerVFXHitDetector(VFXController vfx, PlayerController player, Attack attack) : base(vfx, player.transform)
    {
        this.player = player;
        this.attack = attack;
    }
    
    public override void HitboxTriggerEnter(Collider other)
    {
        if (!vfx.activeHitbox || !vfx.vfxEnabled) return;
        
        if (player == null || attack == null) return;
        
        if (other.TryGetComponent(out LockOnTarget enemy) && !enemy.TookDamageThisAction(attack))
        {
            enemy.OnHit(vfx.elementType, player, attack, vfx.transform, vfx.vfxSpawnInfo.vfxPlayerActionIndex);
            Services.Get<CombatSystem>().PlayHitEffects(vfx.elementType, player, attack, vfx, true);
        }
    }

    public override void HitboxTriggerStay(Collider other)
    {
        if (!vfx.activeHitbox || !vfx.vfxEnabled) return;
        
        if (player == null || attack == null) return;
        
        if (other.TryGetComponent(out LockOnTarget enemy) && !enemy.TookDamageThisAction(attack))
        {
            enemy.OnHit(vfx.elementType, player, attack, vfx.transform, vfx.vfxSpawnInfo.vfxPlayerActionIndex);
            Services.Get<CombatSystem>().PlayHitEffects(vfx.elementType, player, attack, vfx, true);
        }
    }
}
