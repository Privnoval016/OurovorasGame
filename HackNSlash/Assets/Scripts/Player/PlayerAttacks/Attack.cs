using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using ExtensionUtils;
using Animancer;

[CreateAssetMenu(menuName = "Player/Attack")]
public class Attack : ScriptableObject
{
    [Header("General")] 
    public bool isEnabled = true;
    public AttackTypes attackType;
    public ElementEffect element;
    
    [Space(5)] 
    
    public bool isLockedOn = false;
    
    public KeyBind[] keyBinds;
    public Vector2 inputDirection;
    public bool applyTargetDirection = true;
    public NBool isMidair = NBool.False;
    public bool applyRootMotion = true;

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
    
    
    public HitInfo hitInfo;

    [Header("VFX")] 
    
    public VFXInfo[] vfxInfos;


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
    [Tooltip("Used for follow velocity and other select hit actions")]
    public Vector3 hitDirection;
    
    public bool tweenToPlayer = false;
    [Tooltip("Used for follow velocity and other select hit actions")]
    public bool elasticCollision = false;
}

[Serializable]
public class VFXInfo
{
    public VFXAttack vfxAttack;
    public float duration = 0.3f;
    public float delay = 0;
    public Quaternion rotation;
}
