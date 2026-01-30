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
    public PlayerAttackStats stats = new PlayerAttackStats();
    
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
    public int[] vfxInstantSpawns;
    
    public VFXSpawnInfo[] vfxInfos;
    
    [Header("Hit Stop")]
    
    public HitStopProfile[] hitStopProfiles;
    
    [Header("Audio")]
    public AudioProfile[] audioProfiles;


    protected virtual void OnValidate()
    {
        stats.charge = Mathf.Abs(stats.charge);
    }

    public bool HasEnoughCharge(PlayerController pc)
    {
        if (!stats.restoreCharge && stats.charge > 0 && pc.ps.CurrentElementCharge < stats.charge) return false;

        return true;
    }

    public float GetChargePercentage(PlayerController pc)
    {
        if (stats.charge <= 0) return 1f;
        
        float percentage = pc.ps.CurrentElementCharge / stats.charge;
        
        return Mathf.Clamp(percentage, 0f, 1f);
    }
}

[Serializable]
public class AudioProfile
{
    /**
     * <summary>
     * When the audio event will be played.
     * </summary>
     */
    public enum PlayTime
    {
        /**
         * <summary>
         * Play the audio event when the attack is initiated.
         * </summary>
         */
        Instant
    }
    
    public AudioEvent audioEvent;
    public AudioParamValue[] parameters;
    public PlayTime playTime = PlayTime.Instant;
}

[Serializable]
public class VFXSpawnInfo
{
    public VFXAttack vfxAttack;
    
    [FormerlySerializedAs("vfxHitActionIndex")]
    [FormerlySerializedAs("vfxActionIndex")]
    [Header("VFX Parameters")]
    [Tooltip("Used for determining which hitAction to use by the player")] public int vfxPlayerActionIndex;

    [FormerlySerializedAs("vfxEventActionIndex")] [Tooltip("Used in specific cases by enemies")] public int vfxEnemyActionIndex = 0;
    public float duration = 0.3f;
    [FormerlySerializedAs("delay")] public float spawnDelay = 0;
    public float actionDelay = 0f;

    [Header("Default Spawn Parameters")] 
    public PlayerTarget spawnTarget = PlayerTarget.None;
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
                vfxPlayerActionIndex = original[i].vfxPlayerActionIndex,
                duration = original[i].duration,
                spawnDelay = original[i].spawnDelay,
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


public enum PlayerTarget
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