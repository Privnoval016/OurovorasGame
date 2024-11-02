using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using OnActionCallbacks;

public class PlayerAttacking : State
{
    private PlayerController pc;
    private Attack attack;

    private float attackCoolDownTime;
    private float attackEndTime;
    
    #region State Methods
    
    public PlayerAttacking(Attack attack)
    {
        this.attack = attack;
    }
    
    public override void OnEnter()
    {
        pc = (PlayerController) sc.parent;
        pc.animancer.applyRootMotion = attack.applyRootMotion;
        pc.rootMotion.enabled = attack.applyRootMotion;
        pc.rb.linearVelocity = Vector3.zero;
        SetAttackGravity();
        LaunchAttack();
    }

    public override void OnUpdate()
    {
        attackCoolDownTime -= Time.deltaTime;
        attackEndTime -= Time.deltaTime;
        
        if (attackCoolDownTime < 0)
        {
            pc.canAttack = true;
            SetAttackGravity(false);
        }
        
        if (attackEndTime < 0)
        {
            pc.canAttack = true;
            sc.ResumePrevious();
        }
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        pc.animancer.applyRootMotion = false;
        pc.rootMotion.enabled = false;
    }
    
    #endregion

    #region Input Callbacks
    

    #endregion

    #region Attack Methods

    private void LaunchAttack()
    {
        pc.canAttack = false;
        attackEndTime = attack.attackClip.length;
        Debug.Log(attackEndTime);
        attackCoolDownTime = attack.attackCoolDown;

        if (attack.attackClip != null)
        {
            pc.animancer.Play(attack.attackClip, 0.25f);
        }
        else if (attack.attackNameToHash != "")
        {
            pc.animancer.CrossFade(Animator.StringToHash(attack.attackNameToHash), 0.25f);
        }
        
        pc.InvokeOnAttack(attack);
    }

    private void SetAttackGravity(bool midAttack = true)
    {
        if (midAttack)
        {
            pc.gravityScale = 0;
            return;
        }
        
        pc.gravityScale = pc.playerData.midairAttackGravityMult;
    }

    #endregion
}
