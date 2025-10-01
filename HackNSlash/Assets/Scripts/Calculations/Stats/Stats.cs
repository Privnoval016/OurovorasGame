using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

public enum InnateStat : int // value based stats
{
    MaxHealth,
    MaxCharge,
    Strength,
    Defense,
}

public enum StatusEffect : int // stack based status effects
{
    Speed,
    Vulnerability,
    SharedDamage,
    DoT
}

public enum ChangeType
{
    Flat,
    AdditivePercent,
    MultiplicativePercent,
}

[Serializable]
public class StatChange
{
    
    [FormerlySerializedAs("stat")] public InnateStat innateStat;
    public float value;
    public ChangeType changeType;
}

public class EvaluatedStats
{
    readonly BaseStats baseStats;
    
    public Mediator<StatQueryKey> StatMediator = new();
    public Mediator<StatusEffectQueryKey> StatusEffectMediator = new();

    public Dictionary<InnateStat, int> Stats()
    {
        var evaluatedStats = new Dictionary<InnateStat, int>();
        foreach (var kvp in baseStats.Stats)
        {
            var query = new StatQueryKey(kvp.Key);
            var queryContext = new QueryContext<StatQueryKey>(query, kvp.Value);
            StatMediator.PerformQuery(this, queryContext);
            evaluatedStats[kvp.Key] = queryContext.Value;
        }
        return evaluatedStats;
    }
    
    public Dictionary<StatusEffect, int> StatusEffects()
    {
        var evaluatedStatusEffects = new Dictionary<StatusEffect, int>();
        foreach (var kvp in baseStats.StatusEffects)
        {
            var query = new StatusEffectQueryKey((InnateStat)kvp.Key);
            var queryContext = new QueryContext<StatusEffectQueryKey>(query, kvp.Value);
            StatusEffectMediator.PerformQuery(this, queryContext);
            evaluatedStatusEffects[kvp.Key] = queryContext.Value;
        }
        return evaluatedStatusEffects;
    }
    
    public EvaluatedStats(BaseStats baseStats, Mediator<StatQueryKey> statMediator = null,
        Mediator<StatusEffectQueryKey> statusEffectMediator = null)
    {
        this.baseStats = baseStats;
        StatMediator = statMediator ?? new Mediator<StatQueryKey>();
        StatusEffectMediator = statusEffectMediator ?? new Mediator<StatusEffectQueryKey>();
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
