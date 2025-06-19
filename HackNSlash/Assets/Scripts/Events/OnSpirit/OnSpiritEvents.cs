using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using UnityEngine;
using MEC;

public enum OnSpiritActions
{
    None,
    RangedAttack,
    SpawnVFX,
}

public class OnSpiritEvents : Singleton<OnSpiritEvents>
{
    
    public Dictionary<OnSpiritActions, Action<ElementalSpirit, SpiritAttack>> OnSpiritActionMap = new();
    
    public OnSpiritParameters parameters;
    
    protected override void Awake()
    {
        base.Awake();
        
        AddOnSpiritEvents();
    }
    
    private void AddOnSpiritEvents()
    {
        OnSpiritActionMap.Add(OnSpiritActions.None, (spirit, a) => { });
        OnSpiritActionMap.Add(OnSpiritActions.RangedAttack, RangedAttack);
        OnSpiritActionMap.Add(OnSpiritActions.SpawnVFX, SpawnVFX);
    }
    
    IEnumerator<float> ResumeMoving(ElementalSpirit spirit, Attack a, float time, Action action = null)
    {
        yield return Timing.WaitForSeconds(time);
        action?.Invoke();
        spirit.canAttack = true;
    }

    #region Ranged Attack
    
    private void RangedAttack(ElementalSpirit spirit, SpiritAttack a)
    {
        this.RunSegmentCoroutine(BeginRangedAttack(spirit, a));
    }
    
    private IEnumerator<float> BeginRangedAttack(ElementalSpirit spirit, SpiritAttack a)
    {
        ElementEffect element = spirit.pc.pi.CurrentElementEffect;
        
        HashSet<LockOnTarget> enemies = spirit.HitScanEnemies(a.hitInfo.numTargets, a.hitInfo.lateralRadius, a.hitInfo.verticalRadius, a.hitInfo.hitRegisterAngle);
        
        KeyBind[] holdKeys = InputManager.GetReleaseable(a.keyBinds);
        float startTime = Time.time;
        yield return Timing.WaitUntilTrue(() => holdKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
        float elapsedTime = Time.time - startTime;

 
        if (elapsedTime < parameters.spiritProjectileHoldTime)
        {
            foreach (LockOnTarget enemy in enemies)
            {
                CreateVFX(spirit, a, 0);
                enemy.OnHit(element, spirit.pc, a, 0);
            }
        }
        else
        {
            foreach (LockOnTarget enemy in enemies)
            {
                CreateVFX(spirit, a, 0);
                enemy.OnHit(element, spirit.pc, a, 1);
            }
        }
        
        this.RunSegmentCoroutine(ResumeMoving(spirit, a, a.hitInfo.attackCoolDown));
    }

    #endregion
    
    #region SpawnVFX

    private void SpawnVFX(ElementalSpirit spirit, SpiritAttack a)
    {
        this.RunSegmentCoroutine(BeginSpawnVFX(spirit, a));
    }
    
    private IEnumerator<float> BeginSpawnVFX(ElementalSpirit spirit, SpiritAttack a)
    {
        for (int i = 0; i < a.vfxInfos.Length; i++)
        {
            CreateVFX(spirit, a, i);
        }

        this.RunSegmentCoroutine(ResumeMoving(spirit, a, parameters.vfxCooldownTime));

        yield break;
    }

    private VFXController CreateVFX(ElementalSpirit spirit, SpiritAttack a, int i)
    {
        return OnVFXEvents.Instance.InvokeOnVFX(spirit.pc, a, i);
    }

    #endregion
}
