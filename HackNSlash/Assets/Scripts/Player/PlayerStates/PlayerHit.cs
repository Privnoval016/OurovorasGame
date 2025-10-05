using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

public class PlayerHit : PlayerState
{
    public HitInstance hit { get; private set; }

    private bool midairKnockback;

    private bool exiting;
    
    #region State Methods
    
    public PlayerHit(HitInstance hit)
    {
        this.hit = hit;
    }

    public override void OnEnter()
    {
        pc.cam.isFollowingPlayer = true;
        pc.rb.linearVelocity = Vector3.zero;
        pc.psm.canAttack = false;
        
        pc.oae.KillObjectCoroutines();
        pc.IgnoreAllCollisionsWithLayer(GameManager.Instance.enemyLayer, false);

        
        pc.psm.CalculateGravity();
        
        pc.ps.TakeDamage(hit.element, hit.damage);

        AddKnockbackForce();
        
        if (pc.psm.IsMidair) KnockbackMidair();
        else KnockbackGround();
    }

    public override void OnUpdate()
    {
        if (midairKnockback && pc.psm.IsGrounded)
        {
            pc.rb.linearVelocity = Vector3.zero;
            pc.pac.PlayAnimation(pc.pac.HitAnims.airHit.EndClip);

            if (!exiting)
            {
                pc.RunSegmentCoroutine(ExitHit(pc.pac.HitAnims.airHit.EndClip.FadeDuration));
                exiting = true;
            }
        }
    }

    #endregion
    
    #region Movement Methods

    public void AddKnockbackForce()
    {
        Vector3 vertical = hit.force.y * Vector3.up;
        Vector3 horizontal = hit.force.x * hit.horizontalDirection.ToVector3();

        pc.rb.AddForce(vertical + horizontal, ForceMode.VelocityChange);
    }
    
    #endregion
    
    #region Animation Methods

    private void KnockbackMidair()
    {
        midairKnockback = true;
        pc.pac.RootMotionEnabled(false);
        
        pc.gameObject.LookInDirection(-hit.horizontalDirection.ToVector3());
        pc.pac.ExitTimeAnimation(pc.pac.HitAnims.airHit.StartClip, pc.pac.HitAnims.airHit.LoopClip);
    }

    private void KnockbackGround()
    {
        midairKnockback = false;
        pc.pac.RootMotionEnabled(false);
        
        pc.pac.SetAnimancerParam("HitX", hit.horizontalDirection.x, false);
        pc.pac.SetAnimancerParam("HitZ", hit.horizontalDirection.y, false);
        Debug.Log("Hit Direction: " + hit.horizontalDirection);
        pc.pac.PlayAnimation(pc.pac.HitAnims.groundHit);
        
        pc.RunSegmentCoroutine(ExitHit(pc.pac.HitAnims.groundHit.FadeDuration));
    }
    
    #endregion

    private IEnumerator<float> ExitHit(float time)
    {
        yield return Timing.WaitForSeconds(time);
        pc.psm.canAttack = true;
        sc.ResumePrevious();
    }
    
    
}

public struct HitInstance
{
    public Vector2 horizontalDirection;   // direction of the hit in the xz plane
    public Vector2 force;       // x: horizontal force, y: vertical force
    public float damage;
    public ElementEffect element;
}
