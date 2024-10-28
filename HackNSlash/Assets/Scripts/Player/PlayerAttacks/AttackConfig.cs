using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/AttackConfig")]
public class AttackConfig : ScriptableObject
{
    [Header("Unlock Parameters")] 
    
    public bool doubleJumpEnabled;
    
    
    [Header("Attack Parameters")]
    
    public Attack[] lightComboAttacks; // priority 3
    public Attack[] heavyComboAttacks; // priority 3
    
    [Space(5)]
    
    public Attack[] midairAttacks; // priority 1
    
    public Attack[] otherAttacks; // priority 2
    
}

