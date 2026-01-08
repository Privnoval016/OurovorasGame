using System;
using System.Collections.Generic;
using Extensions.Modifiers;
using UnityEngine;
using UnityEngine.Serialization;

public class EvaluatedStats
{
    private readonly BaseStats baseStats;
    private Dictionary<InnateStat, int> leveledStats; // Stats after applying level-based increases
    
    public readonly Mediator<StatQueryKey> StatMediator;
    public readonly Mediator<StatusEffectQueryKey> StatusEffectMediator;
    
    private Dictionary<InnateStat, int> cachedStats;
    private Dictionary<StatusEffect, int> cachedStatusEffects;

    private readonly Func<int> GetLevel;

    public Dictionary<InnateStat, int> Stats()
    {
        return cachedStats;
    }
    
    private int GetLeveledStat(InnateStat innateStat)
    {
        int level = GetLevel();
        switch (innateStat)
        {
            case InnateStat.MaxHealth:
                return Mathf.FloorToInt((2 * baseStats.Stats[InnateStat.MaxHealth] * level) / 100f) + level + 10; // HP scales differently to compensate for low levels
            case InnateStat.MaxCharge:
                return baseStats.Stats[InnateStat.MaxCharge]; // MaxCharge does not scale with level
            default:
                return Mathf.FloorToInt((2 * baseStats.Stats[innateStat] * level) / 100f) + 5;
        }
    }
    
    private void EvaluateStats()
    {
        var evaluatedStats = new Dictionary<InnateStat, int>();
        foreach (var kvp in baseStats.Stats)
        {
            int leveledValue = GetLeveledStat(kvp.Key);
            
            var query = new StatQueryKey(kvp.Key);
            var queryContext = new QueryContext<StatQueryKey>(query,
                leveledValue,
                leveledValue);
            StatMediator.PerformQuery(this, queryContext);
            evaluatedStats[kvp.Key] = queryContext.CurrentValue;
        }
        
        cachedStats = evaluatedStats;
    }
    
    public Dictionary<StatusEffect, int> StatusEffects()
    {
        return cachedStatusEffects;
    }

    private void EvaluateStatusEffects()
    {
        var evaluatedStatusEffects = new Dictionary<StatusEffect, int>();
        foreach (var kvp in baseStats.GetBaseStatusEffects())
        {
            var query = new StatusEffectQueryKey(kvp.Key);
            var queryContext = new QueryContext<StatusEffectQueryKey>(query,
                kvp.Value,
                kvp.Value);
            StatusEffectMediator.PerformQuery(this, queryContext);
            evaluatedStatusEffects[kvp.Key] = queryContext.CurrentValue;
        }
        
        cachedStatusEffects = evaluatedStatusEffects;
    }
    
    public EvaluatedStats(BaseStats baseStats, Func<int> getLevel, Mediator<StatQueryKey> statMediator = null,
        Mediator<StatusEffectQueryKey> statusEffectMediator = null)
    {
        GetLevel = getLevel ?? (() => 1);
        this.baseStats = baseStats;
        StatMediator = statMediator ?? new Mediator<StatQueryKey>();
        StatusEffectMediator = statusEffectMediator ?? new Mediator<StatusEffectQueryKey>();
        
        StatMediator.OnModifiersChanged += EvaluateStats;
        StatusEffectMediator.OnModifiersChanged += EvaluateStatusEffects;
        
        EvaluateStats();
        EvaluateStatusEffects();
    }
    
    public int GetInnateStat(InnateStat innateStat)
    {
        var stats = Stats();
        return stats.GetValueOrDefault(innateStat, 0);
    }
    
    /**
     * <summary>
     * Calculates the total multiplier for a given status effect target by combining the multipliers of all
     * relevant status effects.
     * </summary>
     *
     * <param name="statusEffectTarget">The target type of the status effects to consider.</param>
     * <returns>The combined multiplier for the specified status effect target.</returns>
     */
    public float GetStatusEffectMultiplier(StatusEffectTargets statusEffectTarget)
    {
        var statusEffects = StatusEffects();
        
        float totalMultiplier = 1;
        
        foreach (var kvp in statusEffects)
        {
            if (kvp.Key.Target == statusEffectTarget)
            {
                totalMultiplier *= kvp.Key.CalculateMultiplier(kvp.Value);
            }
        }
        
        return totalMultiplier;
    }

    public void Update()
    {
        StatMediator.Update();
        StatusEffectMediator.Update();
    }

    public override string ToString()
    {
        var stats = Stats();
        var statStrings = new List<string>();
        foreach (var kvp in stats)
        {
            statStrings.Add($"({kvp.Key} Value: {kvp.Value})");
        }
        
        var statusEffects = StatusEffects();
        var statusEffectStrings = new List<string>();
        foreach (var kvp in statusEffects)
        {
            statusEffectStrings.Add($"({kvp.Key} Stacks: {kvp.Value})");
        }
        
        return $"Stats: {string.Join(", ", statStrings)} | Status Effects: {string.Join(", ", statusEffectStrings)}";
    }
}

public enum ChangeType
{
    Flat,
    AdditivePercent,
    MultiplicativePercent,
}
