using System;
using Animancer;
using UnityEngine;

[Serializable]
public class EnemyAttack
{
    [Header("General")] 
    public bool isEnabled;
    public ElementEffect element;

    [Header("Attack Properties")] 
    public float damage;
    public Vector2 attackKnockback;
    public float attackCooldown;
    public bool isParryable;

    [Header("Animations")] 
    public ClipTransition[] attackClips;

    public bool useRootMotion;

    [Header("VFX")] 
    
    public VFXSpawnInfo[] vfxInfos;

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
