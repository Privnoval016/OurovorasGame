using System.Collections;
using System.Collections.Generic;
using Animancer;
using Extensions.Utils;
using MEC;
using UnityEngine;

public class EnemyAttacking : EnemyState
{
    private EnemyAttackInfo attackInfo;
    private bool attackFinished;
    
    private Vector3 playerLastKnownPosition;

    private float resetTimer;
    private float resetTime;

    public EnemyAttacking(EnemyAttackInfo attackInfo)
    {
        this.attackInfo = attackInfo;
        attackFinished = false;
    }

    public override void OnEnter()
    {
        playerLastKnownPosition = ec.ts.pc.transform.position;
        
        resetTimer = 0;
        resetTime = attackInfo.attack.attackCooldown;
        foreach (var clip in attackInfo.attack.attackClips)
        {
            resetTime += clip.Length;
        }
        
        ec.ts.currentAttackInfo = attackInfo;
        
        ec.RunSegmentCoroutine(PlayAttackAnimations(), nameof(PlayAttackAnimations));
    }

    public override void OnUpdate()
    {
        resetTimer += Time.deltaTime;
        
        ec.TurnToPosition(playerLastKnownPosition);
        
        if (resetTimer > resetTime)
            ec.sc.ResumePrevious();
    }

    public override void OnExit()
    {
        base.OnExit();
        ec.ts.ea.RootMotionEnabled(false);
        ec.ts.animListener.DeactivateAllHitboxes();
        ec.ts.parryWindowActive = false;
    }

    public override void OnInterrupt()
    {
        base.OnInterrupt();
        ec.ts.ea.RootMotionEnabled(false);
        ec.ts.animListener.DeactivateAllHitboxes();
        ec.ts.parryWindowActive = false;
    }

    private IEnumerator<float> PlayAttackAnimations()
    {
        if (attackInfo.attack.attackClips.Length == 0) yield break;
        
        ec.ts.ea.RootMotionEnabled(attackInfo.attack.useRootMotion);
        
        foreach (var clip in attackInfo.attack.attackClips)
        {
            ec.ts.ea.SwitchAnimState(clip);
            yield return Timing.WaitForSeconds(clip.Length);
        }
    }
}
