using Pathfinding;
using UnityEngine;

public class EnemyHit : EnemyState
{
    private bool isGrounded, lastWasMidair, exitTriggered;
    
    public override void OnEnter()
    {
        isGrounded = ec.ts.pe.IsGrounded;
        lastWasMidair = !isGrounded;
        PlayHitAnimation();
        ec.ts.animListener.DeactivateAllHitboxes();
    }

    public override void OnUpdate()
    {
        
        if (!ec.ts.pe.IsGrounded) lastWasMidair = true;
                
        if (!ec.ts.pe.IsMidAttack && ec.ts.pe.IsGrounded)
        {
            if (!lastWasMidair) 
                ExitHit();
            else ec.ts.ea.ExitTimeAnimation(ec.enemyAnimData.getUpClip, null, ExitHit);
            
            exitTriggered = true;
        }
        else
        {
            CheckHitAnimationSwap();
        }
    }

    private void CheckHitAnimationSwap()
    {
        if (ec.ts.pe.IsGrounded != isGrounded)
        {
            isGrounded = ec.ts.pe.IsGrounded;
            PlayHitAnimation();
        }
    }
    
    private void ExitHit()
    {
        ec.sc.ResumePrevious();
    }

    private void PlayHitAnimation()
    {
        if (isGrounded)
        {
            ec.ts.ea.SwitchAnimState(ec.enemyAnimData.groundHitClip);
        }
        else
        {
            ec.ts.ea.SwitchAnimState(ec.enemyAnimData.airHitClip);
        }
    }
}
