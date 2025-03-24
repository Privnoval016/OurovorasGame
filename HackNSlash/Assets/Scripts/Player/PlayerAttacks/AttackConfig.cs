using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/AttackConfig")]
public class AttackConfig : ScriptableObject
{
    public Dictionary<AttackTypes, Attack[]> AttackMap = new Dictionary<AttackTypes, Attack[]>();
    
    [Header("Unlock Parameters")] 
    
    public bool doubleJumpEnabled;

    public float dodgeCoolDown = 1.2f;
    
    [Header("Combo Parameters")]
    
    public float comboResetTime = 1.5f;
    
    public ComboConfig[] comboAttacks;
    
    
    [Header("Regular Attack Parameters")]
    
    public float moveInterruptBuffer = 0.3f;
    
    [SerializeField] private Attack[] lightComboAttacks; // priority 3
    [SerializeField] private Attack[] heavyComboAttacks; // priority 3
    
    [SerializeField] private Attack[] midairAttacks; // priority 1


    [Header("Special Attack Parameters")] 
    
    public Attack[] dodgeAttacks;
    
    public Attack[] enemyStepAttacks;
    
    [SerializeField] private Attack[] elementalAttacks;
    
    [SerializeField] private Attack[] specialAttacks; // priority 2


    private void OnValidate()
    {
        AttackMap.Clear();
        
        HashSet<Attack> allAttacks = new HashSet<Attack>();
        
        allAttacks.UnionWith(lightComboAttacks);
        allAttacks.UnionWith(heavyComboAttacks);
        allAttacks.UnionWith(midairAttacks);
        allAttacks.UnionWith(specialAttacks);
        allAttacks.UnionWith(elementalAttacks);
        
        foreach (var attack in allAttacks)
        {
            if (!AttackMap.ContainsKey(attack.attackType))
            {
                AttackMap[attack.attackType] = Array.Empty<Attack>();
            }
            
            AttackMap[attack.attackType] = AttackMap[attack.attackType].Append(attack).ToArray();
        }
        
    }

}

