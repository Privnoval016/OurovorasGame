using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using OnActionCallbacks;
using System.Collections;
using System.Linq;

public class PlayerAttacking : State
{
    private PlayerController pc;
    public Attack attack;

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
        
        if (attack.exitCondition == ExitConditions.AttackRelease)
        {
            attackEndTime = 10;
            KeyBind[] holdKeys = attack.keyBinds.GetHoldVersion();
            
            
            if (!holdKeys.Any(k => pc.KeyMap[k]()))
            {
                pc.canAttack = true;
                sc.ResumePrevious();
            }
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
        attackCoolDownTime = attack.attackCoolDown;

        if (attack.playFirstClipOnly)
        {
            attackEndTime = attack.attackClips[0].length;
            pc.PlayAnimationClip(attack.attackClips[0], 0.01f);
            pc.InvokeOnAttack(attack);
            return;
        }
        
        attackEndTime = 0;
        foreach (var clip in attack.attackClips)
        {
            attackEndTime += clip.length;
        }
        
        if (attack.attackClips.Length > 0)
        {
            pc.StartCoroutine(AttackWithClip());
        }
        else if (attack.attackNameToHash != "")
        {
            pc.animancer.CrossFade(Animator.StringToHash(attack.attackNameToHash), 0.25f);
        }
        
        pc.InvokeOnAttack(attack);
    }
    
    private IEnumerator AttackWithClip()
    {
        foreach (var clip in attack.attackClips)
        {
            pc.PlayAnimationClip(clip, 0.25f);
            yield return new WaitForSeconds(clip.length);
        }
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
