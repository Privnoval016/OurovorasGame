using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Attacks/SpiritAttack")]
public class SpiritAttack : Attack
{
    
    [Header("Spirit Attack Parameters")]
    public OnSpiritMovement onSpiritMovement = OnSpiritMovement.Follow;
    
    public SpiritActionInfo[] spiritActions;
    
    protected override void OnValidate()
    {
        base.OnValidate();
        attackType = AttackTypes.Spirit;
    }
}

public enum OnSpiritMovement
{
    Follow,
}

[Serializable]
public struct SpiritActionInfo
{
    [SerializeReference] public ISpiritAction spiritAction;
}