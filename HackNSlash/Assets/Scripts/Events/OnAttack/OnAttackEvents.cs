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
}

public class OnAttackEvents : Singleton<OnAttackEvents>
{

    public static Dictionary<OnAttackActions, Action<PlayerController, PlayerAttack>> OnAttackActionMap;
    
    [SerializeField] private OnAttackParameters attackParameters;
    
    protected override void Awake()
    {
        base.Awake();
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
        
    }
    
    IEnumerator<float> ResumeMoving(PlayerController pc, PlayerAttack a, float time, Action action = null)
    {
        yield return Timing.WaitForSeconds(time);
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
            
            Vector3 targetPos = pc.cam.TargetPosition;
            
            Quaternion targetRot = Quaternion.LookRotation((pc.transform.position - targetPos).ZeroVector3Axis());
            
            OnVFXEvents.Instance.InvokeOnVFX(pc, new TransformInfo(targetPos, targetRot, Vector3.one), a, 3);
            
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

                OnVFXEvents.Instance.InvokeOnVFX(pc, new TransformInfo(startPos, startRot, Vector3.one), a, i);
                
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
        yield return Timing.WaitForSeconds(a.animDelay);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        pc.psm.pauseComboReset = true;
        pc.psm.TurnToLook();
        
        KeyBind[] holdKeys = InputManager.GetReleaseable(a.keyBinds);
        yield return Timing.WaitForSeconds(attackParameters.grappleMaxTime);
        
        if (holdKeys.Any(k => !InputManager.KeyMap[k].holdAction()) || pc.psm.IsMidair)
        {
            pc.pac.PlayAnimation(a.attackClips[0], a.animFade);
            
            Vector3 targetPos = pc.cam.TargetPosition;
            Quaternion targetRot = Quaternion.LookRotation((pc.transform.position - targetPos).ZeroVector3Axis());
            
            OnVFXEvents.Instance.InvokeOnVFX(pc, new TransformInfo(targetPos, targetRot, Vector3.one), a, 1);
            
            this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
            
        }
        else
        {
            pc.pac.PlayAnimation(a.attackClips[1], a.animFade);
            
            yield return Timing.WaitUntilTrue(() => holdKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
            
            pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
            
            Vector3 startPos = pc.transform.forward.FindRadialVector3(pc.psm.playerData.mediumRadius, 0) + pc.transform.position;
            Quaternion startRot = Quaternion.LookRotation((pc.cam.TargetPosition - pc.transform.position).ZeroVector3Axis());
                
            OnVFXEvents.Instance.InvokeOnVFX(pc, new TransformInfo(startPos, startRot, Vector3.one), a, 2);
            
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
            out RaycastHit hit, 100f, pc.psm.groundLayer) ? hit.distance : attackParameters.maxAirDashDistance;
        float dist = Mathf.Min(attackParameters.maxAirDashDistance, Mathf.Max(distToGround, direction.magnitude));
        
        Func<bool> loopCondition = () => Time.time - startTime < minAnimTime || pc.psm.IsMidair &&
            a.keyBinds.Any(k => InputManager.KeyMap[k].holdAction())
                                     && Vector3.Distance(startPos, pc.transform.position) < dist;
        

        pc.pac.animancer.gameObject.transform.LookAt(pc.transform.position + direction);
        
        pc.IgnoreCollision(pc.cam.TargetedEnemy?.col, true);
        
        this.RunSegmentCoroutine(pc.rb.TraverseWithVelocity(direction.normalized, attackParameters.airDashSpeed, loopCondition));
        yield return Timing.WaitUntilTrue(() => !loopCondition());
        
        pc.rb.linearVelocity = Vector3.zero;

        //pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
        this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));
        
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

            collidersInPath = Physics.SphereCastAll(pc.transform.position, pc.mainCol.radius, direction, distance * 2f, pc.psm.enemyLayer);

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
                if (hit.collider.TryGetComponent(out LockOnTarget d)) d.OnHit(pc, a, 1);
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
        yield return Timing.WaitForSeconds(a.animDelay);
        
        KeyBind[] holdKeys = InputManager.GetHoldable(a.keyBinds);
        
        yield return Timing.WaitForSeconds(attackParameters.launchUpHoldTime);

        if (!holdKeys.Any(k => InputManager.KeyMap[k].holdAction()))
        {
            yield break;
        }
        
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
        yield return Timing.WaitForSeconds(a.animDelay);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        pc.pac.PlayAnimation(a.attackClips[0], a.animFade);
        yield return Timing.WaitForSeconds(a.attackClips[0].length);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = true;
        pc.pac.PlayAnimation(a.attackClips[1], a.animFade);
        
        pc.rb.linearVelocity = Vector3.zero;
        
        float minAnimTime = 0.02f;
        float startTime = Time.time;

        Func<bool> loopCondition = () => Time.time - startTime < minAnimTime || pc.psm.IsMidair &&
            pc.psm.StandardizedMoveDir.normalized.IsInDirectionCone(a.inputDirection.normalized, 92f);
        
        this.RunSegmentCoroutine(pc.rb.TraverseWithVelocity(Vector3.down, attackParameters.plungeSpeed, loopCondition));
        
        yield return Timing.WaitUntilTrue(() => !loopCondition());
        pc.rb.linearVelocity = Vector3.zero;

        if (pc.psm.IsGrounded)
        {
            pc.pac.PlayAnimation(a.attackClips[2], a.animFade);
        }
        
        this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));
    }
    
    #endregion
    
    #region Enemy Step
    
    public void EnemyStep(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginEnemyStep(pc, a));
    }
    
    private IEnumerator<float> BeginEnemyStep(PlayerController pc, PlayerAttack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        Vector3 enemyPos = pc.psm.GetClosestEnemyInRadius(pc.psm.playerData.mediumRadius) != null ? 
            pc.psm.GetClosestEnemyInRadius(pc.psm.playerData.mediumRadius).TargetedPosition() : pc.transform.position;
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
    }
    
    IEnumerator<float> BeginTargetDodge(PlayerController pc, PlayerAttack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);

        Vector3 teleportedPosition = pc.cam.TargetedEnemy.transform.position +
                                     (pc.transform.position - pc.cam.TargetedEnemy.transform.position)
                                     .ZeroVector3Axis().normalized * pc.psm.playerData.mediumRadius;

        Vector3 dodgeDirection = teleportedPosition - pc.transform.position;

        float distance = dodgeDirection.magnitude;
        dodgeDirection.Normalize();

        float moveTime = attackParameters.dodgeTime / 2;
        
        this.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        yield return Timing.WaitForSeconds(moveTime);
    }
    
    IEnumerator<float> BeginTeleportsBehindYou(PlayerController pc, PlayerAttack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        Vector3 targetPos = pc.cam.TargetPosition +
                            (pc.cam.TargetPosition - pc.transform.position).ZeroVector3Axis()
                            .normalized * (pc.psm.playerData.mediumRadius + pc.cam.TargetedEnemy.radius);

        Vector3 dodgeDirection = targetPos - pc.transform.position;
        float distance = dodgeDirection.magnitude;
        dodgeDirection.Normalize();

        float moveTime = attackParameters.dodgeTime;

        pc.IgnoreCollision(pc.cam.TargetedEnemy?.col, true);
        
        this.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        yield return Timing.WaitForSeconds(moveTime);
        
        pc.IgnoreCollision(pc.cam.TargetedEnemy?.col, false);
    }
    
    IEnumerator<float> BeginDodgeDown(PlayerController pc, PlayerAttack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);

        Vector3 dodgeDirection;
        float distance;
        float moveTime = attackParameters.dodgeTime;
        
        if (pc.psm.IsMidair)
        {
            Vector3 targetPos = -pc.transform.forward.normalized * attackParameters.dodgeDistance + pc.transform.position;
            bool isGround = Physics.Raycast(targetPos, Vector3.down,
                out RaycastHit hit, 100f, pc.psm.groundLayer);
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
    }
    

    #endregion

    #region Mash Attack
    
    private void MashAttack(PlayerController pc, PlayerAttack a)
    {
        this.RunSegmentCoroutine(BeginMashAttack(pc, a));
    }

    IEnumerator<float> BeginMashAttack(PlayerController pc, PlayerAttack a)
    {
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

        for (int i = 0; i < a.vfxInfos.Length; i++)
        {
            if (a.vfxInfos[i].spawnTarget == Target.None) continue;

            Vector3 targetPos = pc.transform.position;
            Quaternion targetRot = Quaternion.identity;
            switch (a.vfxInfos[i].spawnTarget)
            {
                case Target.Player:
                    targetPos = pc.transform.position;
                    targetRot = Quaternion.LookRotation(pc.transform.forward);
                    break;
                case Target.TargetedEnemy:
                    targetPos = pc.cam.TargetPosition;
                    targetRot = Quaternion.LookRotation(pc.transform.position - targetPos);
                    break;
            }
                    
            OnVFXEvents.Instance.InvokeOnVFX(pc, new TransformInfo(
                targetPos, targetRot, Vector3.one), a, i);
        }
    }

    #endregion
}
