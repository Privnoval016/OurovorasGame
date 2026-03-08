public class ShieldDamageReductionRule : IDamageRule
{
    private float _reductionMultiplier;
    
    public ShieldDamageReductionRule(float reductionMultiplier)
    {
        _reductionMultiplier = reductionMultiplier;
    }
    
    public override DamageResult Apply(IDamageEvent evt, DamageContext ctx)
    {
        if (ctx.Defender.DamageableComponents.TryGetComponent(out ShieldComponent shield) && shield.CurrentShieldPercentage > 0)
        {
            ctx.Multiplicative *= _reductionMultiplier;
        }
        
        return default;
    }
    
    public override bool IsMatch(IDamageEvent eventData, DamageContext context)
        => context.Defender.DamageableComponents.TryGetComponent(out ShieldComponent shield) && shield.CurrentShieldPercentage > 0;
}