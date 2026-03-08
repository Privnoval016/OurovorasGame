public class StatusEffectDamageTakenRule : IDamageRule
{
    public override DamageResult Apply(IDamageEvent evt, DamageContext ctx)
    {
        // Get the damage taken multiplier from the defender's stats
        float multiplier = ctx.Defender.Stats.GetStatusEffectMultiplier(StatusEffectTargets.DamageTaken);
        
        ctx.Multiplicative *= multiplier;
        
        return default;
    }

    public override bool IsMatch(IDamageEvent eventData, DamageContext context)
        => context.Defender.Stats.GetStatusEffectMultiplier(StatusEffectTargets.DamageTaken) > 1f;
}

// if the target has a shield on top of their health bar, we apply a damage reduction until the shield is gone