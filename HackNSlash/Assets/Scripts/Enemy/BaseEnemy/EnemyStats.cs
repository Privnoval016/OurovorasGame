using System;
using System.Collections.Generic;
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
        currentHealth = EvaluatedStats.GetStat(InnateStat.MaxHealth);
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
    
    #endregion
}
