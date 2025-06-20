using UnityEngine;

public class SpiritAttacking : SpiritState
{
    private SpiritAttack _playerAttack;
    
    public SpiritAttacking(SpiritAttack a)
    {
        _playerAttack = a;
    }
    
    public override void OnEnter()
    {
        spirit.canAttack = false;
        spirit.lastAttackHoldDuration = 0;
        OnSpiritEvents.Instance.OnSpiritActionMap[_playerAttack.onSpiritAction](spirit, _playerAttack);
    }
    
    
    public override void OnUpdate()
    {
        if (spirit.canAttack)
        {
            sc.ResumePrevious();
        }

        UpdateSpiritMovement();
    }
    
    private void UpdateSpiritMovement()
    {
        switch (_playerAttack.onSpiritMovement)
        {
            case OnSpiritMovement.Follow:
                spirit.FollowPlayer();
                break;
        }
    }
}
