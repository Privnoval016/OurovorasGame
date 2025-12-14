using Extensions.Patterns;

public abstract class IDamageRule : IRule<IDamageEvent, DamageContext, DamageResult>
{
    public DamagePhase Phase { get; }
    public abstract DamageResult Apply(IDamageEvent input, DamageContext context);
    public abstract bool IsMatch(IDamageEvent eventData, DamageContext context);
}

public enum DamagePhase
{
    PreCalculation,
    AdditiveModifiers,
    MultiplicativeModifiers,
    DefensiveModifiers,
    Finalization
}