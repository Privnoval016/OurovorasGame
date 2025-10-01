using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class HitScanSpiritAction : ISpiritAction
{
    [Header("Hit Scan VFX Parameters")]
    [Tooltip("Indices of VFX to spawn from the SpiritAttack's VFX list in a hit scan manner.")]
    public int[] vfxInstantSpawns;
    [Tooltip("Number of targets to hit")]
    public int numTargets = 7;
    
    public override void Execute(OnSpiritEvents onSpiritEvents, SpiritAttack attack)
    {
        base.Execute(onSpiritEvents, attack);
        
        spirit.canAttack = false;
        
        ose.RunSegmentCoroutine(BeginHitScanVFX());
    }
    
    
    private IEnumerator<float> BeginHitScanVFX()
    {
        ElementEffect element = spirit.pc.pi.currentElementEffect;
        
        HashSet<LockOnTarget> enemies = spirit.pc.HitScanEnemies(numTargets, a.hitInfo.lateralRadius, a.hitInfo.verticalRadius, a.hitInfo.hitRegisterAngle, a);

        int maxVFX = vfxInstantSpawns.Length;
        
        LockOnTarget[] enemiesToHit = new LockOnTarget[maxVFX];
        int[] vfxToUse = new int[maxVFX];
        
        for (int i = 0; i < maxVFX; i++)
        {
            enemiesToHit[i] = enemies.ElementAtOrDefault(i % enemies.Count);
            vfxToUse[i] = vfxInstantSpawns[i];
        }
        
        for (int i = 0; i < maxVFX; i++)
        {
            CreateVFX(vfxToUse[i], new TransformInfo(enemiesToHit[i].transform, false));
            enemiesToHit[i].OnHit(element, spirit.pc, a, spirit.transform, 0);
            CombatManager.Instance.PlayHitEffects(a.element, spirit.pc, a, spirit, true, 0);
            
            yield return Timing.WaitForSeconds(a.hitInfo.attackCoolDown);
        }
        
        ose.RunSegmentCoroutine(ResumeMoving(0));
        
        yield break;
    }
}
