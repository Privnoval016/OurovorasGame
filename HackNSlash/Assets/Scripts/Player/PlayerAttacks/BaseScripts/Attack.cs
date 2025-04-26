using System.Collections.Generic;
using Extensions.Utils;
using UnityEngine;

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
    //public OnSpiritActions onSpiritAction = OnSpiritActions.Follow;
    public HitInfo hitInfo;
}
