using System.Collections.Generic;
using Extensions.Modifiers;
using Extensions.Patterns;
using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public BaseStats baseStats;

    public EvaluatedStats EvaluatedStats;
    public float currentHealth;

    public ElementEffect currentElementEffect = ElementEffect.None;

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        EvaluatedStats = new EvaluatedStats(baseStats);
        currentHealth = EvaluatedStats.GetInnateStat(InnateStat.MaxHealth);
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

    public virtual void TakeDamage(ElementEffect element, float damageAmount)
    {
    
        Debug.Log($"{gameObject.name} took {damageAmount} damage of element {element}");
    
        // Override this method to implement damage logic
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

