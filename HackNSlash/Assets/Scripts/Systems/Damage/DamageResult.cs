
/**
 * <summary>
 * Result of a damage calculation, including base damage, final damage, and whether it was a critical hit.
 * </summary>
 */
public struct DamageResult
{
    public float BaseDamage;
    public float FinalDamage;
    public bool IsCritical;

    public DamageResult(float damage)
    {
        BaseDamage = damage;
        FinalDamage = damage;
        IsCritical = false;
    }
    
    public DamageResult(float baseDamage, float finalDamage, bool isCritical)
    {
        BaseDamage = baseDamage;
        FinalDamage = finalDamage;
        IsCritical = isCritical;
    }
    
    public override string ToString()
    {
        return $"DamageResult(BaseDamage: {BaseDamage}, FinalDamage: {FinalDamage}, IsCritical: {IsCritical})";
    }
    
    public static DamageResult operator +(DamageResult a, DamageResult b)
    {
        // return b if b is not default, otherwise a
        
        if (b.FinalDamage != 0f || b.IsCritical)
        {
            return b;
        }
        
        return a;
    }
    
    public static DamageResult Empty => new DamageResult
    {
        BaseDamage = 0f,
        FinalDamage = 0f,
        IsCritical = false
    };
}