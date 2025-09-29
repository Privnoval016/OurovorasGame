using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class ProjectileParryAttackAction : IAttackAction
{
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(BeginProjectileParry())
            .OnDestroy(() =>
            {
                pc.pi.isInvincible = false;
            });
    }
    
    private IEnumerator<float> BeginProjectileParry()
    {
        yield return Timing.WaitForSeconds(a.animDelay);

        if (pc.wc.activeWeapons.Count == 0)
        {
            pc.pi.isInvincible = false;
            yield break;
        }
        
        pc.rb.linearVelocity = Vector3.zero;
        
        pc.pi.isInvincible = true;
        
        foreach (VFXHitbox h in pc.psm.ParriedProjectiles)
        {
            Vector3 startPosition = h.transform.position;
            Vector3 moveDirection = (h.HitDetector.creatorTransform.position - startPosition).normalized;
            CreateVFX(h.HitDetector.vfx.vfxSpawnInfo, new TransformInfo(startPosition, 
                Quaternion.LookRotation(moveDirection), 
                Vector3.one));
        }
        
        List<Collider> hitboxes = new();
        foreach (VFXHitbox h in pc.psm.ParriedProjectiles)
        {
            hitboxes.Add(h.col);
        }

        CombatManager.Instance.PlayParryEffects(hitboxes.ToArray(), a.element, pc, a, pc.wc.activeWeapons[0], true, 1);

        foreach (var h in pc.psm.ParriedProjectiles)
        {
            h.HitDetector.vfx.DisableVFX();
        }
        
        yield return Timing.WaitForSeconds(a.attackClips[0].length);
    }
}
