using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "ScriptableObjects/Attack")]
public class Attack : ScriptableObject
{
    [Header("Requirements")]
    public bool isEnabled;

    [Space(5)] 
    
    public bool isLockedOn = false;
    
    public KeyBind[] keyBinds;
    public Vector2 inputDirection;
    public bool applyTargetDirection = true;
    public bool isMidair;
    public bool applyRootMotion = true;
    
    [Header("Animation")]
    
    public AnimationClip[] attackClips;

    public string attackNameToHash;

    public bool playFirstClipOnly;
    
    public ExitConditions exitCondition = ExitConditions.AnimationEnd;

    [FormerlySerializedAs("onAttackMethod")] [Header("Events")] 
    public OnAttackActions onAttackAction = OnAttackActions.None;
    
    [Header("Stats")]
    
    public float damage;
    
    public float attackCoolDown;
}

public enum ExitConditions
{
    AnimationEnd,
    AttackRelease
}
