using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Attacks/SpiritAttack")]
public class SpiritAttack : Attack
{
    public OnSpiritActions onSpiritAction = OnSpiritActions.Follow;
    
    
    private void OnValidate()
    {
        attackType = AttackTypes.Spirit;
    }
}
