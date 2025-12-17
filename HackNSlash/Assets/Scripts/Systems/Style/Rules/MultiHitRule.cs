using System;
using Extensions.Patterns;

public class MultiHitRule : IRule<AttackStyleEvent, StyleContext, StyleResult>
{
    private readonly Func<int, float> _hitMultiplier;

    public MultiHitRule(Func<int, float> hitMultiplier) => _hitMultiplier = hitMultiplier;

    public bool IsMatch(AttackStyleEvent eventData, StyleContext context) => eventData.EnemiesHit > 1;

    public StyleResult Apply(AttackStyleEvent eventData, StyleContext context)
    {
        float extra = eventData.BaseStyle * (_hitMultiplier(eventData.EnemiesHit) - 1f);
        return new StyleResult(extra);
    }
}