using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using MEC;
using UnityEngine;
using UnityEngine.Serialization;

public enum OnAttackActions
{
    None,
    DashToTarget,
    LaunchUp,
    PlungeAttack,
    DodgeMove,
    FloorDash,
    MashAttack,
    BladeBeam,
    Grapple,
    EnemyStep,
    SpawnVFX,
    ImbueElement,
    HoldSpawnVFX,
    BasicFinisher,
    AetherFinisher,
    Parry
}

public class OnAttackEvents : Singleton<OnAttackEvents>
{

    public static Dictionary<OnAttackActions, Action<PlayerController, PlayerAttack>> OnAttackActionMap;
    
    [SerializeField] private OnAttackParameters[] attackParametersArray;
    
    public Dictionary<MovingStates, OnAttackParameters> AttackParametersMap = new();

    private OnAttackParameters GetAttackParameters(PlayerController pc)
    {
        return AttackParametersMap[pc.psm.movingState != MovingStates.NonCombat ? pc.psm.movingState : MovingStates.DualSword];
    }
    
    protected override void Awake()
    {
        base.Awake();
        
        foreach (OnAttackParameters attackParameters in attackParametersArray)
        {
            AttackParametersMap.TryAdd(attackParameters.movingState, attackParameters);
        }
        
        AddOnAttackMethods();
    }
    
    private void AddOnAttackMethods()
    {
        if (OnAttackActionMap != null) return;
        
        OnAttackActionMap = new();
        
        OnAttackActionMap.Add(OnAttackActions.None, (pc, a) => { });

        OnAttackActionMap.Add(OnAttackActions.DashToTarget, DashToTarget);
        OnAttackActionMap.Add(OnAttackActions.LaunchUp, LaunchUp);
        OnAttackActionMap.Add(OnAttackActions.PlungeAttack, PlungeAttack);
        OnAttackActionMap.Add(OnAttackActions.DodgeMove, Dodge);
        OnAttackActionMap.Add(OnAttackActions.FloorDash, FloorDash);
        OnAttackActionMap.Add(OnAttackActions.MashAttack, MashAttack);
        OnAttackActionMap.Add(OnAttackActions.BladeBeam, BladeBeam);
        OnAttackActionMap.Add(OnAttackActions.Grapple, Grapple);
        OnAttackActionMap.Add(OnAttackActions.EnemyStep, EnemyStep);
        OnAttackActionMap.Add(OnAttackActions.SpawnVFX, SpawnVFX);
        OnAttackActionMap.Add(OnAttackActions.ImbueElement, ImbueElement);
        OnAttackActionMap.Add(OnAttackActions.HoldSpawnVFX, HoldSpawnVFX);
        OnAttackActionMap.Add(OnAttackActions.BasicFinisher, BasicFinisher);
        OnAttackActionMap.Add(OnAttackActions.Parry, Parry);
        
    }
    
    IEnumerator<float> ResumeMoving(PlayerController pc, PlayerAttack a, float time, Action action = null)
    {
        yield return Timing.WaitForSeconds(time);
        
        pc.pi.isInvincible = false;
        
        action?.Invoke();
        pc.psm.canAttack = true;
    }
    
    #region Blade Beam
    
    private void BladeBeam(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginBladeBeam(pc, a));
    }
    
    private IEnumerator<float> BeginBladeBeam(PlayerController pc, PlayerAttack a)
    {
        var attackParameters = GetAttackParameters(pc);
        
        yield return Timing.WaitForSeconds(a.animDelay);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        pc.psm.pauseComboReset = true; 
        pc.psm.TurnToLook();
        
        KeyBind[] holdKeys = InputManager.GetReleaseable(a.keyBinds);
        float startTime = Time.time;
        yield return Timing.WaitUntilTrue(() => holdKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
        float elapsedTime = Time.time - startTime;

        if (elapsedTime >= attackParameters.bladeBeamHoldTime && pc.cam.IsLockedOn)
        {
            pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
            
            HashSet<LockOnTarget> enemies = pc.HitScanEnemies(a.hitInfo.numTargets, pc.psm.playerData.lockOnRange, pc.psm.playerData.lockOnRange, 360, a);
        
            foreach (LockOnTarget enemy in enemies)
            {
                Vector3 direction =
                    (enemy.transform.position.ZeroVector3Axis() - pc.transform.position.ZeroVector3Axis()).normalized;
                TransformInfo targetTransform = new TransformInfo(enemy.transform.position - direction * 1.5f, 
                    Quaternion.LookRotation(direction), 
                    Vector3.one);
                CreateVFX(pc, a, 3, targetTransform);
            }
            
            this.RunSegmentCoroutine(ResumeMoving(pc, a, attackParameters.crossSlashDuration, () => pc.psm.pauseComboReset = false));
        }
        else
        {
            pc.pac.PlayAnimation(a.attackClips[1], a.animFade);

            for (int i = 1; i <= 2; i++)
            {
                Vector3 startPos = pc.transform.forward.FindRadialVector3(pc.psm.playerData.mediumRadius, 0) +
                                   pc.transform.position;
                Quaternion startRot = pc.transform.rotation;
                if (pc.cam.IsLockedOn)
                {
                    Vector3 targetPos = pc.cam.TargetPosition;

                    startRot = Quaternion.LookRotation(targetPos - pc.transform.position);
                }

                CreateVFX(pc, a, i, new TransformInfo(startPos, startRot, Vector3.one));
                
                yield return Timing.WaitForSeconds(attackParameters.bladeBeamInterval);
            }

            this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
        }
    }
    
    #endregion
    
    
    #region Grapple
    
    private void Grapple(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginGrapple(pc, a));
    }
    
    private IEnumerator<float> BeginGrapple(PlayerController pc, PlayerAttack a)
    {
        var attackParameters = GetAttackParameters(pc);
        
        yield return Timing.WaitForSeconds(a.animDelay);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        pc.psm.pauseComboReset = true;
        pc.psm.TurnToLook();
        
        KeyBind[] holdKeys = InputManager.GetReleaseable(a.keyBinds);
        yield return Timing.WaitForSeconds(attackParameters.grappleMaxTime);
        
        if (holdKeys.Any(k => !InputManager.KeyMap[k].holdAction()) || pc.psm.IsMidair)
        {
            pc.pac.PlayAnimation(a.attackClips[1], a.animFade);
            
            var vfx = CreateVFX(pc, a, 1);

            if (pc.cam.IsLockedOn)
            {
                pc.cam.TargetedEnemy.OnHit(pc.pi.currentElementEffect, pc, a, pc.transform, 0);
                CombatManager.Instance.PlayHitEffects(a.element, pc, a, vfx, true, 0);
            }
            
            this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
            
        }
        else
        {
            yield return Timing.WaitUntilTrue(() => holdKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
            
            pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
            
            Vector3 startPos = pc.transform.forward.FindRadialVector3(pc.psm.playerData.mediumRadius, 0) + pc.transform.position;
            Quaternion startRot = Quaternion.LookRotation((pc.cam.TargetPosition - pc.transform.position).ZeroVector3Axis());
                
            CreateVFX(pc, a, 2);
            
            this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
        }
        
    }
    
    #endregion
    
    #region Air Dash
    
    private void DashToTarget(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(AirDash(pc, a));
    }

    IEnumerator<float> AirDash(PlayerController pc, PlayerAttack a)
    {
        var attackParameters = GetAttackParameters(pc);
        
        
        yield return Timing.WaitForSeconds(a.animDelay);
        
        pc.pac.PlayAnimation(a.attackClips[0], a.animFade);
        yield return Timing.WaitForSeconds(a.attackClips[0].length);
        
        Quaternion originalRotation = pc.pac.animancer.gameObject.transform.rotation;
        
        pc.pac.PlayAnimation(a.attackClips[1], a.animFade);
       
        pc.rb.linearVelocity = Vector3.zero;
        float minAnimTime = 0.05f;
        float startTime = Time.time;
        Vector3 startPos = pc.transform.position;
        
        Vector3 direction = pc.cam.TargetPosition - pc.transform.position;
        
        if (direction.y > 0)
        {
            direction = direction.ZeroVector3Axis();
            direction = direction.Rotate(-45, Vector3.Cross(direction, Vector3.up));
        }
        
        float distToGround = Physics.SphereCast(pc.transform.position, pc.mainCol.radius, direction.normalized, 
            out RaycastHit hit, 100f, GameManager.Instance.groundLayer) ? hit.distance : 
            attackParameters.maxAirDashDistance;
        float dist = Mathf.Min(attackParameters.maxAirDashDistance, Mathf.Max(distToGround, direction.magnitude));
        
        Func<bool> loopCondition = () => Time.time - startTime < minAnimTime || pc.psm.IsMidair &&
            a.keyBinds.Any(k => InputManager.KeyMap[k].holdAction())
                                     && Vector3.Distance(startPos, pc.transform.position) < dist;
        

        pc.pac.animancer.gameObject.transform.LookAt(pc.transform.position + direction);
        
        pc.IgnoreCollision(pc.cam.TargetedEnemy?.col, true);
        
        this.RunSegmentCoroutine(pc.rb.TraverseWithVelocity(direction.normalized, attackParameters.airDashSpeed, loopCondition));
        yield return Timing.WaitUntilTrue(() => !loopCondition());
        
        pc.rb.linearVelocity = Vector3.zero;

        if (attackParameters.finalAirSlash) pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
        this.RunSegmentCoroutine(ResumeMoving(pc, a, !attackParameters.finalAirSlash ? a.hitInfo.attackCoolDown : Mathf.Max(0, a.hitInfo.attackCoolDown + a.attackClips[2].length - 0.1f)));
        
        pc.pac.animancer.gameObject.transform.rotation = originalRotation;
        pc.IgnoreCollision(pc.cam.TargetedEnemy?.col, false);
        
    }
    
    #endregion

    #region Dash Attack

    private void FloorDash(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(DashAttack(pc, a));
    }

    IEnumerator<float> DashAttack(PlayerController pc, PlayerAttack a)
    {
        var attackParameters = GetAttackParameters(pc);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        yield return Timing.WaitForSeconds(a.animDelay);
        
        KeyBind[] releaseKeys = InputManager.GetReleaseable(a.keyBinds);
        
        float startTime = Time.time;
        
        yield return Timing.WaitUntilTrue(() => releaseKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
        
        float elapsedTime = Time.time - startTime;
        
        
        pc.pac.ExitTimeAnimation(a.attackClips[1], a.attackClips[2]);


        Vector3 direction = (pc.cam.TargetPosition - pc.transform.position).
            WithY(Mathf.Min(pc.cam.TargetPosition.y, pc.transform.position.y) - pc.transform.position.y);
        pc.transform.LookAt(pc.cam.TargetPosition.WithY(pc.transform.position.y));
        
        float distance;
        RaycastHit[] collidersInPath = null;

        if (elapsedTime >= attackParameters.groundDashTimeThreshold)
        {
            distance = Mathf.Max(Mathf.Min(attackParameters.groundDashDistance * 2, direction.magnitude * 2), attackParameters.groundDashDistance * 0.8f);

            collidersInPath = Physics.SphereCastAll(pc.transform.position, pc.mainCol.radius, 
                direction, distance * 2f, GameManager.Instance.enemyLayer);

            foreach (var hit in collidersInPath)
            {
               pc.IgnoreCollision(hit.collider, true);
            }
            pc.IgnoreCollision(pc.cam.TargetedEnemy?.col, true);
        }
        else
        {
            ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = true;
            distance = Mathf.Min(attackParameters.groundDashDistance, direction.magnitude);
        }
        
        yield return Timing.WaitUntilDone(pc.rb.TraverseDistanceInTime(direction.normalized, distance, attackParameters.groundDashTime));
        

        pc.pac.PlayAnimation(a.attackClips[3], a.animFade);
        
        
        this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));
        
        if (collidersInPath != null)
        {
            foreach (var hit in collidersInPath)
            {
                if (hit.collider.TryGetComponent(out LockOnTarget d)) d.OnHit(pc.pi.currentElementEffect, pc, a, pc.transform, 1);
                pc.IgnoreCollision(hit.collider, false);
            }
        }
        pc.IgnoreCollision(pc.cam.TargetedEnemy?.col, false);
    }
    
    #endregion

    #region Launch Up Attack
    
    private void LaunchUp(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginLaunchUp(pc, a));
    }
    
    private IEnumerator<float> BeginLaunchUp(PlayerController pc, PlayerAttack a)
    {
        var attackParameters = GetAttackParameters(pc);
        
        KeyBind[] holdKeys = InputManager.GetHoldable(a.keyBinds);
        
        yield return Timing.WaitForSeconds(attackParameters.launchUpHoldTime);

        if (!holdKeys.Any(k => InputManager.KeyMap[k].holdAction()))
        {
            yield break;
        }
        
        Timing.WaitForSeconds(a.animDelay);
        
        pc.pac.PlayAnimation(a.attackClips[1], a.animFade, false);
        
        this.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(Vector3.up, attackParameters.launchUpHeight, attackParameters.launchUpTime));

        Timing.WaitForSeconds(attackParameters.launchUpTime);
        
        pc.rb.linearVelocity = pc.psm.playerData.jumpHangSpeedThreshold * Vector3.up;

    }
    
    #endregion
    
    #region Plunge Attack
    
    private void PlungeAttack(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginPlungeAttack(pc, a));
    }
    
    private IEnumerator<float> BeginPlungeAttack(PlayerController pc, PlayerAttack a)
    {
        var attackParameters = GetAttackParameters(pc);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        pc.pac.PlayAnimation(a.attackClips[0], a.animFade);
        yield return Timing.WaitForSeconds(a.attackClips[0].length);
        
        yield return Timing.WaitForSeconds(a.animDelay);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = true;
        pc.pac.PlayAnimation(a.attackClips[1], a.animFade);
        
        pc.rb.linearVelocity = Vector3.zero;

        float minAnimTime = attackParameters.minPlungeTime;
        float startTime = Time.time;

        Func<bool> loopCondition = () => Time.time - startTime < minAnimTime || pc.psm.IsMidair &&
            pc.psm.StandardizedMoveDir.normalized.IsInDirectionCone(a.inputDirection.normalized, 92f);
        
        this.RunSegmentCoroutine(pc.rb.TraverseWithVelocity(Vector3.down, attackParameters.plungeSpeed, loopCondition));
        
        yield return Timing.WaitUntilTrue(() => !loopCondition());
        pc.rb.linearVelocity = Vector3.zero;

        if (pc.psm.IsGrounded)
        {
            pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
            this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));
        }
        else
        {
            this.RunSegmentCoroutine(ResumeMoving(pc, a, minAnimTime));
        }
        
        
    }
    
    #endregion
    
    #region Enemy Step
    
    public void EnemyStep(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginEnemyStep(pc, a));
    }
    
    private IEnumerator<float> BeginEnemyStep(PlayerController pc, PlayerAttack a)
    {
        var attackParameters = GetAttackParameters(pc);
        
        yield return Timing.WaitForSeconds(a.animDelay);
        
        Vector3 enemyPos = pc.psm.GetClosestEnemyInCapsule(pc.psm.playerData.mediumRadius, pc.psm.playerData.heightRadius) != null ? 
            pc.psm.GetClosestEnemyInCapsule(pc.psm.playerData.mediumRadius, pc.psm.playerData.heightRadius).TargetedPosition() : pc.transform.position;
        Vector3 direction = (pc.transform.position - enemyPos).ZeroVector3Axis().normalized;
        direction = (Vector3.up + direction * attackParameters.enemyStepPushBack).normalized;
        
        pc.rb.linearVelocity = pc.rb.linearVelocity.ZeroVector3Axis();
		
        this.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(direction, attackParameters.enemyStepHeight, attackParameters.enemyStepTime));
    }
    
    
    #endregion

    #region Dodge
    
    private void Dodge(PlayerController pc, PlayerAttack a)
    {
        
        IEnumerator<float> attack = BeginRegularDodge(pc, a);
        
        pc.pi.isInvincible = true;
        
        switch (a.attackEventIndex)
        {
            case 0:
                attack = BeginRegularDodge(pc, a);
                break;
            case 1:
                attack = BeginTargetDodge(pc, a);
                break;
            case 2:
                attack = BeginDodgeDown(pc, a);
                break;
            case 3:
                attack = BeginTeleportsBehindYou(pc, a);
                break;
        }
        
        this.RunSegmentCoroutine(attack);
    }

    IEnumerator<float> BeginRegularDodge(PlayerController pc, PlayerAttack a)
    { 
        var attackParameters = GetAttackParameters(pc);

        yield return Timing.WaitForSeconds(a.animDelay);
        
        Vector3 dodgeDirection = pc.psm.moveDirection.ZeroVector3Axis().normalized;
        float distance = attackParameters.dodgeDistance;
        float moveTime = attackParameters.dodgeTime;
        
        if (pc.psm.StandardizedMoveDir.magnitude < 0.1f)
        {
            dodgeDirection = pc.psm.IsMidair ? Vector3.down : Vector3.up;
        }
        
        this.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        yield return Timing.WaitForSeconds(moveTime);
        
        this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));

    }
    
    IEnumerator<float> BeginTargetDodge(PlayerController pc, PlayerAttack a)
    {
        var attackParameters = GetAttackParameters(pc);

        
        yield return Timing.WaitForSeconds(a.animDelay);

        Vector3 teleportedPosition = pc.cam.TargetedEnemy.transform.position +
                                     (pc.transform.position - pc.cam.TargetedEnemy.transform.position)
                                     .ZeroVector3Axis().normalized * pc.psm.playerData.mediumRadius;

        Vector3 dodgeDirection = teleportedPosition - pc.transform.position;

        float distance = dodgeDirection.magnitude;
        dodgeDirection.Normalize();

        float moveTime = attackParameters.dodgeTime / 2;


        (pc.cam.TargetedEnemy as PhysicsEnemy)?.ResetMovement();
        
        
        this.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        yield return Timing.WaitForSeconds(moveTime);
        
        this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));
    }
    
    IEnumerator<float> BeginTeleportsBehindYou(PlayerController pc, PlayerAttack a)
    {
        var attackParameters = GetAttackParameters(pc);

        yield return Timing.WaitForSeconds(a.animDelay);
        
        Vector3 targetPos = pc.cam.TargetPosition +
                            (pc.cam.TargetPosition - pc.transform.position).ZeroVector3Axis()
                            .normalized * pc.psm.playerData.mediumRadius;

        Vector3 dodgeDirection = targetPos - pc.transform.position;
        float distance = dodgeDirection.magnitude;
        dodgeDirection.Normalize();

        float moveTime = attackParameters.dodgeTime;
        
        (pc.cam.TargetedEnemy as PhysicsEnemy)?.ResetMovement();

        pc.IgnoreCollision(pc.cam.TargetedEnemy?.col, true);
        
        this.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        yield return Timing.WaitForSeconds(moveTime);
        
        pc.IgnoreCollision(pc.cam.TargetedEnemy?.col, false);
        
        this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));

    }
    
    IEnumerator<float> BeginDodgeDown(PlayerController pc, PlayerAttack a)
    {
        var attackParameters = GetAttackParameters(pc);

        yield return Timing.WaitForSeconds(a.animDelay);

        Vector3 dodgeDirection;
        float distance;
        float moveTime = attackParameters.dodgeTime;
        
        if (pc.psm.IsMidair)
        {
            Vector3 targetPos = -pc.transform.forward.normalized * attackParameters.dodgeDistance + pc.transform.position;
            bool isGround = Physics.Raycast(targetPos, Vector3.down,
                out RaycastHit hit, 100f, GameManager.Instance.groundLayer);
            Vector3 point = isGround ? hit.point : targetPos;

            dodgeDirection = point - pc.transform.position;
            distance = dodgeDirection.magnitude;
            dodgeDirection.Normalize();
        }
        else
        {
            dodgeDirection = -pc.transform.forward.normalized;
            distance = attackParameters.dodgeDistance;
        }

        this.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        yield return Timing.WaitForSeconds(moveTime);
        
        this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));

    }
    

    #endregion

    #region Mash Attack
    
    private void MashAttack(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginMashAttack(pc, a));
    }

    IEnumerator<float> BeginMashAttack(PlayerController pc, PlayerAttack a)
    {
        var attackParameters = GetAttackParameters(pc);

        yield return Timing.WaitForSeconds(a.animDelay);
        
        float timeSinceLastClick = 0;
        float startTime = Time.time;
        
        pc.psm.pauseComboReset = true;
        
        while (timeSinceLastClick < attackParameters.mashInterval && Time.time - startTime < attackParameters.mashDuration)
        {
            if (InputManager.KeyMap[a.keyBinds[0]].action())
            {
                timeSinceLastClick = 0;
            }
            
            timeSinceLastClick += Time.deltaTime;
            
            yield return Timing.WaitForOneFrame;
        }
        
        
        if (timeSinceLastClick >= attackParameters.mashInterval)
        {
            this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
            pc.pac.PlayAnimation(a.attackClips[1], a.animFade);
        }
        else
        {
            this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown * 2, () => pc.psm.pauseComboReset = false));
            pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
        }
    }
    
    #endregion

    #region SpawnVFX

    private void SpawnVFX(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginSpawnVFX(pc, a));
    }
    
    private IEnumerator<float> BeginSpawnVFX(PlayerController pc, PlayerAttack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);

        CreateVFX(pc, a, a.attackEventIndex);
    }

    private VFXController CreateVFX(PlayerController pc, PlayerAttack a, int index, TransformInfo start = default)
    {
        return OnVFXEvents.Instance.SpawnPlayerVFX(pc, a, index, start);
    }

    #endregion
    
    
    #region Imbue Element
    
    private void ImbueElement(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginImbueElement(pc, a));
    }
    
    private IEnumerator<float> BeginImbueElement(PlayerController pc, PlayerAttack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        if (pc.pi.currentElementEffect == ElementEffect.None) yield break;
        
        pc.KillObjectCoroutines(nameof(ResetImbuedElement));

        float delay = a.hitInfo.attackCoolDown;

        if (pc.pi.imbuedElementEffect != pc.pi.currentElementEffect)
        {
            pc.pac.PlayAnimation(a.attackClips[pc.psm.IsMidair ? 1 : 0], a.animFade);
            pc.pi.imbuedElementEffect = pc.pi.currentElementEffect;
        }
        else
        {
            delay = 0;
            pc.pi.imbuedElementEffect = ElementEffect.None;
        }
        
        pc.RunSegmentCoroutine(ResetImbuedElement(pc, GetAttackParameters(pc).imbueElementDuration), nameof(ResetImbuedElement));

        this.RunSegmentCoroutine(ResumeMoving(pc, a, delay));
    }

    private IEnumerator<float> ResetImbuedElement(PlayerController pc, float time)
    {
        yield return Timing.WaitForSeconds(time);
        pc.pi.imbuedElementEffect = ElementEffect.None;
        pc.wc.DeactivateAllWeaponVFX();
    }
    
    
    #endregion
    
    
    #region Hold Spawn VFX
    
    
    private void HoldSpawnVFX(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginHoldSpawnVFX(pc, a));
    }
    
    private IEnumerator<float> BeginHoldSpawnVFX(PlayerController pc, PlayerAttack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        KeyBind[] holdKeys = InputManager.GetReleaseable(a.keyBinds);
        float startTime = Time.time;
        yield return Timing.WaitUntilTrue(() => holdKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
        float elapsedTime = Time.time - startTime;

        pc.spirit.lastAttackHoldDuration = elapsedTime;
        print($"Hold Duration: {elapsedTime}");
        
        pc.pac.PlayAnimation(a.attackClips[1], a.animFade);

        for (int i = 0; i < a.vfxInfos.Length; i++)
        {
            CreateVFX(pc, a, i);
        }
        
        this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));
    }
    
    #endregion
    
    #region Basic Finisher
    
    private void BasicFinisher(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginBasicFinisher(pc, a));
    }
    
    private IEnumerator<float> BeginBasicFinisher(PlayerController pc, PlayerAttack a)
    {
        #region Initial Setup
        
        LockOnTarget target = pc.psm.NearestHEnemy;
        
        if (target == null)
        {
            Debug.LogWarning("No target found for finisher!");
            yield break;
        }
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        pc.cam.FinisherTarget = target;
        pc.cam.SwitchState(PlayerCamStates.FinisherCloseUp);
        
        pc.psm.SwapToUltimate();
        
        #endregion
        
        #region Move to Target
        
        var attackParameters = GetAttackParameters(pc);

        Vector3 teleportedPosition = pc.cam.FinisherTarget.transform.position +
                                     (pc.transform.position - pc.cam.FinisherTarget.transform.position)
                                     .ZeroVector3Axis().normalized * pc.psm.playerData.mediumRadius;

        Vector3 dodgeDirection = teleportedPosition - pc.transform.position;

        float distance = dodgeDirection.magnitude;
        dodgeDirection.Normalize();

        float moveTime = attackParameters.dodgeTime / 2;


        (pc.cam.TargetedEnemy as PhysicsEnemy)?.ResetMovement();
        
        pc.pac.PlayAnimation(a.attackTransitions[0]);
        
        
        this.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        yield return Timing.WaitForSeconds(moveTime);
        
        #endregion
        
        #region Perform Finisher
        
        float timeScale = attackParameters.finisherTimeScale != 0 ? attackParameters.finisherTimeScale : 1;
        
        CombatManager.Instance.ApplySlowedTimeScale(pc, true, timeScale);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = true;
        
        pc.pac.PlayAnimation(a.attackClips[0], a.animFade);

        yield return Timing.WaitForSeconds(a.attackClips[0].length / timeScale);
        
        CombatManager.Instance.ApplySlowedTimeScale(pc, false);
        
        #endregion
        
        #region Final Setup
        
        pc.psm.SwapToUltimate();
        
        pc.cam.SwitchState(PlayerCamStates.Free);
        pc.cam.FinisherTarget = null;
        
        this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));
        
        #endregion
        
    }
    
    #endregion
    
    #region Parry
    
    private void Parry(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginParry(pc, a));
    }
    
    private IEnumerator<float> BeginParry(PlayerController pc, PlayerAttack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);

        if (pc.wc.activeWeapons.Count == 0)
        {
            pc.pi.isInvincible = false;
            yield break;
        }
        
        pc.rb.linearVelocity = Vector3.zero;
        
        pc.pi.isInvincible = true;

        HashSet<PhysicsEnemy> parriedEnemies = new();
        foreach (var hitbox in pc.psm.ParriedHitboxes)
        {
            if (hitbox.ts.lot is PhysicsEnemy enemy)
            {
                parriedEnemies.Add(enemy);
            }
        }

        foreach (var e in parriedEnemies)
        {
            e.OnStagger(pc.pi.currentElementEffect, pc, a, pc.transform, 0);
        }
        
        CombatManager.Instance.PlayParryEffects(a.element, pc, a, pc.wc.activeWeapons[0], true, 0);
        
        yield return Timing.WaitForSeconds(a.attackClips[0].length + a.hitInfo.attackCoolDown);
        
        pc.pi.isInvincible = false;
    }
    
    #endregion
}
