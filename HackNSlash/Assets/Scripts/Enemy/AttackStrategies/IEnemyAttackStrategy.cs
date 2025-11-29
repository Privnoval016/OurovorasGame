
using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;


public abstract class IEnemyAttackStrategy
{
    protected OnEnemyEvents oee;
    protected EnemyController ts;
    protected EnemyAttackAIAction a;
    protected PlayerController pc;
    
    public void Execute(OnEnemyEvents onEnemyEvents, EnemyAttackAIAction attack)
    { 
        oee = onEnemyEvents;
        ts = oee.ts;
        a = attack;

        ts.esm.sensor.GetNearestDetectedObject(EnemyAIContextKey.Player).TryGetComponent(out pc);

        Debug.Log("Executing Enemy Strategy");
        
        ts.esm.SetIsAttacking(true);

        oee.RunSegmentCoroutine(LaunchClipAttacks());
        
        OnExecute();
    }

    protected virtual void OnExecute()
    {
        
    }

    private IEnumerator<float> LaunchClipAttacks()
    {
        if (a.attack.attackClips.Length == 0) yield break;

        ts.ea.RootMotionEnabled(a.attack.useRootMotion);
        
        yield return Timing.WaitForSeconds(a.attack.animDelay);
        
        foreach (var clip in a.attack.attackClips)
        {
            ts.ea.PlayEnemyAnimation(clip);
            yield return Timing.WaitForSeconds(clip.MaximumDuration);
        }

        if (a.attack.exitConditions == ExitConditions.AnimationEnd)
        {
            oee.RunSegmentCoroutine(ResumeMoving(0));
        }
    }
    
    protected IEnumerator<float> ResumeMoving(float time, Action action = null)
    {
        yield return Timing.WaitForSeconds(time);
        action?.Invoke();
        ts.ea.RootMotionEnabled(false);
        ts.esm.SetIsAttacking(false);
    }

    protected VFXController CreateVFX(int index, TransformInfo start = default)
    {
        return Services.VFXSystem.SpawnEnemyVFX(ts, a, index, start);
    }
}