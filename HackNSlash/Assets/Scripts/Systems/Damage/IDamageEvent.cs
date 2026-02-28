using Extensions.Patterns;

public interface IDamageEvent
{
    float BasePower { get; }
    float BaseShieldDamage { get; }
    IRule<IDamageEvent, DamageContext, DamageResult> GetFirstRule();
}

/**
 * <summary>
 * Damage event originating from a player attack.
 * </summary>
 */
public struct PlayerDamageEvent : IDamageEvent
{
    public float BasePower { get; }
    public float BaseShieldDamage { get; }
    
    public PlayerDamageEvent(Attack attack)
    {
        BasePower = attack.stats.damage;
        BaseShieldDamage = attack.stats.shieldDamage;
    }
    
    public IRule<IDamageEvent, DamageContext, DamageResult> GetFirstRule()
    {
        return new BaseDamageRule();
    }
}

/**
 * <summary>
 * Damage event originating from an enemy attack.
 * </summary>
 */
public struct EnemyDamageEvent : IDamageEvent
{
    public float BasePower { get; }
    public float BaseShieldDamage => 0f; // Enemies don't deal shield damage

    public EnemyDamageEvent(HitInstance hitInstance)
    {
        BasePower = hitInstance.Damage;
    }
    
    public IRule<IDamageEvent, DamageContext, DamageResult> GetFirstRule()
    {
        return new BaseDamageRule();
    }
}

/**
 * <summary>
 * Damage event originating from a dynamic source (DOT, shared damage, etc).
 * </summary>
 */
public struct DynamicDamageEvent : IDamageEvent
{
    public float BasePower { get; }
    public float BaseShieldDamage { get; }
    
    public DynamicDamageEvent(float baseDamage, float baseShieldDamage = 0)
    {
        BasePower = baseDamage;
        BaseShieldDamage = baseShieldDamage;
    }
    
    public IRule<IDamageEvent, DamageContext, DamageResult> GetFirstRule()
    {
        // TODO: Create a specific rule for dynamic damage like DOT that is tied to status effects
        return new BaseDamageRule();
    }
}