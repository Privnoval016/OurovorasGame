
using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using Systems;
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

        ts.esm.sensor.GetNearestTarget(EnemyContextKeys.Player)?.TryGetComponent(out pc);

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
        ts.ea.RootMotionEnabled(a.attack.useRootMotion);

        if (a.attack.attackClips.Length > 0)
        {
            yield return Timing.WaitForSeconds(a.attack.animDelay);

            foreach (var clip in a.attack.attackClips)
            {
                ts.ea.PlayEnemyAnimation(clip);
                yield return Timing.WaitForSeconds(clip.MaximumDuration);
            }
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
        // Pop EnemyActing so EnemyInitialState.OnResume fires and the brain re-evaluates.
        ts.esm.sc.ResumePrevious();
    }

    protected VFXController CreateVFX(int index, TransformInfo start = default)
    {
        return Services.Get<VFXSystem>().SpawnEnemyVFX(ts, a, index, start);
    }
}