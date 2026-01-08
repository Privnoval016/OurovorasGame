using System.Collections.Generic;
using Extensions.Modifiers;
using Extensions.Patterns;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    
    [field: SerializeField] public int Level { get; private set; } = 5;
    [field: SerializeField] public int NumHealthBars { get; private set; } = 1;
    
    public BaseStats baseStats;

    public EvaluatedStats EvaluatedStats;
    [ReadOnly] public float CurrentHealth { get; private set; }

    public ElementEffect currentElementEffect = ElementEffect.None;

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        EvaluatedStats = new EvaluatedStats(baseStats, () => Level);
        CurrentHealth = EvaluatedStats.GetInnateStat(InnateStat.MaxHealth);
    }

    private void Update()
    {
        EvaluatedStats.Update();
    }

    #endregion

    #region Element Methods

    public ElementEffect GetElementFromAttack(ElementEffect attackElement)
    {
        ElementEffect element = attackElement;

        if (element == ElementEffect.MatchCurrent)
        {
            element = currentElementEffect;
        }
    
        return element;
    }

    #endregion

    #region Stat Methods

    public float GetStat(InnateStat stat)
    {
        return EvaluatedStats.GetInnateStat(stat);
    }

    #endregion

    #region Damageable Methods
    
    public virtual void ApplyStatusEffect(Modifier<StatusEffectQueryKey> statusEffectModifier)
    {
        if (statusEffectModifier?.Key == null ||
            statusEffectModifier.Key.Key is NoStatusEffect) return;
        
        EvaluatedStats.StatusEffectMediator.AddModifier(statusEffectModifier);
        Debug.Log($"{gameObject.name} applied status effect {statusEffectModifier.Key.Key}");
    }

    public virtual void TakeDamage(ElementEffect element, IDamageable attacker, IDamageEvent damageEvent)
    {
        float damageAmount = Services.Get<DamageSystem>().ResolveDamage(attacker, this, damageEvent).FinalDamage;
        
        Debug.Log($"{gameObject.name} took {damageAmount} damage of element {element}");
    }

    public virtual void Heal(float healAmount)
    {
        Debug.Log($"{gameObject.name} healed {healAmount} health");
    
        // Override this method to implement healing logic
    }

    public EvaluatedStats Stats => EvaluatedStats;

    public IEnumerable<IDamageRule> DamageEvalRules
    => new List<IDamageRule>();

    #endregion
}

