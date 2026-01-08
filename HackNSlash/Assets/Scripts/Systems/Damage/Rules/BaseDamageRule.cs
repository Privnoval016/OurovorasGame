using Extensions.Patterns;

/**
 * <summary>
 * Applies the base damage formula to calculate the initial damage before modifiers.
 * Ran before any other damage rules.
 * </summary>
 */
public class BaseDamageRule : IDamageRule
{
    public override DamageResult Apply(IDamageEvent evt, DamageContext ctx)
    {
        float baseDamage =
            (2f * ctx.AttackerStatSnapshot[InnateStat.Strength] / 5f + 2f)
                * ctx.BasePower
                * ctx.AttackerLevel / 
                ctx.DefenderStatSnapshot[InnateStat.Defense] / 50f + 2f;

        ctx.BaseDamage = baseDamage;
        return default;
    }
    
    public override bool IsMatch(IDamageEvent eventData, DamageContext context) => true;
}