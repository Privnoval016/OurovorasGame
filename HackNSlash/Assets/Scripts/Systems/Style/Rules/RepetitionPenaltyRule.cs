using Extensions.Patterns;

public class RepetitionPenaltyRule : IRule<AttackStyleEvent, StyleContext, StyleResult>
{
    private readonly float _penalty;

    public RepetitionPenaltyRule(float penalty = -5f) => _penalty = penalty;

    public bool IsMatch(AttackStyleEvent eventData, StyleContext context)
        => context.LastAttack == eventData.Attack && context.AttackHitCounts.TryGetValue(eventData.Attack, out var c) && c > 1;

    public StyleResult Apply(AttackStyleEvent evt, StyleContext ctx) => new StyleResult(_penalty);
}