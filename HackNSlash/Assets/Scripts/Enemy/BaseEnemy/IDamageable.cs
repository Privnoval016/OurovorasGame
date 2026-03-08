using System.Collections.Generic;
using Extensions.EntityComponent;
using Extensions.Modifiers;

/**
 * <summary>
 * Interface for damageable entities that can take damage, heal, and apply status effects.
 * Inherits from IDamageAgent to include damage-calculation functionalities.
 * </summary>
 */
public interface IDamageable
{
    public Entity<IDamageableComponent> DamageableComponents { get; }
    
    public float CurrentHealth { get; }
    public int Level { get; }
    public int NumHealthBars { get; }
    
    public void ApplyStatusEffect(Modifier<StatusEffectQueryKey> statusEffectModifier);
    
    public void TakeDamage(ElementEffect element, IDamageable attacker, IDamageEvent damageEvent);
    
    public void Heal(float healAmount);
    
    EvaluatedStats Stats { get; }
    
    IEnumerable<IDamageRule> DamageEvalRules { get; }
}