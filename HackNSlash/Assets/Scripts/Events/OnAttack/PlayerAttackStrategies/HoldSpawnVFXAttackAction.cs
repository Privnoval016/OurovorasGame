using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class HoldSpawnVFXAttackAction : IAttackAction
{
    [Header("Hold Spawn VFX Parameters")]
    [Tooltip("Indices of VFX to spawn from the PlayerAttack's VFX list after holding the attack.")]
    public int[] vfxIndices;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(BeginHoldSpawnVFX());
    }
    
    private IEnumerator<float> BeginHoldSpawnVFX()
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        KeyBind[] holdKeys = InputManager.GetReleaseable(a.keyBinds);
        float startTime = Time.time;
        yield return Timing.WaitUntilTrue(() => holdKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
        float elapsedTime = Time.time - startTime;

        pc.spirit.lastAttackHoldDuration = elapsedTime;
        
        pc.pac.PlayAnimation(a.attackClips[1], a.animFade);

        foreach (int i in vfxIndices)
        {
            CreateVFX(i);
        }
        
        oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown));
    }
}
