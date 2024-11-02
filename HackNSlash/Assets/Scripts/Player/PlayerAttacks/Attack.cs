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
    
    public KeyBind[] keyBinds;
    public Vector2 inputDirection;
    public bool isMidair;
    public bool applyRootMotion = true;
    
    [Space(5)]
    
    public AnimationClip attackClip;

    public string attackNameToHash;


    [FormerlySerializedAs("onAttackMethod")] [Header("Events")] 
    public OnAttackActions onAttackAction = OnAttackActions.None;
    
    [Header("Stats")]
    
    public float damage;
    
    public float attackCoolDown;
}
