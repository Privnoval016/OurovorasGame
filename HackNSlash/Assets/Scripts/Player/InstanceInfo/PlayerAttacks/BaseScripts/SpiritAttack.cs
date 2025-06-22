using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Attacks/SpiritAttack")]
public class SpiritAttack : Attack
{
    
    [Header("Spirit Attack Parameters")]
    public OnSpiritMovement onSpiritMovement = OnSpiritMovement.Follow;
    public OnSpiritActions onSpiritAction = OnSpiritActions.None;
    
    protected override void OnValidate()
    {
        base.OnValidate();
        attackType = AttackTypes.Spirit;
    }
    
    protected override void UpdateLinkedAttack()
    {
        base.UpdateLinkedAttack();
        
        if (linkedAttack == this) linkedAttack = null;
        if (linkedAttack == null) return;
        if (!updateLinkedAttack) return;
        if (linkedAttack is not SpiritAttack spiritLinkedAttack) return;
        
        spiritLinkedAttack.onSpiritMovement = onSpiritMovement;
        spiritLinkedAttack.onSpiritAction = onSpiritAction;
    }
}

public enum OnSpiritMovement
{
    Follow,
}
