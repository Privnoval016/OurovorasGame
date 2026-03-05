using System;
using System.Collections.Generic;
using Extensions.Pathfinding;
using Extensions.Utils;
using MEC;
using UnityEngine;

/** <summary>
 * Attack strategy that fires a physical dash toward the player's position
 * at the animation's commit frame (<see cref="EnemyAttack.animDelay"/>),
 * then allows the hitbox to handle damage as normal.
 *
 * Assign this as the <c>enemyAttack</c> field on the lunge <see cref="EnemyAttack"/>
 * asset. Configure <see cref="dashSpeed"/> and <see cref="dashDuration"/> to match
 * the visual length of the lunge animation clip.
 * </summary>
 */
[Serializable]
public class DashLungeEnemyAttackStrategy : IEnemyAttackStrategy
{
    [Tooltip("Index of the lunge animation clip in the EnemyAttack's attackClips array. " +
             "This clip should visually match the dash timing, but the dash is triggered by code at animDelay, " +
             "so the clip's own timing doesn't affect when the dash happens.")]
    public int animationClipIndex = 0;
    
    [Tooltip("Horizontal speed of the dash in m/s. Applied as a direct velocity set, not a force.")]
    public float dashSpeed = 10f;

    [Tooltip("How long the dash velocity is held (seconds). Should match the lunge clip's active phase.")]
    [Min(0.05f)] public float dashDuration = 0.3f;
    
    [Tooltip("How long after the attack starts until the dash is applied (seconds). " +
             "Set this to match the commit frame of the lunge animation, so the dash hits at the right time.")]
    public float telegraphDuration = 0.5f;
    
    [Tooltip("Number of dashes to perform in sequence. If >1, the dash will repeat after a short delay, " +
             "allowing Calliope to cover more distance if the player is far away. " +
             "Set this to 1 for a single dash, or higher for a multi-dash lunge.")]
    public int numberOfDashes = 1;
    
    [Tooltip("Delay between consecutive dashes when numberOfDashes > 1. " +
             "This should be long enough to allow the dash animation to reset visually before the next dash starts.")]
    public float dashCooldown = 0.5f; // Time between consecutive dashes when numberOfDashes > 1

    [Tooltip("If true, direction is locked to the player's position at dash start and does not track mid-dash.")]
    public bool lockDirectionOnDash = true;
    
    protected override bool AutoPlayAttackClips => false;
    
    protected override void OnExecute()
    {
        oee.RunSegmentCoroutine(DashRoutine()).OnDestroy(() =>
        {
            ts.ea.RootMotionEnabled(false);
            ts.esm.SetIsAttacking(false);
            ts.esm.sc.ResumePrevious();
        });
    }

    private IEnumerator<float> DashRoutine()
    {
        for (int i = 0; i < numberOfDashes; i++)
        {
            LaunchClipAttack(animationClipIndex);
            
            // Lock the motor so steering doesn't fight the dash.
            NavMotor motor = ts.motor;
            if (motor != null) motor.LockMotion(dashDuration);
            
            ts.pe.rb.linearVelocity = Vector3.zero;
            
            // Wait until the commit frame to launch the dash, so it visually syncs with the animation.
            yield return Timing.WaitForSeconds(telegraphDuration);

            // Snapshot direction to player at commit frame.
            Vector3 dashDir = ts.transform.forward;
            dashDir.y = 0f;
            if (pc != null)
            {
                Vector3 toPlayer = pc.transform.position - ts.transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    dashDir = toPlayer.normalized;
                    // Face the player instantly so the animation reads correctly.
                    ts.transform.rotation = Quaternion.LookRotation(dashDir);
                }
            }

            float elapsed = 0f;
            while (elapsed < dashDuration)
            {
                float dt = Timing.DeltaTime;
                elapsed += dt;

                if (!lockDirectionOnDash && pc != null)
                {
                    Vector3 live = pc.transform.position - ts.transform.position;
                    live.y = 0f;
                    if (live.sqrMagnitude > 0.001f) dashDir = live.normalized;
                }

                // Set velocity directly each frame -- do NOT use AddForce VelocityChange
                // in a loop, which compounds the impulse on every frame.
                // Preserve vertical velocity so gravity still applies.
                float vy = ts.pe.rb.linearVelocity.y;
                ts.pe.rb.linearVelocity = new Vector3(dashDir.x * dashSpeed, vy, dashDir.z * dashSpeed);

                yield return Timing.WaitForOneFrame;
            }

            // Bleed off horizontal velocity so Calliope stops near the player.
            if (motor != null) motor.Brake();
            
            // If performing multiple dashes, wait a bit before the next one to allow the animation to reset visually.
            if (i < numberOfDashes - 1)
            {
                yield return Timing.WaitForSeconds(dashCooldown);
            }
        }
    }
}


