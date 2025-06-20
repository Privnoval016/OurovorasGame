using System;
using System.Collections.Generic;
using Extensions.Utils;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class Attack : ScriptableObject
{
    public static readonly List<AttackTypes> AttackTypePriority = new()
    {
        AttackTypes.Other,
        AttackTypes.Spirit,
        AttackTypes.Directional,
        AttackTypes.Special,
        AttackTypes.Midair,
        AttackTypes.Heavy,
        AttackTypes.Light,
        AttackTypes.Element
    };
    
    [Header("General")] 
    public bool isEnabled = true;
    public AttackTypes attackType;
    public ElementEffect element;
    
    [Space(5)] 
    
    [Header("Conditions")]
    
    public bool isLockedOn = false;
    
    public KeyBind[] keyBinds;
    public Vector2 inputDirection;
    public Vector2 comboDirection;
    public bool applyTargetDirection = true;
    
    [Space(5)]
    
    public NBool isMidair = NBool.False;
    
    public int maxUses = 0;
    
    [Header("Events")]
    public HitInfo hitInfo;
    
    [Header("VFX")] 
    
    public VFXSpawnInfo[] vfxInfos;
}

[Serializable]
public class VFXSpawnInfo
{
    public VFXAttack vfxAttack;
    
    [FormerlySerializedAs("hitIndex")] [Header("VFX Parameters")]
    public int onHitActionIndex;
    public float duration = 0.3f;
    public float delay = 0;

    [Header("Default Spawn Parameters")] 
    public Target spawnTarget = Target.None;
    public bool parentToTarget = false;

    public TransformInfo spawnTransform;
    [FormerlySerializedAs("applyParentPose")] public bool applyParentPoseToPosition = false;
}

public enum HitDetections
{
    WeaponTrail,
    SphereCast,
    HitScan
}

[Serializable]
public class HitInfo
{
    public OnHitActions[] onHitActions = {OnHitActions.BasicKnockBack};
    
    [Header ("Hit Detection")]
    public HitDetections hitDetection;
    public float lateralRadius = 3;
    public float verticalRadius = 3;
    public float hitRegisterAngle = 120;
    public int numTargets = 1;
    
    [Header("Stats")]
    public float attackCoolDown;
    public float damage;
    
    [Header("Hit Parameters")]
    
    [Tooltip("Used for knockback and other select hit actions")]
    public float hitForce;
    [Tooltip("Used for knockback and other select hit actions")]
    public float hitDelay;

    [Tooltip("Used for follow velocity and other select hit actions")]
    public Vector3 hitDirection;
    
    [Tooltip("Used for follow velocity and other select hit actions")]
    public bool elasticCollision = false;
}



public enum Target
{
    None,
    Player,
    TargetedEnemy,
    KatanaSpirit,
    EnemyWithOffset
}

public enum AttackTypes
{
    Light,
    Heavy,
    Midair,
    Directional,
    Special,
    Other,
    Spirit,
    Element
}