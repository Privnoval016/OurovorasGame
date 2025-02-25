using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using ExtensionUtils;
using MEC;

public class PlayerAttacking : State
{
    private PlayerController pc;
    public Attack attack;

    public bool readyToHit = true;

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

        pc.pac.rootMotion.enabled = attack.applyRootMotion;
        pc.rb.linearVelocity = Vector3.zero;
        
        SetAttackGravity();

        if (attack.attackTransitions.Length > 0) LaunchTransitionAttack();
        else if (attack.attackClips.Length > 0) LaunchClipAttack();
    }

    public override void OnUpdate()
    {
        attackCoolDownTime -= Time.deltaTime;
        attackEndTime -= Time.deltaTime;
        
        if (InputManager.GetHoldable(attack.keyBinds).Length == 0) InputManager.Instance.ReleaseHoldAttacks();
        
        SetAttackGravity();
        TurnToLookOnAttack();
        CheckEnemyCollision();
        
        CheckAttackExit();
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        Timing.KillCoroutines(OnAttackEvents.Instance.GetInstanceID());
        pc.psm.pauseComboReset = false;
        pc.pac.rootMotion.enabled = false;
    }
    
    #endregion

    #region Exit Methods

    private void CheckAttackExit()
    {
        if (attack.exitCondition == ExitConditions.ExternalExit)
        {
            attackEndTime = 10;
            attackCoolDownTime = 10;
            if (pc.psm.canAttack)
            {
                sc.ResumePrevious();
            }
        }
        
        if (attackCoolDownTime < 0)
        {
            pc.psm.canAttack = true;
        }
        
        if (attackEndTime < 0)
        {
            pc.psm.canAttack = true;
            sc.ResumePrevious();
        }
        
        if (attackCoolDownTime < -pc.psm.attackData.moveInterruptBuffer && pc.psm.StandardizedMoveDir.magnitude > 0.1f)
        {
            sc.ResumePrevious();
        }
    }
    

    #endregion

    #region Attack Methods

    private void LaunchClipAttack()
    {
        pc.psm.canAttack = false;
        attackCoolDownTime = attack.hitInfo.attackCoolDown;

        List<AnimationClip> clips = new();
        
        for (int i = 0; i < attack.clipsToPlay; i++)
        {
            clips.Add(attack.attackClips[i]);
        }
        
        attackEndTime = 0;
        foreach (var clip in clips)
        {
            attackEndTime += clip.length;
        }

        Timing.RunCoroutine(AttackWithClip(clips));

        
        pc.psm.InvokeOnAttack(attack);
    }

    private void LaunchTransitionAttack()
    {
        pc.psm.canAttack = false;
        attackCoolDownTime = attack.hitInfo.attackCoolDown;
        
        List<TransitionAsset> transitions = new();
        
        for (int i = 0; i < attack.clipsToPlay; i++)
        {
            transitions.Add(attack.attackTransitions[i]);
        }
        
        attackEndTime = 0;
        foreach (var clip in transitions)
        {
            attackEndTime += clip.MaximumDuration / clip.Speed;
        }
        
        Timing.RunCoroutine(AttackWithTransition(transitions));
        
        pc.psm.InvokeOnAttack(attack);
    }
    
    private IEnumerator<float> AttackWithClip(List<AnimationClip> clips)
    {
        if (clips.Count == 0) yield break;
        
        foreach (var clip in clips)
        {
            pc.pac.PlayAnimation(clip, 0.2f, false);
            yield return Timing.WaitForSeconds(clip.length);
        }
    }
    
    private IEnumerator<float> AttackWithTransition(List<TransitionAsset> transitions)
    {
        if (transitions.Count == 0) yield break;
        
        foreach (var clip in transitions)
        {
            pc.pac.PlayAnimation(clip);
            yield return Timing.WaitForSeconds(clip.MaximumDuration / clip.Speed);
        }
    }

    private void SetAttackGravity()
    {
        if (!attack.isMidair.IsTrue()) ; //pc.CalculateGravity();

        else if (!pc.psm.canAttack) pc.psm.SetGravityScale(0);

        else pc.psm.gravityScale = pc.psm.playerData.midairAttackGravityMult;
    }

    #endregion
    
    #region Collision Methods
    
    private void CheckEnemyCollision()
    {
        
        if (pc.psm.canAttack || !readyToHit) return;
        
        HashSet<Collider> enemies = new();
        switch (attack.hitInfo.hitDetection)
        {
            case HitDetections.WeaponTrail:
                enemies = EnemiesInWeaponTrail();
                break;
            case HitDetections.SphereCast:
                enemies = EnemiesInSphere();
                break;
        }
        
        
        foreach (Collider enemy in enemies)
        {
            Debug.Log(enemy.name);
            enemy.GetComponent<IDamageable>().OnHit(pc, attack);
        }
    }

    private HashSet<Collider> EnemiesInWeaponTrail()
    {
        HashSet<Collider> enemies = Physics.OverlapSphere(
            pc.transform.position, 10, pc.psm.enemyLayer).ToHashSet();
        
        enemies.RemoveWhere(e => !e.TryGetComponent(out IDamageable d) || d.tookDamageThisAction);
   
        enemies.RemoveWhere(e => !(e.transform.position - pc.transform.position).ToVector2().
            IsInDirectionCone(pc.transform.forward.ToVector2(), attack.hitInfo.hitRegisterAngle));
        
        enemies = enemies.Where(e => pc.wc.IsIntersecting(e)).ToHashSet();
        
        return enemies;
    }
    
    private HashSet<Collider> EnemiesInSphere()
    {
        HashSet<Collider> enemies = Physics.OverlapSphere(
            pc.transform.position, attack.hitInfo.hitRegisterRadius, pc.psm.enemyLayer).ToHashSet();
        
        enemies.RemoveWhere(e => !e.TryGetComponent(out IDamageable d) || d.tookDamageThisAction);
        
        enemies.RemoveWhere(e => !(e.transform.position - pc.transform.position).ToVector2().
            IsInDirectionCone(pc.transform.forward.ToVector2(), attack.hitInfo.hitRegisterAngle));
        
        return enemies;
    }
    
    #endregion
    
    #region Rotation Methods
    
    private void TurnToLookOnAttack()
    {
        if (attack.exitCondition != ExitConditions.AnimationEnd) return;
        
       // Debug.Log(pc.NearestEnemy);
        if (pc.psm.NearestEnemy != null)
        {
            Vector3 lookDir = (pc.psm.NearestEnemy.transform.position - pc.transform.position).ZeroVector3Axis();
            
            if (lookDir.magnitude < 0.5f) return;
            
            pc.transform.rotation =
                EaseUtil.DampQuaternion(pc.transform.rotation, Quaternion.LookRotation(lookDir.normalized), 5f, 0.1f);
        }
        else
        {
            pc.psm.TurnToLook();
        }
    }
    
    #endregion
}
