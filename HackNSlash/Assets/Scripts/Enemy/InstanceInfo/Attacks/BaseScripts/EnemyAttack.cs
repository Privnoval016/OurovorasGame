using System;
using Animancer;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class EnemyAttack
{
    [Header("General")] 
    public bool isEnabled;
    public ElementEffect element;
    public bool isInterruptible = false;
    
    [Header("Attack Properties")]
    public Vector2 attackKnockback;
    public float attackCooldown;
    public bool isParryable;

    [FormerlySerializedAs("enemyAction")]
    [Header("Attack Events")] 
    [SerializeReference] public IEnemyAttackStrategy enemyAttack;

    [Header("Animations")] 
    public ClipTransition[] attackClips;

    public float animDelay = 0f;

    public bool useRootMotion;
    
    public ExitConditions exitConditions = ExitConditions.AnimationEnd;

    [Header("VFX")] 
    
    public VFXSpawnInfo[] vfxInfos;
}


[Serializable]
public class EnemyAttackDamageInfo
{
    public float damageMultiplier = 1f;
}
