using System;
using System.Collections.Generic;
using ExtensionUtils;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/AttackConfig")]
public class AttackConfig : ScriptableObject
{
    [Header("Unlock Parameters")] 
    
    public bool doubleJumpEnabled;

    public float dodgeCoolDown = 1.2f;
    
    [Header("Combo Parameters")]
    
    public float comboResetTime = 1.5f;
    
    public ComboConfig[] comboAttacks;
    
    
    [Header("Regular Attack Parameters")]
    
    public float moveInterruptBuffer = 0.3f;
    
    public Attack[] lightComboAttacks; // priority 3
    public Attack[] heavyComboAttacks; // priority 3
    
    public Attack[] midairAttacks; // priority 1


    [Header("Special Attack Parameters")] 
    
    public Attack[] dodgeAttacks;
    
    public Attack[] enemyStepAttacks;

    public Attack[] directionalAttacks;
    
    public Attack[] specialAttacks; // priority 2
}

