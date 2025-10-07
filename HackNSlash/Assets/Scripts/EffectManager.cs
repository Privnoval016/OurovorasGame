using System;
using System.Collections.Generic;
using Extensions.Patterns;
using UnityEngine;

public class EffectManager : Singleton<EffectManager>
{
    [SerializeField] private List<StatusEffectInfo> statusEffects = new();
    
    #region MonoBehaviour Callbacks
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    #endregion
    
    
    #region Status Effect Methods
    
    public StatusEffect GetStatusEffect(StatusEffect statusEffect)
    {
        statusEffect ??= new NoStatusEffect();
        
        foreach (var effectInfo in statusEffects)
        {
            if (effectInfo.statusEffect.Equals(statusEffect))
                return effectInfo.statusEffect;
        }

        Debug.LogError($"Status Effect {statusEffect} not found in EffectManager.");
        return null;
    }
    
    #endregion
}

[Serializable]
public class StatusEffectInfo
{
    [SerializeReference] public StatusEffect statusEffect;
}