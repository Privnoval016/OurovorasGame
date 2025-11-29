using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class RangedSpiritAction : ISpiritAction
{
    
    [Header("Ranged Attack Parameters")]
    [Tooltip("Time in seconds the player must hold the attack before the spirit fires a charged shot.")]
    public float spiritProjectileHoldTime = 0.5f;
    [Tooltip("Number of targets to hit")]
    public int numTargets = 1;
    
    public override void Execute(OnSpiritEvents onSpiritEvents, SpiritAttack attack)
    {
        base.Execute(onSpiritEvents, attack);

        ose.RunSegmentCoroutine(BeginRangedAttack());
    }
    
    private IEnumerator<float> BeginRangedAttack()
    {
        ElementEffect element = spirit.pc.pcc.currentElementEffect;
        
        HashSet<LockOnTarget> enemies = spirit.pc.HitScanEnemies(numTargets, a.hitInfo.lateralRadius, a.hitInfo.verticalRadius, a.hitInfo.hitRegisterAngle, a);
        
        KeyBind[] holdKeys = InputManager.GetReleaseable(a.keyBinds);
        float startTime = Time.time;
        yield return Timing.WaitUntilTrue(() => holdKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
        float elapsedTime = Time.time - startTime;

 
        if (elapsedTime < spiritProjectileHoldTime)
        {
            foreach (LockOnTarget enemy in enemies)
            {
                CreateVFX(0);
                enemy.OnHit(element, spirit.pc, a, spirit.transform, 0);
                Services.CombatSystem.PlayHitEffects(a.element, spirit.pc, a, spirit, true, 0);
            }
        }
        else
        {
            foreach (LockOnTarget enemy in enemies)
            {
                CreateVFX(0);
                enemy.OnHit(element, spirit.pc, a, spirit.transform, 1);
                Services.CombatSystem.PlayHitEffects(a.element, spirit.pc, a, spirit, true, 1);
            }
        }
        
        ose.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown));
    }
}
