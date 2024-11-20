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
    
    [Header("Regular Attack Parameters")]
    
    public Attack[] lightComboAttacks; // priority 3
    public Attack[] heavyComboAttacks; // priority 3
    
    public Attack[] midairAttacks; // priority 1


    [Header("Special Attack Parameters")] 
    
    public Attack[] dodgeAttacks;
    
    public Attack[] specialAttacks; // priority 2
    
    public Attack[] holdAttacks; // priority 4
    
    [HideInInspector] public Dictionary<BasicAttackTypes, Attack> BasicToHoldAttackMap = new();

    private void OnValidate()
    {
        BasicToHoldAttackMap = new();
        
        foreach (var attack in holdAttacks)
        {
            if (attack.isMidair.IsTrue()) BasicToHoldAttackMap.Add(BasicAttackTypes.MidairAttack, attack);
            else if (attack.keyBinds[0] == KeyBind.LightAttackHold) BasicToHoldAttackMap.Add(BasicAttackTypes.LightAttack, attack);
            else if (attack.keyBinds[0] == KeyBind.HeavyAttackHold) BasicToHoldAttackMap.Add(BasicAttackTypes.HeavyAttack, attack);
        }
    }
}

