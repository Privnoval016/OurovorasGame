using System;
using System.Collections.Generic;
using Extensions.Utils;
using UnityEngine;
using MEC;

public enum OnEnemyActions
{
    None, 
    SpawnVFXAtHitbox,
    // add more as needed
}

// handles events that are triggered by the enemy through attacks, like projectiles, dashes, etc.
public class OnEnemyEvents : MonoBehaviour
{
    [HideInInspector] public EnemyController ts;
    
    public readonly Dictionary<OnEnemyActions, Action<EnemyAttackInfo>> OnEnemyActionMap = new();

    private void Awake()
    {
        ts = GetComponent<EnemyController>();
        
        AddOnEnemyMethods();
    }

    private void AddOnEnemyMethods()
    {
        OnEnemyActionMap.Add(OnEnemyActions.None, (a) => { });
        OnEnemyActionMap.Add(OnEnemyActions.SpawnVFXAtHitbox, SpawnVFXAtHitbox);
    }
    
    public void TriggerOnEnemyAction(EnemyAttackInfo a)
    {
        if (OnEnemyActionMap.TryGetValue(a.attack.attackAction, out Action<EnemyAttackInfo> method))
        {
            method.Invoke(a);
        }
    }
    
    #region SpawnVFX
    
    private void SpawnVFXAtHitbox(EnemyAttackInfo a)
    {
        Timing.RunCoroutine(BeginSpawnVFXAtHitbox(a));
    }
    
    private IEnumerator<float> BeginSpawnVFXAtHitbox(EnemyAttackInfo a)
    {
        VFXSpawnInfo vfxInfo = a.attack.vfxInfos[a.attack.attackEventIndex];
        yield return Timing.WaitUntilTrue(() => ts.attackHitboxes[vfxInfo.vfxActionIndex].activeHitbox);
        
        CreateVFX(a, a.attack.attackEventIndex, new TransformInfo(ts.attackHitboxes[vfxInfo.vfxActionIndex].transform));
    }
    
    

    private VFXController CreateVFX(EnemyAttackInfo a, int index, TransformInfo start = default)
    {
        return OnVFXEvents.Instance.SpawnEnemyVFX(ts, a, index, start);
    }
    
    #endregion
}
