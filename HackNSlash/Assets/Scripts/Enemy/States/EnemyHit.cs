using Pathfinding;
using UnityEngine;

public class EnemyHit : EnemyState
{
    private bool isGrounded;
    
    public override void OnEnter()
    {
        isGrounded = ec.IsGrounded;
        PlayHitAnimation();
    }

    public override void OnUpdate()
    {
        CheckHitAnimationSwap();
        
        if (!ec.IsMidAttack && ec.IsGrounded)
        {
            ec.sc.ResumePrevious();
        }
    }

    private void CheckHitAnimationSwap()
    {
        if (ec.IsGrounded != isGrounded)
        {
            isGrounded = ec.IsGrounded;
            PlayHitAnimation();
        }
    }

    private void PlayHitAnimation()
    {
        ec.ea.SwitchAnimState(isGrounded ? ec.enemyAnimData.groundHitClip : ec.enemyAnimData.airHitClip);
    }
}
