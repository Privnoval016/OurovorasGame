using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using ExtensionUtils;
using MEC;

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
        //pc.animancer.applyRootMotion = attack.applyRootMotion;
        pc.rootMotion.enabled = attack.applyRootMotion;
        pc.rb.linearVelocity = Vector3.zero;
        
        SetAttackGravity();

        if (attack.attackTransitions.Length > 0) LaunchWithTransition();
        else LaunchClipAttack();
    }

    public override void OnUpdate()
    {
        attackCoolDownTime -= Time.deltaTime;
        attackEndTime -= Time.deltaTime;
        
        if (InputManager.GetHoldVersion(attack.keyBinds).Length == 0) InputManager.Instance.ReleaseHoldAttacks();
        
        SetAttackGravity();
        TurnToLookOnAttack();
        CheckWeaponCollision();
        
        if (attackCoolDownTime < 0)
        {
            pc.canAttack = true;
        }
        
        if (attackEndTime < 0)
        {
            pc.canAttack = true;
            sc.ResumePrevious();
        }
        
        if (attack.exitCondition == ExitConditions.AttackRelease)
        {
            attackEndTime = 10;
            KeyBind[] holdKeys = InputManager.GetHoldVersion(attack.keyBinds);
            
            if (!holdKeys.Any(k => pc.KeyMap[k]()) || pc.moveInput.magnitude > 0.1f)
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
        //pc.animancer.applyRootMotion = false;
        pc.rootMotion.enabled = false;
    }
    
    #endregion

    #region Input Callbacks
    

    #endregion

    #region Attack Methods

    private void LaunchClipAttack()
    {
        pc.canAttack = false;
        attackCoolDownTime = attack.attackCoolDown;

        if (attack.playFirstClipOnly)
        {
            attackEndTime = attack.attackClips[0].length;
            pc.PlayAnimation(attack.attackClips[0], 0.01f);
            pc.InvokeOnAttack(attack);
            return;
        }
        
        attackEndTime = 0;
        foreach (var clip in attack.attackClips)
        {
            attackEndTime += clip.length;
        }

        Timing.RunCoroutine(AttackWithClip());

        
        pc.InvokeOnAttack(attack);
    }

    private void LaunchWithTransition()
    {
        pc.canAttack = false;
        attackCoolDownTime = attack.attackCoolDown;
        
        if (attack.playFirstClipOnly)
        {
            attackEndTime = attack.attackTransitions[0].MaximumDuration;
            pc.PlayAnimation(attack.attackTransitions[0]);
            pc.InvokeOnAttack(attack);
            return;
        }
        
        attackEndTime = 0;
        foreach (var clip in attack.attackTransitions)
        {
            attackEndTime += clip.MaximumDuration / clip.Speed;
        }
        Debug.Log(attackEndTime);
        
        Timing.RunCoroutine(AttackWithTransition());
        
        pc.InvokeOnAttack(attack);
    }
    
    private IEnumerator<float> AttackWithClip()
    {
        foreach (var clip in attack.attackClips)
        {
            pc.PlayAnimation(clip, 0.25f);
            yield return Timing.WaitForSeconds(clip.length);
        }
    }
    
    private IEnumerator<float> AttackWithTransition()
    {
        foreach (var clip in attack.attackTransitions)
        {
            pc.PlayAnimation(clip);
            yield return Timing.WaitForSeconds(clip.MaximumDuration / clip.Speed);
        }
    }

    private void SetAttackGravity()
    {
        if (!attack.isMidair.IsTrue()) ; //pc.CalculateGravity();

        else if (!pc.canAttack) pc.SetGravityScale(0);

        else pc.gravityScale = pc.playerData.midairAttackGravityMult;
    }

    #endregion
    
    #region Collision Methods
    
    private void CheckWeaponCollision()
    {
        
        if (pc.canAttack) return;
        
        
        HashSet<Collider> enemies = new();
        
        foreach (GameObject sword in pc.weapons)
        {
            if (!sword.TryGetComponent(out Collider c)) continue;
            
            Collider[] colliders = Physics.OverlapBox(c.bounds.center, c.bounds.extents, c.transform.rotation, pc.enemyLayer);
            
            
            enemies = enemies.Union(colliders).ToHashSet();
        }

        enemies.RemoveWhere(e => !e.TryGetComponent(out IDamageable d) || d.tookDamageThisAction);
        enemies.RemoveWhere(e => Mathf.Abs(Vector3.Dot(e.transform.position - pc.transform.position, pc.transform.forward)) < 0.8f);
        
        
        foreach (Collider enemy in enemies)
        {
            enemy.GetComponent<IDamageable>().OnHit(pc, attack);
        }
    }
    
    #endregion
    
    #region Rotation Methods
    
    private void TurnToLookOnAttack()
    {
       // Debug.Log(pc.NearestEnemy);
        if (pc.NearestEnemy != null)
        {
            Vector3 lookDir = (pc.NearestEnemy.transform.position - pc.transform.position).ZeroVector3Axis();
            
            if (lookDir.magnitude < 0.5f) return;
            
            pc.transform.rotation =
                EaseUtil.DampQuaternion(pc.transform.rotation, Quaternion.LookRotation(lookDir.normalized), 5f, 0.1f);
        }
        else
        {
            pc.TurnToLook();
        }
    }
    
    #endregion
}
