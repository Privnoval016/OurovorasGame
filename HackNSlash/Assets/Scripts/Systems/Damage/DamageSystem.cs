using System;
using System.Linq;
using Extensions.Patterns;
using UnityEngine;

public class DamageSystem : MonoBehaviour, IService
{
    [Header("Damage Settings")]
    
    private readonly RuleSystem<DamageContext, DamageResult> _baseRuleSystem = new RuleSystem<DamageContext, DamageResult>();
    private IDamageRule _finalizeDamageRule = new FinalizeDamageRule();
    
    private static DamageResult Combine(DamageResult a, DamageResult b) => a + b;

    private void Awake()
    {
        InitializeBaseRules();
    }
    
    private void InitializeBaseRules()
    {
        // Add other base rules as needed
        _baseRuleSystem.AddRule(new StatusEffectDamageDealtRule());
        _baseRuleSystem.AddRule(new StatusEffectDamageTakenRule());
        
        // Finalize damage rule should be the last rule applied
        _finalizeDamageRule = new FinalizeDamageRule();
    }
    
    public DamageResult ResolveDamage(IDamageable attacker, IDamageable defender, IDamageEvent damageEvent)
    {
        if (damageEvent == null)
        {
            Debug.LogWarning("DamageEvent is null. Cannot process damage.");
            return DamageResult.Empty;
        }
        
        if (attacker == null || defender == null)
        {
            Debug.LogWarning("Attacker or Defender is null. Cannot process damage.");
            return new DamageResult(damageEvent.BasePower);
        }
        
        var context = new DamageContext(attacker, defender, damageEvent.BasePower);
        var additionalRules = attacker.DamageEvalRules.Concat(defender.DamageEvalRules);
        
        var result = _baseRuleSystem.ApplyRules(damageEvent, context, Combine, 
            damageEvent.GetFirstRule(), additionalRules, _finalizeDamageRule);
        
        
        return result;
    }
}