using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BaseStats", menuName = "ScriptableObjects/BaseStats", order = 1)]
public class BaseStats : ScriptableObject
{
    [SerializeField] private List<StatInfo> baseStatsList = new();
    
    public readonly Dictionary<InnateStat, int> Stats = new();

    [HideInInspector] public readonly Dictionary<StatusEffect, int> StatusEffects = new();

    private void OnValidate()
    {
        Stats.Clear();
        foreach (var statInfo in baseStatsList)
        {
            if (!Stats.ContainsKey(statInfo.InnateStat))
            {
                Stats.Add(statInfo.InnateStat, statInfo.value);
            }
            else
            {
                Debug.LogWarning($"Duplicate stat {statInfo.InnateStat} found in {name}. Only the first occurrence will be used.");
            }
        }
        
        StatusEffects.Clear();
        foreach (StatusEffect status in Enum.GetValues(typeof(StatusEffect)))
        {
            StatusEffects[status] = 0;
        }
    }
}

[Serializable]
public struct StatInfo
{
    public InnateStat InnateStat;
    public int value;
}
