using System;
using Extensions.Patterns;

public class DecayRule : IRule<TickStyleEvent, StyleContext, StyleResult>
{
    private readonly Func<float, float> _decayCurve; // maps current style -> decay rate

    public DecayRule(Func<float, float> decayCurve)
    {
        _decayCurve = decayCurve;
    }

    public bool IsMatch(TickStyleEvent eventData, StyleContext context) => eventData.DeltaTime > 0f;

    public StyleResult Apply(TickStyleEvent eventData, StyleContext context)
    {
        float decayRate = _decayCurve(context.CurrentStyle);
        float amount = -decayRate * eventData.DeltaTime;
        return new StyleResult(amount);
    }
}