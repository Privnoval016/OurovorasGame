using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentLoadout", menuName = "Inventory/EquipmentLoadout", order = 1)]
public class EquipmentLoadout : ScriptableObject
{
    public string loadoutName;
    
    // public Reaction[] activeReactions;
    
    [Header("Equipped Items")]
    
    public Accessory[] equippedAccessories = new Accessory[3];
    
    // public PassiveSkill[] equippedPassives = new PassiveSkill[3];
}



