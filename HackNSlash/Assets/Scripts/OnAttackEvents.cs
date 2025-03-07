using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionUtils;
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
    SpawnVFX
}

public class OnAttackEvents : MonoBehaviour
{
    public static OnAttackEvents Instance { get; private set; }

    public static Dictionary<OnAttackActions, Action<PlayerController, Attack>> OnAttackActionMap;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
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
        
    }
    
    IEnumerator<float> ResumeMoving(PlayerController pc, Attack a, float time, Action action = null)
    {
        yield return Timing.WaitForSeconds(time);
        if (action != null) action();
        pc.psm.canAttack = true;
    }
    
    #region Blade Beam
    
    [Header("Blade Beam")]
    [SerializeField] private float bladeBeamHoldTime;
    [SerializeField] private float crossSlashDuration = 1.3f;
    
    private void BladeBeam(PlayerController pc, Attack a)
    {
        this.RunSegmentCoroutine(BeginBladeBeam(pc, a));
    }
    
    private IEnumerator<float> BeginBladeBeam(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        pc.psm.pauseComboReset = true; 
        pc.psm.TurnToLook();
        
        KeyBind[] holdKeys = InputManager.GetReleaseable(a.keyBinds);
        float startTime = Time.time;
        yield return Timing.WaitUntilTrue(() => holdKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
        float elapsedTime = Time.time - startTime;

        if (elapsedTime >= bladeBeamHoldTime && pc.cam.IsLockedOn)
        {
            pc.pac.PlayAnimation(a.attackClips[2], 0.1f);
            
            Vector3 targetPos = pc.cam.TargetPosition;
            
            Quaternion targetRot = Quaternion.LookRotation((pc.transform.position - targetPos).ZeroVector3Axis());
            
            OnVFXEvents.Instance.InvokeOnVFX(pc, new TransformInfo(targetPos, targetRot, Vector3.one), a, 2);
            
            this.RunSegmentCoroutine(ResumeMoving(pc, a, crossSlashDuration, () => pc.psm.pauseComboReset = false));
        }
        else
        {
            pc.pac.PlayAnimation(a.attackClips[1], 0.1f);
            
            Vector3 startPos = pc.transform.forward.FindRadialVector3(pc.psm.playerData.mediumRadius, 0) + pc.transform.position;
            Quaternion startRot = pc.transform.rotation;
            if (pc.cam.IsLockedOn)
            {
                Vector3 targetPos = pc.cam.TargetPosition;

                startRot = Quaternion.LookRotation(targetPos - pc.transform.position);
            }
            
            OnVFXEvents.Instance.InvokeOnVFX(pc, new TransformInfo(startPos, startRot, Vector3.one), a, 1);
            
            this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
        }
    }
    
    #endregion
    
    
    #region Grapple
    
    [Header("Grapple")]
    [SerializeField] private float grappleMaxTime = 0.2f;
    
    private void Grapple(PlayerController pc, Attack a)
    {
        this.RunSegmentCoroutine(BeginGrapple(pc, a));
    }
    
    private IEnumerator<float> BeginGrapple(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        pc.psm.pauseComboReset = true;
        pc.psm.TurnToLook();
        
        KeyBind[] holdKeys = InputManager.GetReleaseable(a.keyBinds);
        yield return Timing.WaitForSeconds(grappleMaxTime);
        
        if (holdKeys.Any(k => !InputManager.KeyMap[k].holdAction()) || pc.psm.IsMidair)
        {
            Vector3 targetPos = pc.cam.TargetPosition;
            Quaternion targetRot = Quaternion.LookRotation((pc.transform.position - targetPos).ZeroVector3Axis());
            
            OnVFXEvents.Instance.InvokeOnVFX(pc, new TransformInfo(targetPos, targetRot, Vector3.one), a, 1);
            
            this.RunSegmentCoroutine(ResumeMoving(pc, a, 0.01f, () => pc.psm.pauseComboReset = false));
            
        }
        else
        {
            pc.pac.PlayAnimation(a.attackClips[1], 0.1f);
            
            yield return Timing.WaitUntilTrue(() => holdKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
            
            pc.pac.PlayAnimation(a.attackClips[2], 0.1f);
            
            Vector3 startPos = pc.transform.forward.FindRadialVector3(pc.psm.playerData.mediumRadius, 0) + pc.transform.position;
            Quaternion startRot = Quaternion.LookRotation((pc.cam.TargetPosition - pc.transform.position).ZeroVector3Axis());
                
            OnVFXEvents.Instance.InvokeOnVFX(pc, new TransformInfo(startPos, startRot, Vector3.one), a, 2);
            
            this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
        }
        
    }
    
    #endregion
    
    
    #region Air Dash

    [FormerlySerializedAs("dashSpeed")]
    [Header("Air Dash")]
    [SerializeField] private float airDashSpeed;

    [FormerlySerializedAs("maxDashDistance")] [SerializeField] private float maxAirDashDistance = 30f;
    
    private void DashToTarget(PlayerController pc, Attack a)
    {
        this.RunSegmentCoroutine(AirDash(pc, a));
    }

    IEnumerator<float> AirDash(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        pc.pac.PlayAnimation(a.attackClips[0], 0.01f);
        yield return Timing.WaitForSeconds(a.attackClips[0].length);
        
        Quaternion originalRotation = pc.pac.animancer.gameObject.transform.rotation;
        
        pc.pac.PlayAnimation(a.attackClips[1], 0.01f);
       
        pc.rb.linearVelocity = Vector3.zero;
        float minAnimTime = 0.05f;
        float startTime = Time.time;
        Vector3 startPos = pc.transform.position;
        
        Func<bool> loopCondition = () => (Time.time - startTime < minAnimTime || pc.psm.IsMidair &&
            pc.psm.StandardizedMoveDir.normalized.IsInDirectionCone(a.inputDirection.normalized, 92f)) &&
                                         Vector3.Distance(startPos, pc.transform.position) < maxAirDashDistance;
        
        // rotate the direction downwards by 45 degrees
        Vector3 direction = pc.cam.TargetPosition - pc.transform.position;

        if (direction.y > 0)
        {
            direction = direction.ZeroVector3Axis();
            direction = direction.Rotate(-45, Vector3.Cross(direction, Vector3.up));
        }


        pc.pac.animancer.gameObject.transform.LookAt(pc.transform.position + direction);
        
        pc.IgnoreCollision(pc.cam.TargetedEnemy.col, true);
        
        this.RunSegmentCoroutine(pc.rb.TraverseWithVelocity(direction.normalized, airDashSpeed, loopCondition));
        yield return Timing.WaitUntilTrue(() => !loopCondition());
        
        pc.rb.linearVelocity = Vector3.zero;

        pc.pac.PlayAnimation(a.attackClips[2], 0.01f);
        this.RunSegmentCoroutine(ResumeMoving(pc, a, a.attackClips[2].length));
        
        pc.pac.animancer.gameObject.transform.rotation = originalRotation;
        pc.IgnoreCollision(pc.cam.TargetedEnemy.col, false);
        
    }
    
    #endregion

    #region Dash Attack
    
    [FormerlySerializedAs("dashDistance")]
    [Header("Dash Attack")]
    
    [SerializeField] private float groundDashDistance;
    [FormerlySerializedAs("dashTime")] [SerializeField] private float groundDashTime = 0.2f;
    [FormerlySerializedAs("dashTimeThreshold")] [SerializeField] private float groundDashTimeThreshold = 0.4f;

    private void FloorDash(PlayerController pc, Attack a)
    {
        this.RunSegmentCoroutine(DashAttack(pc, a));
    }

    IEnumerator<float> DashAttack(PlayerController pc, Attack a)
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

        if (elapsedTime >= groundDashTimeThreshold)
        {
            distance = Mathf.Max(Mathf.Min(groundDashDistance * 2, direction.magnitude * 2), groundDashDistance * 0.8f);

            collidersInPath = Physics.SphereCastAll(pc.transform.position, pc.mainCol.radius, direction, distance * 2f, pc.psm.enemyLayer);

            foreach (var hit in collidersInPath)
            {
               pc.IgnoreCollision(hit.collider, true);
            }
            pc.IgnoreCollision(pc.cam.TargetedEnemy.col, true);
        }
        else
        {
            ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = true;
            distance = Mathf.Min(groundDashDistance, direction.magnitude);
        }
        
        yield return Timing.WaitUntilDone(pc.rb.TraverseDistanceInTime(direction.normalized, distance, groundDashTime));
        

        pc.pac.PlayAnimation(a.attackClips[3], 0.01f);
        
        
        this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));
        
        if (collidersInPath != null)
        {
            foreach (var hit in collidersInPath)
            {
                if (hit.collider.TryGetComponent(out LockOnTarget d)) d.OnHit(pc, a, 1);
                pc.IgnoreCollision(hit.collider, false);
            }
        }
        pc.IgnoreCollision(pc.cam.TargetedEnemy.col, false);
    }
    
    #endregion

    #region Launch Up Attack

    [Header("Launch Up Attack")] 
    [SerializeField] private float launchUpHoldTime;
    
    [SerializeField] private float launchUpTime = 0.5f;
    [SerializeField] private float launchUpHeight = 10f;
    
    private void LaunchUp(PlayerController pc, Attack a)
    {
        this.RunSegmentCoroutine(BeginLaunchUp(pc, a));
    }
    
    private IEnumerator<float> BeginLaunchUp(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        KeyBind[] holdKeys = InputManager.GetHoldable(a.keyBinds);
        
        yield return Timing.WaitForSeconds(launchUpHoldTime);
        
        if (!holdKeys.Any(k => InputManager.KeyMap[k].holdAction())) yield break;
        
        pc.pac.PlayAnimation(a.attackClips[1], 0.01f, false);
        
        this.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(Vector3.up, launchUpHeight, launchUpTime));
        
        Timing.WaitForSeconds(a.hitInfo.attackCoolDown);
        
        pc.psm.activateMidairEntry = true;
        
        Timing.WaitForSeconds(launchUpTime - a.hitInfo.attackCoolDown);
        
        pc.rb.linearVelocity = (pc.psm.playerData.jumpHangTimeThreshold - 0.01f) * Vector3.up;

    }
    
    #endregion
    
    #region Plunge Attack
    
    [Header("Plunge Attack")]
    
    [SerializeField] private float plungeSpeed = 10;
    
    private void PlungeAttack(PlayerController pc, Attack a)
    {
        this.RunSegmentCoroutine(BeginPlungeAttack(pc, a));
    }
    
    private IEnumerator<float> BeginPlungeAttack(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        pc.pac.PlayAnimation(a.attackClips[0], 0.01f);
        yield return Timing.WaitForSeconds(a.attackClips[0].length);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = true;
        pc.pac.PlayAnimation(a.attackClips[1], 0.01f);
        
        pc.rb.linearVelocity = Vector3.zero;
        
        float minAnimTime = 0.05f;
        float startTime = Time.time;

        Func<bool> loopCondition = () => Time.time - startTime < minAnimTime || pc.psm.IsMidair &&
            pc.psm.StandardizedMoveDir.normalized.IsInDirectionCone(a.inputDirection.normalized, 92f);
        
        this.RunSegmentCoroutine(pc.rb.TraverseWithVelocity(Vector3.down, plungeSpeed, loopCondition));
        
        yield return Timing.WaitUntilTrue(() => !loopCondition());
        pc.rb.linearVelocity = Vector3.zero;

        if (pc.psm.IsGrounded)
        {
            pc.pac.PlayAnimation(a.attackClips[2], 0.01f);
        }
        
        this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));
    }
    
    #endregion
    
    #region Enemy Step

    [Header("Enemy Step")] [SerializeField]
    private float enemyStepPushBack = 1;
    [SerializeField]
    private float enemyStepTime = 0.3f;
    [SerializeField]
    private float enemyStepHeight = 15f;
    
    
    public void EnemyStep(PlayerController pc, Attack a)
    {
        this.RunSegmentCoroutine(BeginEnemyStep(pc, a));
    }
    
    private IEnumerator<float> BeginEnemyStep(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        Vector3 enemyPos = pc.psm.GetEnemyInRadius(pc.psm.playerData.mediumRadius) != null ? 
            pc.psm.GetEnemyInRadius(pc.psm.playerData.mediumRadius).TargetedPosition() : pc.transform.position;
        Vector3 direction = (pc.transform.position - enemyPos).ZeroVector3Axis().normalized;
        direction = (Vector3.up + direction * enemyStepPushBack).normalized;
        
        pc.rb.linearVelocity = pc.rb.linearVelocity.ZeroVector3Axis();
		
        this.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(direction, enemyStepHeight, enemyStepTime));
    }
    
    
    #endregion

    #region Dodge

    [Header("Dodge")]
    
    [SerializeField] private float dodgeDistance;

    [SerializeField] private float dodgeTime;
    
    private void Dodge(PlayerController pc, Attack a)
    {
        this.RunSegmentCoroutine(BeginDodge(pc, a));
    }
    
    IEnumerator<float> BeginDodge(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        Vector3 dodgeDirection = pc.psm.moveDirection.ZeroVector3Axis().normalized;
        float distance = dodgeDistance;
        float moveTime = dodgeTime;

        if (pc.cam.IsLockedOn)
        {
            if (pc.psm.StandardizedMoveDir.magnitude < 0.1f)
            {
                dodgeDirection = pc.psm.IsMidair ? Vector3.down : Vector3.up;
            }
            else if (pc.psm.StandardizedMoveDir.IsInDirectionCone(new Vector2(0, -1), 92f))
            {
                if (pc.psm.lastInputDir.IsInDirectionCone(new Vector2(0, 1), 92f))
                {
                    if (pc.psm.IsMidair)
                    {
                        Vector3 targetPos = -pc.transform.forward.normalized * dodgeDistance + pc.transform.position;
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
                        distance = dodgeDistance;
                    }
                }
                else
                {
                    Vector3 targetPos = pc.cam.TargetPosition +
                                         (pc.cam.TargetPosition - pc.transform.position).ZeroVector3Axis()
                                         .normalized * (pc.psm.playerData.largeRadius + pc.cam.TargetedEnemy.radius);

                    dodgeDirection = targetPos - pc.transform.position;
                    distance = dodgeDirection.magnitude;
                    dodgeDirection.Normalize();

                    pc.IgnoreCollision(pc.cam.TargetedEnemy.col, true);
                }
            }
            else if (pc.psm.StandardizedMoveDir.IsInDirectionCone(new Vector2(0, 1), 92f))
            {
                Vector3 teleportedPosition = pc.cam.TargetedEnemy.transform.position +
                                             (pc.transform.position - pc.cam.TargetedEnemy.transform.position)
                                             .ZeroVector3Axis().normalized * pc.psm.playerData.mediumRadius;
                dodgeDirection = teleportedPosition - pc.transform.position;
                
                distance = dodgeDirection.magnitude;
                dodgeDirection.Normalize();
                
                moveTime = dodgeTime / 2;
            }
        }
        
        this.RunSegmentCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime));
        
        yield return Timing.WaitForSeconds(moveTime);
        
        if (pc.cam.IsLockedOn) pc.IgnoreCollision(pc.cam.TargetedEnemy.col, false);
    }
    

    #endregion

    #region Mash Attack

    [Header("Mash Attack")] 
    [SerializeField] private float mashInterval;
    [SerializeField] private float mashDuration;
    private void MashAttack(PlayerController pc, Attack a)
    {
        this.RunSegmentCoroutine(BeginMashAttack(pc, a));
    }

    IEnumerator<float> BeginMashAttack(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        float timeSinceLastClick = 0;
        float startTime = Time.time;
        
        pc.psm.pauseComboReset = true;
        
        while (timeSinceLastClick < mashInterval && Time.time - startTime < mashDuration)
        {
            if (InputManager.KeyMap[a.keyBinds[0]].action())
            {
                timeSinceLastClick = 0;
            }
            
            timeSinceLastClick += Time.deltaTime;
            
            yield return Timing.WaitForOneFrame;
        }
        
        
        if (timeSinceLastClick >= mashInterval)
        {
            this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
            pc.pac.PlayAnimation(a.attackClips[1], 0.01f);
        }
        else
        {
            this.RunSegmentCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown * 2, () => pc.psm.pauseComboReset = false));
            pc.pac.PlayAnimation(a.attackClips[2], 0.01f);
        }
    }
    
    #endregion

    #region SpawnVFX

    private void SpawnVFX(PlayerController pc, Attack a)
    {
        this.RunSegmentCoroutine(BeginSpawnVFX(pc, a));
    }
    
    private IEnumerator<float> BeginSpawnVFX(PlayerController pc, Attack a)
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
