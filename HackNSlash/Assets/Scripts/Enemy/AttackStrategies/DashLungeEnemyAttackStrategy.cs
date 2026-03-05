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
    [Tooltip("Horizontal speed of the dash in m/s. Applied as a direct velocity set, not a force.")]
    public float dashSpeed = 10f;

    [Tooltip("How long the dash velocity is held (seconds). Should match the lunge clip's active phase.")]
    [Min(0.05f)] public float dashDuration = 0.3f;

    [Tooltip("If true, direction is locked to the player's position at dash start and does not track mid-dash.")]
    public bool lockDirectionOnDash = true;

    protected override void OnExecute()
    {
        oee.RunSegmentCoroutine(DashRoutine());
    }

    private IEnumerator<float> DashRoutine()
    {
        // Wait for the animation's commit frame before applying the dash.
        if (a.attack.animDelay > 0f)
            yield return Timing.WaitForSeconds(a.attack.animDelay);

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

        // Lock the motor so steering doesn't fight the dash.
        NavMotor motor = ts.motor;
        if (motor != null) motor.LockMotion(dashDuration);

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
    }
}


