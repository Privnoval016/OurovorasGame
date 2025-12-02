using Extensions.Patterns;

public class AerialRule : IRule<AttackStyleEvent, StyleContext, StyleResult>
{
    private readonly float _aerialBonus;

    public AerialRule(float aerialBonus = 2f) { _aerialBonus = aerialBonus; }

    public bool IsMatch(AttackStyleEvent eventData, StyleContext context) => eventData.IsAerial;

    public StyleResult Apply(AttackStyleEvent eventData, StyleContext context) => new StyleResult(_aerialBonus);
}