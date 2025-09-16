using UnityEngine;

public class SpiritAttacking : SpiritState
{
    private SpiritAttack _playerAttack;
    private bool chargeUpdated = false;
    
    public SpiritAttacking(SpiritAttack a)
    {
        _playerAttack = a;
    }
    
    public override void OnEnter()
    {
        spirit.canAttack = false;
        spirit.lastAttackHoldDuration = 0;
        
        OnSpiritEvents.Instance.InvokeOnSpiritAction(spirit, _playerAttack);
    }
    
    
    public override void OnUpdate()
    {
        if (spirit.canAttack)
        {
            sc.ResumePrevious();
        }

        UpdateSpiritMovement();
        ChangeAttackCharge();
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
    
    private void ChangeAttackCharge()
    {
        if (chargeUpdated || _playerAttack.stats.charge <= 0) return;
        
        if (_playerAttack.stats.restoreCharge)
        {
            if (spirit.pc.psm.EnemiesInHit.Count > 0)
            {
                spirit.pc.pi.ApplyAttackMeterChanges(_playerAttack);
                chargeUpdated = true;
            }
        }
        else
        {
            spirit.pc.pi.ApplyAttackMeterChanges(_playerAttack);
            chargeUpdated = true;
        }
    }
}
