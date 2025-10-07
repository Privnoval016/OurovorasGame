public interface IDamageable
{
    public void ApplyStatusEffect(Modifier<StatusEffectQueryKey> statusEffectModifier);
    
    public void TakeDamage(ElementEffect element, float damageAmount);
    
    public void Heal(float healAmount);
}