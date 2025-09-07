using System.Collections;
using System.Collections.Generic;
using Animancer;
using Extensions.Utils;
using MEC;
using UnityEngine;

public class EnemyAttacking : EnemyState
{
    private EnemyAttack attack;
    private bool attackFinished;

    private float resetTimer;
    private float resetTime;

    public EnemyAttacking(EnemyAttack attack)
    {
        this.attack = attack;
        attackFinished = false;
    }

    public override void OnEnter()
    {
        resetTimer = 0;
        resetTime = attack.attackCooldown;
        foreach (var clip in attack.attackClips)
        {
            resetTime += clip.Length;
        }
        
        ec.RunSegmentCoroutine(PlayAttackAnimations(), nameof(PlayAttackAnimations));
    }

    public override void OnUpdate()
    {
        resetTimer += Time.deltaTime;
        if (resetTimer > resetTime)
            ec.sc.ResumePrevious();
    }

    private IEnumerator<float> PlayAttackAnimations()
    {
        if (attack.attackClips.Length == 0) yield break;
        
        foreach (var clip in attack.attackClips)
        {
            ec.ea.SwitchAnimState(clip);
            yield return Timing.WaitForSeconds(clip.Length);
        }
    }
}
