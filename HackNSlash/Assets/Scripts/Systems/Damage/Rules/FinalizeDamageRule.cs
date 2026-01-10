using Extensions.Patterns;
using UnityEngine;

/**
 * <summary>
 * Finalizes the damage calculation by applying all modifiers to compute the final damage.
 * Only one instance of this rule should be used at the end of the damage calculation chain
 * to set the damage result.
 * </summary>
 */
public class FinalizeDamageRule : IDamageRule
{
    public override bool IsMatch(IDamageEvent evt, DamageContext ctx) => true;

    public override DamageResult Apply(IDamageEvent evt, DamageContext ctx)
    {
        float final =
            (ctx.BaseDamage + ctx.FlatBonus)
            * (1f + ctx.AdditivePercent)
            * ctx.CritMultiplier
            * ctx.Multiplicative;

        final = Mathf.Max(1f, final);
        
        return new DamageResult
        {
            BaseDamage = ctx.BaseDamage,
            FinalDamage = final,
            IsCritical = ctx.IsCritical
        };
    }
}