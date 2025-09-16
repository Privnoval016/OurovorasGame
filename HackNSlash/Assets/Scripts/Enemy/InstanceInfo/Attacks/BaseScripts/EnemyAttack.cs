using System;
using Animancer;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttack", menuName = "Enemy/EnemyAttack", order = 1)]
public class EnemyAttack : ScriptableObject
{
    [Header("General")] 
    public bool isEnabled;
    public ElementEffect element;

    [Header("Attack Properties")] 
    public float damage;
    public Vector2 attackKnockback;
    public float attackCooldown;
    public bool isParryable;
    
    [Header("Attack Events")]
    
    public OnEnemyActions attackAction;
    [Tooltip("Case specific index, used for different events")] public int attackEventIndex;

    [Header("Animations")] 
    public ClipTransition[] attackClips;

    public bool useRootMotion;

    [Header("VFX")] 
    
    public VFXSpawnInfo[] vfxInfos;

}

[Serializable]
public class EnemyAttackTriggerInfo
{
    public float distanceToTrigger;
    public float angleToTrigger;
    public float attackWeight;
}

[Serializable]
public class EnemyAttackInfo
{
    public EnemyAttack attack;
    public EnemyAttackDamageInfo damageInfo;
    public EnemyAttackTriggerInfo triggerInfo;
}

[Serializable]
public class EnemyAttackDamageInfo
{
    public float damageMultiplier = 1f;
}
