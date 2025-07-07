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
    
    [Header("Linked Attacks")]
    public bool updateLinkedAttack = false;
    public Attack linkedAttack;
    
    [Header("General")] 
    public bool isEnabled = true;
    public AttackTypes attackType;
    public ElementEffect element;

    [Header("Stats")] 
    public AttackStats stats = new AttackStats();
    
    [Space(5)] 
    
    [Header("Conditions")]
    
    public bool isLockedOn = false;
    public bool requireElementTrigger = false;
    public float triggerCoolDown = 0f;
    
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
    
    [Header("Hit Stop")]
    
    public HitStopProfile[] hitStopProfiles;


    protected virtual void OnValidate()
    {
        stats.charge = Mathf.Abs(stats.charge);
        UpdateLinkedAttack();
    }

    protected virtual void UpdateLinkedAttack()
    {
        if (linkedAttack == this) linkedAttack = null;
        if (linkedAttack == null) return;
        
        linkedAttack.updateLinkedAttack = updateLinkedAttack;
        
        if (!updateLinkedAttack) return;
    
        linkedAttack.isEnabled = isEnabled;
        linkedAttack.attackType = attackType;
        linkedAttack.element = element;
        
        linkedAttack.stats = new AttackStats(stats);
        
        linkedAttack.isLockedOn = isLockedOn;
        linkedAttack.requireElementTrigger = requireElementTrigger;
        linkedAttack.triggerCoolDown = triggerCoolDown;
        
        linkedAttack.keyBinds = keyBinds;
        linkedAttack.inputDirection = inputDirection;
        linkedAttack.comboDirection = comboDirection;
        linkedAttack.applyTargetDirection = applyTargetDirection;
        
        linkedAttack.isMidair = isMidair;
        linkedAttack.maxUses = maxUses;
        linkedAttack.hitInfo = new HitInfo(hitInfo);

        linkedAttack.vfxInfos = VFXSpawnInfo.DeepCopy(vfxInfos);
        
        linkedAttack.hitStopProfiles = HitStopProfile.ShallowCopy(hitStopProfiles);
        
        linkedAttack.linkedAttack = this;
    }

    public bool HasEnoughCharge(PlayerController pc)
    {
        if (!stats.restoreCharge && stats.charge > 0 && pc.pi.currentCharge < stats.charge) return false;

        return true;
    }

    public float GetChargePercentage(PlayerController pc)
    {
        if (stats.charge <= 0) return 1f;
        
        float percentage = pc.pi.currentCharge / stats.charge;
        
        return Mathf.Clamp(percentage, 0f, 1f);
    }
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
    
    public static VFXSpawnInfo[] DeepCopy(VFXSpawnInfo[] original)
    {
        if (original == null) return null;
        
        VFXSpawnInfo[] copy = new VFXSpawnInfo[original.Length];
        for (int i = 0; i < original.Length; i++)
        {
            copy[i] = new VFXSpawnInfo
            {
                vfxAttack = original[i].vfxAttack,
                onHitActionIndex = original[i].onHitActionIndex,
                duration = original[i].duration,
                delay = original[i].delay,
                spawnTarget = original[i].spawnTarget,
                parentToTarget = original[i].parentToTarget,
                spawnTransform = original[i].spawnTransform,
                applyParentPoseToPosition = original[i].applyParentPoseToPosition
            };
        }
        return copy;
    }
}

public enum HitDetections
{
    WeaponCollider,
    SphereCast,
    HitScan,
    WeaponTrail,
    None
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
    
    [Header("Hit Parameters")]
    
    [Tooltip("Used for knockback and other select hit actions")]
    public float hitForce;
    [Tooltip("Used for knockback and other select hit actions")]
    public float hitDelay;

    [Tooltip("Used for follow velocity and other select hit actions")]
    public Vector3 hitDirection;
    
    [Tooltip("Used for follow velocity and other select hit actions")]
    public bool elasticCollision = false;

    public HitInfo()
    {
    }

    public HitInfo(HitInfo copy)
    {
        onHitActions = (OnHitActions[]) copy.onHitActions.Clone();
        hitDetection = copy.hitDetection;
        lateralRadius = copy.lateralRadius;
        verticalRadius = copy.verticalRadius;
        hitRegisterAngle = copy.hitRegisterAngle;
        numTargets = copy.numTargets;
        attackCoolDown = copy.attackCoolDown;
        hitForce = copy.hitForce;
        hitDelay = copy.hitDelay;
        hitDirection = copy.hitDirection;
        elasticCollision = copy.elasticCollision;
    }
        
}

[Serializable]
public class AttackStats
{
    public bool restoreCharge = true;
    [FormerlySerializedAs("chargeRequired")] public float charge = 0f;
    public float ultimateCharge = 8f;
    public float damage = 0f;


    public AttackStats()
    {
    }

    public AttackStats(AttackStats a)
    {
        restoreCharge = a.restoreCharge;
        charge = a.charge;
        damage = a.damage;
    }
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