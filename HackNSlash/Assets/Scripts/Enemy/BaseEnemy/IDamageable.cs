using Extensions.Modifiers;

/**
 * <summary>
 * Interface for damageable entities that can take damage, heal, and apply status effects.
 * Inherits from IDamageAgent to include damage-calculation functionalities.
 * </summary>
 */
public interface IDamageable : IDamageAgent
{
    
    public void ApplyStatusEffect(Modifier<StatusEffectQueryKey> statusEffectModifier);
    
    public void TakeDamage(ElementEffect element, float damageAmount);
    
    public void Heal(float healAmount);
}