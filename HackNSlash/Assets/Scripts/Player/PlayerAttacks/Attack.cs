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
    
    [Space(5)] 
    
    public bool isLockedOn = false;
    
    public KeyBind[] keyBinds;
    public Vector2 inputDirection;
    public bool applyTargetDirection = true;
    public NBool isMidair = NBool.False;
    public bool applyRootMotion = true;
    
    [Header("Animation")]
    
    public TransitionAsset[] attackTransitions;
    public AnimationClip[] attackClips;

    public int clipsToPlay = 1;
    
    public ExitConditions exitCondition = ExitConditions.AnimationEnd;

    [FormerlySerializedAs("onAttackMethod")] [Header("Events")] 
    public OnAttackActions onAttackAction = OnAttackActions.None;
    
    
    public HitInfo hitInfo;
    
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
    public OnHitActions onHitAction = OnHitActions.BasicKnockBack;
    
    [Header ("Hit Detection")]
    public HitDetections hitDetection;
    public float hitRegisterRadius = 3;
    public float hitRegisterAngle = 120;
    
    [Header("Stats")]
    public float attackCoolDown;
    public float knockBackForce;
    public float damage;
}
