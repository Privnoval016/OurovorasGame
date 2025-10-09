using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "BaseStats", menuName = "ScriptableObjects/BaseStats", order = 1)]
public class BaseStats : SerializedScriptableObject
{
    public readonly Dictionary<InnateStat, int> Stats = new();
    
    private Dictionary<StatusEffect, int> statusEffects = new();

    public Dictionary<StatusEffect, int> GetBaseStatusEffects()
    {
        if (statusEffects == null || statusEffects.Count == 0)
        {
            Initialize();
        }
        
        return statusEffects;
    }

    private void Initialize()
    {
        statusEffects = new Dictionary<StatusEffect, int>();
        statusEffects.Clear();
        foreach (var statusEffect in EffectManager.Instance.statusEffects)
        {
            statusEffects[statusEffect] = 0;
        }
    }
}
