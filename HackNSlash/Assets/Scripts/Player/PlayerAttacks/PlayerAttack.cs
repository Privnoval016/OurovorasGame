using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Extensions.Utils;
using Animancer;

[CreateAssetMenu(menuName = "Player/Attacks/PlayerAttack")]
public class PlayerAttack : Attack
{
    public OnAttackActions onAttackAction = OnAttackActions.None;
    
    [Tooltip("Case specific parameter")]
    public int attackEventIndex = 0;
    
    
    [Header("Root Motion")]
    public bool applyRootMotion = true;
    public bool moveCameraWithAttack = true;
    
    [Header("Animation")]
    
    public TransitionAsset[] attackTransitions;
    public AnimationClip[] attackClips;
    
    public float animDelay = 0;
    public float animFade = 0.2f;

    public int clipsToPlay = 1;

    public bool useNormalGravity = false;
    
    public ExitConditions exitCondition = ExitConditions.AnimationEnd;

    [Header("VFX")] 
    
    public VFXSpawnInfo[] vfxInfos;


    private void OnValidate()
    {
        if (hitInfo.onHitActions.Length == 0)
        {
            hitInfo.onHitActions = new[] {OnHitActions.BasicKnockBack};
        }
        
        if (attackType == AttackTypes.Spirit)
        {
            attackType = AttackTypes.Other;
        }
    }
}

public enum ExitConditions
{
    AnimationEnd,
    Immediate,
    ExternalExit
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
    public float hitRegisterRadius = 3;
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

[Serializable]
public class VFXSpawnInfo
{
    public VFXAttack vfxAttack;
    
    [Header("VFX Parameters")]
    public int hitIndex;
    public float duration = 0.3f;
    public float delay = 0;

    [Header("Default Spawn Parameters")] 
    public Target spawnTarget = Target.None;

    public TransformInfo spawnTransform;
}

public enum Target
{
    None,
    Player,
    TargetedEnemy,
    KatanaSpirit
}

public enum AttackTypes
{
    Light,
    Heavy,
    Midair,
    Directional,
    Special,
    Other,
    Spirit
}
