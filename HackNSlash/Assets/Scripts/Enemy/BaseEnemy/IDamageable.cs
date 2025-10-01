public interface IDamageable
{
    void TakeDamage(ElementEffect element, float damageAmount);
    
    void Heal(float healAmount);
}