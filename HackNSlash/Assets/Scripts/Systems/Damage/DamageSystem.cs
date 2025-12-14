using System;
using System.Linq;
using Extensions.Patterns;
using UnityEngine;

public class DamageSystem : MonoBehaviour, IService
{
    [Header("Damage Settings")]
    
    private readonly RuleSystem<DamageContext, DamageResult> _baseRuleSystem = new RuleSystem<DamageContext, DamageResult>();
    private IRule<IDamageEvent, DamageContext, DamageResult> _finalizeDamageRule = new FinalizeDamageRule();
    
    private static DamageResult Combine(DamageResult a, DamageResult b) => a + b;

    private void Awake()
    {
        InitializeBaseRules();
    }
    
    private void InitializeBaseRules()
    {
        // Add other base rules as needed
        
        _finalizeDamageRule = new FinalizeDamageRule();
    }
    
    public DamageResult ResolveDamage(IDamageAgent attacker, IDamageAgent defender, IDamageEvent damageEvent)
    {
        var context = new DamageContext(attacker, defender, damageEvent.BasePower);
        var additionalRules = attacker.DamageEvalRules.Concat(defender.DamageEvalRules);
        
        var result = _baseRuleSystem.ApplyRules(damageEvent, context, Combine, 
            damageEvent.GetFirstRule(), additionalRules, _finalizeDamageRule);
        
        
        return result;
    }
}