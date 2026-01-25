using Extensions.Utils;
using Pathfinding;
using UnityEngine;

public class EnemyHit : EnemyState
{
    private bool lastWasMidair;
    
    public override void OnEnter()
    {
        esm.PauseUtilityAITimer(true);
        
        esm.ts.onEnemyEvents.KillObjectCoroutines();
        esm.SetIsAttacking(false);
        
        lastWasMidair = !esm.ts.pe.IsGrounded; // Track if we start midair
        
        esm.ts.animListener.DeactivateAllHitboxes();
        
        EntityManager.Instance.MarkEnemyAware(esm.ts);
        
        PlayHitAnimation();
    }

    public override void OnUpdate()
    {
        CheckHitAnimationSwap();
                
        if (!esm.ts.pe.IsMidAttack && esm.ts.pe.IsGrounded)
        {
            if (!lastWasMidair) // If we were never midair, we can exit immediately
                ExitHit();
            else // If we were midair, we need to play the landing animation first
                esm.ts.ea.ExitTimeAnimation(esm.enemyAnimData.getUpClip, null, ExitHit); 
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        esm.PauseUtilityAITimer(false);
    }

    private void CheckHitAnimationSwap()
    {
        if (!esm.ts.pe.IsGrounded && !lastWasMidair) // If we just went midair, swap to midair hit animation
        {
            PlayHitAnimation();
        }
        
        if (!esm.ts.pe.IsGrounded) lastWasMidair = true; // Track if we were ever midair during this hit
    }
    
    private void ExitHit()
    {
        esm.sc.ResumePrevious();
    }

    private void PlayHitAnimation()
    {
        if (esm.ts.pe.IsGrounded)
        {
            esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.groundHitClip);
        }
        else
        {
            esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.airHitClip);
        }
    }
}
