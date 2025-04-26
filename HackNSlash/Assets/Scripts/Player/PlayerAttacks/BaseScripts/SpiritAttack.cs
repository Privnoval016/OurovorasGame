using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Attacks/SpiritAttack")]
public class SpiritAttack : Attack
{
    public OnSpiritMovement onSpiritMovement = OnSpiritMovement.Follow;
    public OnSpiritActions onSpiritAction = OnSpiritActions.None;
    
    private void OnValidate()
    {
        attackType = AttackTypes.Spirit;
    }
}

public enum OnSpiritMovement
{
    Follow,
}
