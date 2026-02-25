using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class LaunchUpAttackAction : IAttackAction
{
    [Header("Launch Up Settings")]
    [Tooltip("Height to launch up")]
    public float launchUpHeight = 15f;
    [Tooltip("Time taken to complete the launch up")]
    public float launchUpTime = 0.3f;
    [Tooltip("Time to hold the button before launching up")]
    public float launchUpHoldTime = 0.25f;
    
    [Tooltip("Whether to play the initial animation before launching up")]
    public bool delayLaunchUp = true;
    
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
        
        oae.RunSegmentCoroutine(BeginLaunchUp());
    }
    
    private IEnumerator<float> BeginLaunchUp()
    {
        if (delayLaunchUp)
        {
            KeyBind[] holdKeys = InputManager.GetHoldable(a.keyBinds);

            yield return Timing.WaitForSeconds(launchUpHoldTime);

            if (!holdKeys.Any(k => InputManager.KeyMap[k].holdAction()))
            {
                yield break;
            }
        }
        
        yield return Timing.WaitForSeconds(a.animDelay);

        pc.pac.PlayAnimation(a.attackClips[1], a.animFade, false);
        
        oae.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(Vector3.up, launchUpHeight, launchUpTime));

        yield return Timing.WaitForSeconds(launchUpTime);
        
        pc.rb.linearVelocity = pc.psm.playerData.jumpHangSpeedThreshold * Vector3.up;
    }
}
