using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using Extensions.Utils;
using MEC;
using Unity.VisualScripting.FullSerializer;

public class PlayerAttacking : PlayerState
{
    private PlayerAttack _playerAttack;

    public bool readyToHit = true;

    private float attackCoolDownTime;
    private float attackEndTime;
    
    private bool chargeUpdated = false;
    
    #region State Methods
    
    public PlayerAttacking(PlayerAttack playerAttack)
    {
        this._playerAttack = playerAttack;
    }
    
    public override void OnEnter()
    {
        pc.pac.RootMotionEnabled(_playerAttack.applyRootMotion);
        pc.cam.isFollowingPlayer = _playerAttack.moveCameraWithAttack;
        pc.rb.linearVelocity = Vector3.zero;
        
        if (_playerAttack.isMidair.IsTrue())
            pc.psm.numMidairAttacks++;
        
        SetAttackGravity();
        
        pc.wc.ActivateWeaponTrailByAttack(_playerAttack);
        pc.wc.ActivateWeaponVFXByAttack(_playerAttack);

        pc.psm.canAttack = false;
        if (_playerAttack.attackTransitions.Length > 0) LaunchTransitionAttack();
        else if (_playerAttack.attackClips.Length > 0) LaunchClipAttack();
        
        pc.psm.InvokeOnAttack(_playerAttack);

        if (_playerAttack.exitCondition == ExitConditions.Immediate)
        {
            attackEndTime = _playerAttack.hitInfo.attackCoolDown;
        }
    }

    public override void OnUpdate()
    {
        attackCoolDownTime -= Time.deltaTime;
        attackEndTime -= Time.deltaTime;
        
        if (InputManager.GetHoldable(_playerAttack.keyBinds).Length == 0) InputManager.Instance.ReleaseHoldAttacks();
        
        SetAttackGravity();
        TurnToLookOnAttack();
        CheckEnemyCollision();
        ChangeAttackCharge();
        
        CheckAttackExit();
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        OnAttackEvents.Instance.KillObjectCoroutines();
        
        pc.rb.linearVelocity = Vector3.zero;
        
        pc.pi.ResetFinisherCharge(_playerAttack);
        
        pc.psm.pauseComboReset = false;
        pc.pac.RootMotionEnabled(false);
        pc.cam.isFollowingPlayer = true;
        
        pc.wc.DeactivateWeaponTrail();
        pc.wc.ActivateImbuedWeaponVFX();
    }
    
    #endregion

    #region Exit Methods

    private void CheckAttackExit()
    {
        if (_playerAttack.exitCondition == ExitConditions.ExternalExit)
        {
            attackEndTime = 10;
            attackCoolDownTime = 10;
            if (pc.psm.canAttack)
            {
                sc.ResumePrevious();
            }
        }
        else if (_playerAttack.exitCondition == ExitConditions.Immediate)
        {
            pc.psm.canAttack = true;
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
        attackCoolDownTime = _playerAttack.hitInfo.attackCoolDown;

        List<AnimationClip> clips = new();
        
        for (int i = 0; i < _playerAttack.clipsToPlay; i++)
        {
            clips.Add(_playerAttack.attackClips[i]);
        }
        
        attackEndTime = 0;
        foreach (var clip in clips)
        {
            attackEndTime += clip.length;
        }

        pc.RunSegmentCoroutine(AttackWithClip(clips));
    }

    private void LaunchTransitionAttack()
    {
        attackCoolDownTime = _playerAttack.hitInfo.attackCoolDown;
        
        List<TransitionAsset> transitions = new();
        
        for (int i = 0; i < _playerAttack.clipsToPlay; i++)
        {
            transitions.Add(_playerAttack.attackTransitions[i]);
        }
        
        attackEndTime = 0;
        foreach (var clip in transitions)
        {
            attackEndTime += clip.MaximumDuration / clip.Speed;
        }

        pc.RunSegmentCoroutine(AttackWithTransition(transitions));
    }
    
    private IEnumerator<float> AttackWithClip(List<AnimationClip> clips)
    {
        if (clips.Count == 0) yield break;
        
        yield return Timing.WaitForSeconds(_playerAttack.animDelay);
        
        foreach (var clip in clips)
        {
            pc.pac.PlayAnimation(clip, _playerAttack.animFade, true);
            yield return Timing.WaitForSeconds(clip.length);
        }
    }
    
    private IEnumerator<float> AttackWithTransition(List<TransitionAsset> transitions)
    {
        if (transitions.Count == 0) yield break;
        
        yield return Timing.WaitForSeconds(_playerAttack.animDelay);
        
        foreach (var clip in transitions)
        {
            pc.pac.PlayAnimation(clip);
            yield return Timing.WaitForSeconds(clip.MaximumDuration / clip.Speed);
        }
    }

    private void SetAttackGravity()
    {
        if (_playerAttack.useNormalGravity)
            pc.psm.CalculateGravity();
        else if (_playerAttack.isMidair.IsTrue())
            pc.psm.SetGravityScale(pc.psm.GetMidairGravity());
    }

    private void ChangeAttackCharge()
    {
        if (chargeUpdated || _playerAttack.stats.charge <= 0) return;
        
        if (_playerAttack.stats.restoreCharge)
        {
            if (pc.psm.EnemiesInHit.Count > 0)
            {
                pc.pi.ApplyAttackMeterChanges(_playerAttack);
                chargeUpdated = true;
            }
        }
        else
        {
            pc.pi.ApplyAttackMeterChanges(_playerAttack);
            chargeUpdated = true;
        }
    }

    #endregion
    
    #region Collision Methods
    
    private void CheckEnemyCollision()
    {
        if (pc.psm.canAttack || !readyToHit) return;
        
        switch (_playerAttack.hitInfo.hitDetection)
        {
            case HitDetections.WeaponCollider:
                pc.psm.enemiesHitThisAction = EnemiesInWeaponCollider().Union(EnemiesInWeaponTrail()).ToHashSet();
                break;
            case HitDetections.WeaponTrail:
                pc.psm.enemiesHitThisAction = EnemiesInWeaponTrail();
                break;
            case HitDetections.SphereCast:
                pc.psm.enemiesHitThisAction = EnemiesInSphere();
                break;
            case HitDetections.HitScan:
                pc.psm.enemiesHitThisAction = EnemiesByHitScan();
                break;
        }

        HashSet<LockOnTarget> secondaryTargts = pc.wc.EnemiesFromFollowWeapons(_playerAttack);
        if (secondaryTargts.Count > 0)
        {
            pc.psm.enemiesHitThisAction = pc.psm.enemiesHitThisAction.Union(secondaryTargts).ToHashSet();
        }
        
        if (pc.psm.enemiesHitThisAction.Count == 0) return;
        
        foreach (LockOnTarget enemy in pc.psm.enemiesHitThisAction)
        {
            enemy.OnHit(ElementData.GetElementFromAttack(_playerAttack.element, pc), pc, _playerAttack, pc.transform);
        }
    }

    private HashSet<LockOnTarget> EnemiesInWeaponTrail()
    {
        HashSet<Collider> enemies = pc.psm
            .GetAllEnemiesInCapsule(pc.psm.playerData.largeRadius, pc.psm.playerData.largeRadius, 360)
            .Select(e => e.GetComponent<Collider>()).ToHashSet();
        
        enemies = enemies.Where(e => pc.wc.IsIntersecting(e)).ToHashSet();
        
        var e = enemies.Select(e => e.GetComponent<LockOnTarget>()).ToHashSet();
        e.RemoveWhere(e => e.TookDamageThisAction(_playerAttack));
        
        return e;
    }

    private HashSet<LockOnTarget> EnemiesInWeaponCollider()
    {
        HashSet<LockOnTarget> enemies = new();

        foreach (var weapon in pc.wc.activeWeapons)
        {
            if (weapon == null) continue;

            enemies = enemies.Union(weapon.IntersectingTargets).ToHashSet();
        }
        
        enemies = enemies.Where(e => e != null && e.TookDamageThisAction(_playerAttack) == false).ToHashSet();

        return enemies;
    }
    
    private HashSet<LockOnTarget> EnemiesInSphere()
    {
        var enemies = pc.psm.GetAllEnemiesInCapsule(_playerAttack.hitInfo.lateralRadius, _playerAttack.hitInfo.verticalRadius, _playerAttack.hitInfo.hitRegisterAngle)
            .ToHashSet();
        
        enemies.RemoveWhere(e => e.TookDamageThisAction(_playerAttack));

        return enemies;
    }
    
    private HashSet<LockOnTarget> EnemiesByHitScan()
    {
        HashSet<LockOnTarget> enemies = new();
        
        if (pc.cam.IsLockedOn) enemies.Add(pc.psm.NearestHEnemy);
        
        int enemiesNeeded = _playerAttack.hitInfo.numTargets - enemies.Count;
        
        if (enemiesNeeded > 0)
        {
            var enemyList = pc.psm.GetAllEnemiesInCapsule(_playerAttack.hitInfo.lateralRadius, _playerAttack.hitInfo.verticalRadius);
            enemies = enemies.Union(enemyList[0..enemiesNeeded]).ToHashSet();
        }
        
        enemies.RemoveWhere(e => e.TookDamageThisAction(_playerAttack));
        
        Debug.Log(enemies.Count);
        
        return enemies;
    }
    
    #endregion
    
    #region Rotation Methods
    
    private void TurnToLookOnAttack()
    {
        if (_playerAttack.exitCondition != ExitConditions.AnimationEnd) return;
        
       // Debug.Log(pc.NearestEnemy);
        if (pc.psm.NearestHEnemy != null)
        {
            Vector3 lookDir = (pc.psm.NearestHEnemy.TargetedPosition() - pc.transform.position).ZeroVector3Axis();
            
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
