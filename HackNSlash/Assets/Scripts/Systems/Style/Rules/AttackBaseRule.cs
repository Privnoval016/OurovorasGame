using Extensions.Patterns;

public class AttackBaseRule : IRule<AttackStyleEvent, StyleContext, StyleResult>
{
    public bool IsMatch(AttackStyleEvent eventData, StyleContext context)
    {
        return eventData.Attack.attackType != AttackTypes.Other;
    }
    
    public StyleResult Apply(AttackStyleEvent eventData, StyleContext context)
    {
        return new StyleResult(eventData.BaseStyle);
    }
}