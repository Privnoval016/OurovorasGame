using Extensions.Utils;
using UnityEngine;

/** <summary>
 * Handles the enemy hit reaction state.
 * Plays a grounded or airborne hit animation depending on whether the enemy
 * is launched, waits for the enemy to land, then plays a get-up animation
 * before returning to the previous state.
 * </summary>
 */
public class EnemyHit : EnemyState
{
    // True once we have ever been airborne during this hit — used to decide
    // whether a get-up animation is needed on landing.
    private bool _wasEverAirborne;

    // Guards against calling ExitTimeAnimation every frame once landed.
    private bool _gettingUp;

    public override void OnEnter()
    {
        esm.PauseUtilityAITimer(true);

        // Stop any in-flight attack coroutines and hitboxes immediately.
        esm.ts.onEnemyEvents.KillObjectCoroutines();
        esm.ts.animListener.DeactivateAllHitboxes();

        // Clear attacking flag without recording combo history — the action was
        // interrupted, not completed.
        esm.IsAttacking = false;

        _wasEverAirborne = !esm.ts.pe.IsGrounded;
        _gettingUp = false;

        EntityManager.Instance.MarkEnemyAware(esm.ts);

        PlayHitAnimation();
    }

    public override void OnUpdate()
    {
        // Once we start the get-up sequence, stop polling.
        if (_gettingUp) return;

        TrackAirborne();

        if (esm.ts.pe.IsGrounded)
        {
            if (!_wasEverAirborne)
            {
                // Simple ground hit — exit as soon as the animation finishes.
                // ExitHit is wired as the onExit callback in PlayHitAnimation.
                // Nothing extra needed here; the animation callback drives the exit.
            }
            else
            {
                // Just landed after being launched — play get-up then exit.
                _gettingUp = true;
                esm.ts.ea.ExitTimeAnimation(esm.enemyAnimData.getUpClip, null, ExitHit);
            }
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        esm.PauseUtilityAITimer(false);
        _gettingUp = false;
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    /** <summary>
     * Checks whether the enemy has become airborne since entering this state.
     * Swaps to the air-hit animation exactly once when the transition happens.
     * </summary>
     */
    private void TrackAirborne()
    {
        if (!esm.ts.pe.IsGrounded && !_wasEverAirborne)
        {
            // Enemy just became airborne — swap to air hit anim.
            _wasEverAirborne = true;
            PlayHitAnimation();
        }
    }

    private void ExitHit()
    {
        esm.sc.ResumePrevious();
    }

    /** <summary>
     * Plays the appropriate hit animation.
     * For ground hits the animation's OnEnd callback drives <see cref="ExitHit"/>
     * directly (no get-up needed).
     * For air hits the exit is driven by landing detection in <see cref="OnUpdate"/>.
     * </summary>
     */
    private void PlayHitAnimation()
    {
        if (esm.ts.pe.IsGrounded)
        {
            // Ground hit: animation ends → exit.
            esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.groundHitClip, ExitHit);
        }
        else
        {
            // Air hit: loop until landed (OnUpdate handles the landing transition).
            esm.ts.ea.PlayEnemyAnimation(esm.enemyAnimData.airHitClip);
        }
    }
}
