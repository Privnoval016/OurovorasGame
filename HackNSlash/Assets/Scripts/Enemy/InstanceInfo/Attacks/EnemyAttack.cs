using System;
using Animancer;
using UnityEngine;

[Serializable]
public struct EnemyAttack
{
    [Header("General")] 
    public bool isEnabled;
    public ElementEffect element;

    [Header("Attack Properties")] 
    public float attackKnockback;
    public float attackCooldown;
    public Vector3 knockbackDirection;

    [Header("Animations")] 
    public ClipTransition[] attackClips;
    
    //[Header("VFX")] make at some point
    
}

[Serializable]
public struct EnemyAttackTriggerInfo
{
    public float distanceToTrigger;
    public float angleToTrigger;
    public float attackWeight;
}

[Serializable]
public struct EnemyAttackInfo
{
    public EnemyAttack attack;
    public EnemyAttackTriggerInfo triggerInfo;
}
