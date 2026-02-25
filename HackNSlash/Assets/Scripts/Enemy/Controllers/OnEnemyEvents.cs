using System;
using Extensions.EventBus;
using Extensions.UtilityAI;
using Extensions.UtilityAI.ConsiderationBases;
using UnityEngine;


// handles events that are triggered by the enemy through attacks, like projectiles, dashes, etc.
public class OnEnemyEvents : MonoBehaviour
{
    [HideInInspector] public EnemyController ts;

    private void Awake()
    {
        ts = GetComponent<EnemyController>();
    
    }

    public void TriggerOnEnemyAction(EnemyAttackAIAction a)
    {
        IEnemyAttackStrategy strategy = a?.attack?.enemyAttack ?? new NoneEnemyAttackStrategy();
        
        strategy.Execute(this, a);
    }
}

/**
* <summary>
* Keys used to define the target of an enemy action.
* </summary>
*/
[AIContextKey]
public enum EnemyAIContextKey
{
    [InspectorName("Player (Target)")] Player,
    [InspectorName("Other Enemy (Target)")] OtherEnemy,
    [InspectorName("Self (Target)")] Self,
    [InspectorName("Self Health (Value)")] SelfHealth,
}

/**
* <summary>
* Implementation of IConsiderationKey for EnemyActionKey, allowing it to be used in the AI utility system
* through the Unity Inspector.
* </summary>
*/
[Serializable]
public class EnemyConsiderationKey : ConsiderationKey
{
    public EnemyAIContextKey AIContextKey;

    public override object GetKey(out Type enumType)
    {
        enumType = typeof(EnemyAIContextKey);
        return AIContextKey;
    }
}
