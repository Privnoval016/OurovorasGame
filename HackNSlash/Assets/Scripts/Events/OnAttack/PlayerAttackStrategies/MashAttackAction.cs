using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class MashAttackAction : IAttackAction
{
    
    [Header("Mash Attack Settings")]
    [Tooltip("Time interval within which the next key press must occur to continue the mash")]
    public float mashInterval = 0.4f;
    [Tooltip("Total duration allowed for the mash attack")]
    public float mashDuration = 1.5f;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(BeginMashAttack());
    }
    
    IEnumerator<float> BeginMashAttack()
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        float timeSinceLastClick = 0;
        float startTime = Time.time;
        
        pc.psm.pauseComboReset = true;
        
        while (timeSinceLastClick < mashInterval && Time.time - startTime < mashDuration)
        {
            if (InputManager.KeyMap[a.keyBinds[0]].action())
            {
                timeSinceLastClick = 0;
            }
            
            timeSinceLastClick += Time.deltaTime;
            
            yield return Timing.WaitForOneFrame;
        }
        
        
        if (timeSinceLastClick >= mashInterval)
        {
            oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
            pc.pac.PlayAnimation(a.attackClips[1], a.animFade);
        }
        else
        {
            oae.RunSegmentCoroutine(ResumeMoving(a.hitInfo.attackCoolDown * 2, () => pc.psm.pauseComboReset = false));
            pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
        }
    }
}
