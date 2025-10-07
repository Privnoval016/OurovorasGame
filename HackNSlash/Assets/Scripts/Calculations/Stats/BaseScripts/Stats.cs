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

public enum StatusEffectTargets
{
    None,
    Speed,          // affects move speed
    DamageDealt,    // affects damage dealt to others
    DamageTaken,    // affects damage taken from others
    DynamicDamage   // affects damage taken during updates/not necessarily just when hit (shared damage, DOT, etc)
}

public class EvaluatedStats
{
    private readonly BaseStats baseStats;
    
    public readonly Mediator<StatQueryKey> StatMediator;
    public readonly Mediator<StatusEffectQueryKey> StatusEffectMediator;

    public Dictionary<InnateStat, int> Stats()
    {
        var evaluatedStats = new Dictionary<InnateStat, int>();
        foreach (var kvp in baseStats.Stats)
        {
            var query = new StatQueryKey(kvp.Key);
            var queryContext = new QueryContext<StatQueryKey>(query,
                kvp.Value,
                kvp.Value);
            StatMediator.PerformQuery(this, queryContext);
            evaluatedStats[kvp.Key] = queryContext.CurrentValue;
        }
        return evaluatedStats;
    }
    
    public Dictionary<StatusEffect, int> StatusEffects()
    {
        var evaluatedStatusEffects = new Dictionary<StatusEffect, int>();
        foreach (var kvp in baseStats.StatusEffects)
        {
            var query = new StatusEffectQueryKey(kvp.Key);
            var queryContext = new QueryContext<StatusEffectQueryKey>(query,
                kvp.Value,
                kvp.Value);
            StatusEffectMediator.PerformQuery(this, queryContext);
            evaluatedStatusEffects[kvp.Key] = queryContext.CurrentValue;
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
