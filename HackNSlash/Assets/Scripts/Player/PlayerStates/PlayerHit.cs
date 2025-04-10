using Extensions.Utils;
using UnityEngine;

public class PlayerHit : PlayerState
{
    public HitInstance hit { get; private set; }

    private bool midairKnockback;
    
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
        
        OnAttackEvents.Instance.KillObjectCoroutines();

        AddKnockbackForce();
        
        if (pc.psm.IsMidair) KnockbackMidair();
        else KnockbackGround();
    }

    public override void OnUpdate()
    {
        if (midairKnockback && pc.psm.IsGrounded)
        {
            pc.rb.linearVelocity = Vector3.zero;
            pc.pac.OnAnimExit(pc.pac.HitAnims.airHit.EndClip, ExitHit);
        }
    }

    #endregion
    
    #region Movement Methods

    public void AddKnockbackForce()
    {
        Vector3 vertical = hit.force.y * Vector3.up;
        Vector3 horizontal = hit.force.x * hit.direction.ToVector3();

        pc.rb.AddForce(vertical + horizontal, ForceMode.VelocityChange);
    }
    
    #endregion
    
    #region Animation Methods

    private void KnockbackMidair()
    {
        midairKnockback = true;
        pc.pac.RootMotionEnabled(false);
        
        pc.gameObject.LookInDirection(-hit.direction);
        pc.pac.ExitTimeAnimation(pc.pac.HitAnims.airHit.StartClip, pc.pac.HitAnims.airHit.LoopClip);
    }

    private void KnockbackGround()
    {
        midairKnockback = false;
        pc.pac.RootMotionEnabled(hit.force.magnitude > 0);
        
        pc.pac.SetAnimancerParam("HitX", hit.direction.x, false);
        pc.pac.SetAnimancerParam("HitZ", hit.direction.y, false);
        pc.pac.OnAnimExit(pc.pac.HitAnims.groundHit, ExitHit);
    }
    
    #endregion

    private void ExitHit()
    {
        pc.psm.canAttack = true;
        sc.ResumePrevious();
    }
    
    
}

public struct HitInstance
{
    public Vector2 direction;
    public Vector2 force;
    public float damage;
}
