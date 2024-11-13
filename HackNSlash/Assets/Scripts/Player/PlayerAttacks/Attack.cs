using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using ExtensionUtils;

[CreateAssetMenu(menuName = "Player/Attack")]
public class Attack : ScriptableObject
{
    [Header("Requirements")]
    public bool isEnabled;

    [Space(5)] 
    
    public bool isLockedOn = false;
    
    public KeyBind[] keyBinds;
    public Vector2 inputDirection;
    public bool applyTargetDirection = true;
    public NBool isMidair = NBool.False;
    public bool applyRootMotion = true;
    
    [Header("Animation")]
    
    public AnimationClip[] attackClips;
    

    public bool playFirstClipOnly;
    
    public ExitConditions exitCondition = ExitConditions.AnimationEnd;

    [FormerlySerializedAs("onAttackMethod")] [Header("Events")] 
    public OnAttackActions onAttackAction = OnAttackActions.None;
    
    public OnHitActions onHitAction = OnHitActions.BasicKnockBack;
    
    [Header("Stats")]
    
    public float damage;

    public float knockBackForce;
    
    public float attackCoolDown;
}

public enum ExitConditions
{
    AnimationEnd,
    AttackRelease
}
