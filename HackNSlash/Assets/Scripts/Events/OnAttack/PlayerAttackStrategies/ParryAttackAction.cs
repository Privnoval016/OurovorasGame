using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class ParryAttackAction : IAttackAction
{
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(BeginParry()).OnDestroy(() => pc.ps.isInvincible = false);
    }
    
    private IEnumerator<float> BeginParry()
    {
        yield return Timing.WaitForSeconds(a.animDelay);

        if (pc.wc.activeWeapons.Count == 0)
        {
            pc.ps.isInvincible = false;
            yield break;
        }
        
        pc.rb.linearVelocity = Vector3.zero;
        
        pc.ps.isInvincible = true;

        HashSet<PhysicsEnemy> parriedEnemies = new();
        foreach (var hitbox in pc.psm.ParriedHitboxes)
        {
            parriedEnemies.Add(hitbox.ts.pe);
        }

        foreach (var e in parriedEnemies)
        {
            e.OnStagger(pc.pcc.currentElementEffect, pc, a, pc.transform, 0);
        }
        
        Collider[] hitboxes = parriedEnemies.Select(e => e.col).ToArray();
        
        CombatManager.Instance.PlayParryEffects(hitboxes, a.element, pc, a, pc.wc.activeWeapons[0], true, 0);
        
        yield return Timing.WaitForSeconds(a.attackClips[0].length);
        
        pc.ps.isInvincible = false;
    }
}
