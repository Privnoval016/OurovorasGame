using Extensions.Patterns;

public class VarietyRule : IRule<AttackStyleEvent, StyleContext, StyleResult>
{
    private readonly float _repeatMultiplier;
    private readonly float _freshBonus;
    
    public VarietyRule(float repeatMultiplier, float freshBonus)
    {
        _repeatMultiplier = repeatMultiplier;
        _freshBonus = freshBonus;
    }
    
    public bool IsMatch(AttackStyleEvent eventData, StyleContext context)
    {
        return eventData.Attack.attackType != AttackTypes.Other;
    }
    
    public StyleResult Apply(AttackStyleEvent eventData, StyleContext context)
    {
        context.AttackHitCounts.TryGetValue(eventData.Attack, out var count);

        float delta;
        if (count == 0)
        {
            delta = _freshBonus + eventData.BaseStyle * 0.2f;
        }
        else
        {
            delta = eventData.BaseStyle * _repeatMultiplier * (1f / (1 + count * 0.5f));
        }

        context.AttackHitCounts[eventData.Attack] = count + 1;
        context.LastAttack = eventData.Attack;

        return new StyleResult(delta);
    }
}