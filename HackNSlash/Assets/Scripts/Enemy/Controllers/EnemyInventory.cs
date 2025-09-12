using System.Collections.Generic;
using UnityEngine;

public class EnemyInventory : MonoBehaviour
{
    [Header("Enemy Info")]
    
    public EnemyAttackConfig attackConfig;
    
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
}
