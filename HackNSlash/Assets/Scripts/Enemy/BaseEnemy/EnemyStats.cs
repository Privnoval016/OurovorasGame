using System.Collections.Generic;
using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    
    public Dictionary<Stat, float> Stats = new();
    public float currentHealth;
    
    public ElementEffect currentElementEffect = ElementEffect.None;
    
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
    
    public virtual void TakeDamage(ElementEffect element, PlayerController pc, Attack a)
    {
        
        Debug.Log($"{gameObject.name} took {a.name} attack from {pc.gameObject.name} with element {element}.");
        
        // Override this method to implement damage logic
    }
    
    #endregion
}
