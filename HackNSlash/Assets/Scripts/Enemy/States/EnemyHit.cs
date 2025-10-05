using Pathfinding;
using UnityEngine;

public class EnemyHit : EnemyState
{
    private bool isGrounded, lastWasMidair, exitTriggered;
    
    public override void OnEnter()
    {
        esm.PauseUtilityAITimer(true);
        isGrounded = esm.ts.pe.IsGrounded;
        lastWasMidair = !isGrounded;
        PlayHitAnimation();
        esm.ts.animListener.DeactivateAllHitboxes();
    }

    public override void OnUpdate()
    {
        
        if (!esm.ts.pe.IsGrounded) lastWasMidair = true;
                
        if (!esm.ts.pe.IsMidAttack && esm.ts.pe.IsGrounded)
        {
            if (!lastWasMidair) 
                ExitHit();
            else esm.ts.ea.ExitTimeAnimation(esm.enemyAnimData.getUpClip, null, ExitHit);
            
            exitTriggered = true;
        }
        else
        {
            CheckHitAnimationSwap();
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        esm.PauseUtilityAITimer(false);
    }

    private void CheckHitAnimationSwap()
    {
        if (esm.ts.pe.IsGrounded != isGrounded)
        {
            isGrounded = esm.ts.pe.IsGrounded;
            PlayHitAnimation();
        }
    }
    
    private void ExitHit()
    {
        esm.sc.ResumePrevious();
    }

    private void PlayHitAnimation()
    {
        if (isGrounded)
        {
            esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.groundHitClip);
        }
        else
        {
            esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.airHitClip);
        }
    }
}
