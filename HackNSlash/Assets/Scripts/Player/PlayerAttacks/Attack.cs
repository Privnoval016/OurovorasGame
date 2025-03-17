using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Extensions.Utils;
using Animancer;

[CreateAssetMenu(menuName = "Player/Attack")]
public class Attack : ScriptableObject
{
    public static readonly List<AttackTypes> AttackTypePriority = new()
    {
        AttackTypes.Other,
        AttackTypes.DirectionalAttack,
        AttackTypes.SpecialAttack,
        AttackTypes.MidairAttack,
        AttackTypes.HeavyAttack,
        AttackTypes.LightAttack,
    };
    
    [Header("General")] 
    public bool isEnabled = true;
    public AttackTypes attackType;
    public ElementEffect element;
    
    [Space(5)] 
    
    public bool isLockedOn = false;
    
    public KeyBind[] keyBinds;
    public Vector2 inputDirection;
    public Vector2 comboDirection;
    public bool applyTargetDirection = true;
    public NBool isMidair = NBool.False;
    public bool applyRootMotion = true;
    [FormerlySerializedAs("applyRootMotionToCamera")] public bool moveCameraWithAttack = true;

    public int maxUses = 0;
    
    [Header("Animation")]
    
    public TransitionAsset[] attackTransitions;
    public AnimationClip[] attackClips;
    
    public float animDelay = 0;
    public float animFade = 0.2f;

    public int clipsToPlay = 1;
    
    public ExitConditions exitCondition = ExitConditions.AnimationEnd;

    [FormerlySerializedAs("onAttackMethod")] [Header("Events")] 
    public OnAttackActions onAttackAction = OnAttackActions.None;
    public int attackEventIndex = 0;
    
    
    public HitInfo hitInfo;

    [Header("VFX")] 
    
    public VFXSpawnInfo[] vfxInfos;


    private void OnValidate()
    {
        if (hitInfo.onHitActions.Length == 0)
        {
            hitInfo.onHitActions = new[] {OnHitActions.BasicKnockBack};
        }
    }
}

public enum ExitConditions
{
    AnimationEnd,
    X,
    ExternalExit
}

public enum HitDetections
{
    WeaponTrail,
    SphereCast,
}

[Serializable]
public class HitInfo
{
    public OnHitActions[] onHitActions = {OnHitActions.BasicKnockBack};
    
    [Header ("Hit Detection")]
    public HitDetections hitDetection;
    public float hitRegisterRadius = 3;
    public float hitRegisterAngle = 120;
    
    [Header("Stats")]
    public float attackCoolDown;
    public float damage;
    
    [Header("Hit Parameters")]
    
    [FormerlySerializedAs("knockBackForce")] [Tooltip("Used for knockback and other select hit actions")]
    public float hitForce;
    [FormerlySerializedAs("knockBackDelay")] [Tooltip("Used for knockback and other select hit actions")]
    public float hitDelay;

    public bool lockGravWhileDelayed = false;
    [Tooltip("Used for follow velocity and other select hit actions")]
    public Vector3 hitDirection;
    
    public bool tweenToPlayer = false;
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
    LightAttack,
    HeavyAttack,
    MidairAttack,
    DirectionalAttack,
    SpecialAttack,
    Other
}
