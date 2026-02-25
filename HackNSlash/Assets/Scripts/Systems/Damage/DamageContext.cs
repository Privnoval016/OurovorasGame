using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/**
 * <summary>
 * Stores the states and modifiers relevant to a damage calculation.
 * Damage formula is applied in stages, with each stage potentially modifying
 * the context before passing it to the next stage.
 * </summary>
 */
public class DamageContext
{
    
    // Immutable Properties
    public readonly float BasePower;
    public readonly IDamageable Attacker;
    public readonly IDamageable Defender;
    public readonly Dictionary<InnateStat, int> AttackerStatSnapshot;
    public readonly int AttackerLevel;
    public readonly Dictionary<InnateStat, int> DefenderStatSnapshot;
    public readonly int DefenderLevel;
    
    // Damage Accumulation
    public float FlatBonus;
    public float AdditivePercent;
    public float Multiplicative = 1f;

    // Critical Hits
    public bool IsCritical;
    public float CritMultiplier = 1f;

    // Base Damage
    public float BaseDamage;
    


    public DamageContext(IDamageable attacker, IDamageable defender, float basePower)
    {
        Attacker = attacker;
        Defender = defender;
        BasePower = basePower;
        
        // Snapshot relevant stats at the time of damage calculation
        AttackerStatSnapshot = new Dictionary<InnateStat, int>(attacker.Stats.Stats());
        DefenderStatSnapshot = new Dictionary<InnateStat, int>(defender.Stats.Stats());
        
        AttackerLevel = attacker.Level;
        DefenderLevel = defender.Level;
    }
}