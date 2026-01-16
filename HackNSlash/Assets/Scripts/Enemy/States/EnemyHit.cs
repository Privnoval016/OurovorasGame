using Extensions.Timers;
using Extensions.Utils;
using Pathfinding;
using UnityEngine;

public class EnemyHit : EnemyState
{
    private bool lastWasMidair;
    private bool hasPlayedLandingAnimation;
    private CountdownTimer hoverFailsafeTimer;
    private const float MaxAirTime = 3f; // Maximum time allowed in air to prevent infinite hovering
    
    public override void OnEnter()
    {
        esm.PauseUtilityAITimer(true);
        
        esm.ts.onEnemyEvents.KillObjectCoroutines();
        esm.SetIsAttacking(false);
        
        // Reset state - important for re-entering hit state
        hasPlayedLandingAnimation = false;
        
        // Check if enemy is being launched (has upward velocity or is already airborne)
        bool isLaunched = !esm.ts.pe.IsGrounded || esm.ts.pe.rb.linearVelocity.y > 0.5f;
        
        // Always reset lastWasMidair based on current state when entering
        // This ensures we play the correct animation even if re-entering from getUp
        lastWasMidair = isLaunched;
        
        // Initialize hover failsafe timer
        if (hoverFailsafeTimer == null)
        {
            hoverFailsafeTimer = new CountdownTimer(MaxAirTime);
            hoverFailsafeTimer.OnTimerStop += OnHoverFailsafe;
        }
        
        esm.ts.animListener.DeactivateAllHitboxes();
        
        // Play the correct animation based on launch detection
        PlayHitAnimation(isLaunched);
    }

    public override void OnUpdate()
    {
        CheckHitAnimationSwap();
        
        // Start failsafe timer if in air
        if (!esm.ts.pe.IsGrounded && !hoverFailsafeTimer.IsRunning)
        {
            hoverFailsafeTimer.Start();
        }
        else if (esm.ts.pe.IsGrounded && hoverFailsafeTimer.IsRunning)
        {
            hoverFailsafeTimer.Stop();
        }
        
        // Exit conditions
        if (!esm.ts.pe.IsMidAttack)
        {
            if (esm.ts.pe.IsGrounded)
            {
                if (!lastWasMidair) // If we were never midair, exit immediately
                {
                    ExitHit();
                }
                else if (!hasPlayedLandingAnimation) // If we were midair, play landing animation first
                {
                    hasPlayedLandingAnimation = true;
                    esm.ts.ea.ExitTimeAnimation(esm.enemyAnimData.getUpClip, null, ExitHit);
                }
                // If landing animation is already playing, let it finish via callback
            }
            // Enemy is falling but not grounded yet, wait for landing
            // Timer will handle failsafe if hovering too long
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        esm.PauseUtilityAITimer(false);
        
        // Clean up timer
        if (hoverFailsafeTimer != null && hoverFailsafeTimer.IsRunning)
        {
            hoverFailsafeTimer.Stop();
        }
    }
    
    private void OnHoverFailsafe()
    {
        Debug.LogWarning($"Enemy {esm.gameObject.name} was hovering for too long in hit state. Forcing exit.");
        ExitHit();
    }

    private void CheckHitAnimationSwap()
    {
        if (!esm.ts.pe.IsGrounded && !lastWasMidair) // If we just went midair, swap to midair hit animation
        {
            PlayHitAnimation(true); // Force air animation
        }
        
        if (!esm.ts.pe.IsGrounded) lastWasMidair = true; // Track if we were ever midair during this hit
    }
    
    private void ExitHit()
    {
        esm.sc.ResumePrevious();
    }

    private void PlayHitAnimation(bool forceAir = false)
    {
        // Force stop any currently playing animation to ensure hit animation takes priority
        // This is critical for interrupting getUp or attack animations
        esm.ts.ea.StopCurrentAnimation();
        
        // Use forceAir parameter to override grounded check
        // This prevents animation mismatches when launching
        if (forceAir || !esm.ts.pe.IsGrounded)
        {
            esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.airHitClip);
        }
        else
        {
            esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.groundHitClip);
        }
    }
}
