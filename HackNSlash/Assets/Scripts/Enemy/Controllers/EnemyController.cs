using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Components")]
    
    [HideInInspector] public LockOnTarget lot;
    
    public PhysicsNavigator nav;

    [HideInInspector] public EnemyAnimator ea;

    [HideInInspector] public PlayerController pc;

    public EnemyAnimListener animListener;
    
    public EnemyHitbox[] attackHitboxes;
    
    [Header("Enemy Info")]
    
    public EnemyAttackConfig attackConfig;
    
    #region Attack Properties
    
    [HideInInspector] public EnemyAttack currentAttack;
    [HideInInspector] public bool parryWindowActive = false;

    #endregion
    
    [Header("Stats")]
    
    public Dictionary<Stat, float> Stats = new();
    public float currentHealth;
    
    public ElementEffect currentElementEffect = ElementEffect.None;
    
    #region Monobehaviour Callbacks

    private void Awake()
    {
        nav = GetComponent<PhysicsNavigator>();
        ea = GetComponent<EnemyAnimator>();
        lot = GetComponent<LockOnTarget>();
        
        animListener.ts = this;
        
        foreach (var hitbox in attackHitboxes)
        {
            hitbox.ts = this;
        }
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
    
    public virtual void TakeDamage(ElementEffect element, PlayerController pc, Attack a)
    {
        
        Debug.Log($"{gameObject.name} took {a.name} attack from {pc.gameObject.name} with element {element}.");
        
        // Override this method to implement damage logic
    }
    
    #endregion
}
