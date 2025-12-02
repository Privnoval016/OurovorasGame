using System;
using Extensions.Patterns;

public class HitTakenPenaltyRule : IRule<HitStyleEvent, StyleContext, StyleResult>
{
    private readonly float _scale;
    private readonly Func<StyleLevel, float> _penaltyByLevel;

    public HitTakenPenaltyRule(float scale = 1f, Func<StyleLevel, float> penaltyByLevel = null)
    {
        _scale = scale;
        _penaltyByLevel = penaltyByLevel ?? (level => level switch
        {
            StyleLevel.D => 1f,
            StyleLevel.C => 1f,
            StyleLevel.B => 1.5f,
            StyleLevel.A => 1.5f,
            StyleLevel.S => 1.5f,
            StyleLevel.SS => 2f,
            StyleLevel.SSS => 2f,
            StyleLevel.X => 2f,
            _ => 1f
        });
    }
    public bool IsMatch(HitStyleEvent eventData, StyleContext context) => eventData.DamageTaken > 0f;

    public StyleResult Apply(HitStyleEvent eventData, StyleContext context)
    {
        float penalty = -eventData.DamageTaken * _scale;
        penalty *= _penaltyByLevel(context.GetLevel());
        return new StyleResult(penalty, true);
    }
}