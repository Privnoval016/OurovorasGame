
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
    
    
    public static DamageResult operator +(DamageResult a, DamageResult b)
    {
        // return b if b is not default, otherwise a
        
        if (b.FinalDamage != 0f || b.IsCritical)
        {
            return b;
        }
        
        return a;
    }
}