public class StatusEffectDamageDealtRule : IDamageRule
{
    public override DamageResult Apply(IDamageEvent evt, DamageContext ctx)
    {
        // Get the damage dealt multiplier from the attacker's stats
        float multiplier = ctx.Attacker.Stats.GetStatusEffectMultiplier(StatusEffectTargets.DamageDealt);
        
        ctx.Multiplicative *= multiplier;
        
        return default;
    }

    public override bool IsMatch(IDamageEvent eventData, DamageContext context)
        => context.Attacker.Stats.GetStatusEffectMultiplier(StatusEffectTargets.DamageDealt) > 1f;
}