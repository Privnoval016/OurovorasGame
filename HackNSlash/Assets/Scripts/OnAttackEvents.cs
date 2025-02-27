using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Animancer;
using ExtensionUtils;
using MEC;
using UnityEngine;
using PrimeTween;
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
    VortexSlash,
    EnemyStep
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
        OnAttackActionMap.Add(OnAttackActions.VortexSlash, VortexSlash);
        OnAttackActionMap.Add(OnAttackActions.EnemyStep, EnemyStep);
        
        
    }
    
    IEnumerator<float> ResumeMoving(PlayerController pc, Attack a, float time, Action action = null)
    {
        yield return Timing.WaitForSeconds(time);
        if (action != null) action();
        pc.psm.canAttack = true;
    }
    
    public void EndObjectCoroutines()
    {
        Timing.KillCoroutines(gameObject);
    }
    
    #region Blade Beam
    
    [Header("Blade Beam")]
    [SerializeField] private float bladeBeamHoldTime;
    [SerializeField] private float crossSlashDuration = 1.3f;
    [SerializeField] private float hitboxDelay = 0.4f;
    
    private void BladeBeam(PlayerController pc, Attack a)
    {
        Timing.RunCoroutine(BeginBladeBeam(pc, a));
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

        if (elapsedTime >= bladeBeamHoldTime && pc.cam.isLockedOn)
        {
            pc.pac.PlayAnimation(a.attackClips[2], 0.1f);
            
            Vector3 targetPos = pc.cam.targetedEnemy.transform.position;
            
            Quaternion targetRot = Quaternion.LookRotation(pc.transform.position - targetPos);
            
            VFXController vfx = OnVFXEvents.Instance.InvokeOnVFX(pc, new TransformInfo(targetPos, targetRot, Vector3.one), a, 2);
            
            Timing.RunCoroutine(ResumeMoving(pc, a, crossSlashDuration, () => pc.psm.pauseComboReset = false));
            
            vfx.activeHitbox = false;
            yield return Timing.WaitForSeconds(hitboxDelay);
            vfx.activeHitbox = true;
        }
        else
        {
            pc.pac.PlayAnimation(a.attackClips[1], 0.1f);
            
            Vector3 startPos = pc.transform.forward.FindRadialVector3(pc.playerRadius, 0) + pc.transform.position;
            Quaternion startRot = pc.transform.rotation;
            if (pc.cam.isLockedOn)
                startRot = Quaternion.LookRotation(pc.cam.targetedEnemy.transform.position - pc.transform.position);
            if (a.vfxInfos[1].rotation.eulerAngles != Vector3.zero)
            {
                startRot *= a.vfxInfos[1].rotation;
            }
            
            OnVFXEvents.Instance.InvokeOnVFX(pc, new TransformInfo(startPos, startRot, Vector3.one), a, 1);
            
            Timing.RunCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
            
            
        }
        
        
    }
    
    #endregion
    
    
    #region Vortex Slash
    
    [Header("Vortex Slash")]
    [SerializeField] private float vortexSlashHoldTime;
    
    private void VortexSlash(PlayerController pc, Attack a)
    {
        Timing.RunCoroutine(BeginVortexSlash(pc, a));
    }
    
    private IEnumerator<float> BeginVortexSlash(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        pc.psm.pauseComboReset = true;
        pc.psm.TurnToLook();
        
        KeyBind[] holdKeys = InputManager.GetReleaseable(a.keyBinds);
        float startTime = Time.time;
        yield return Timing.WaitUntilTrue(() => holdKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
        float elapsedTime = Time.time - startTime;

        int index = elapsedTime >= vortexSlashHoldTime ? 2 : 1;

        Timing.WaitUntilDone(GameUtil.RunAfterDelay(index == 2 ? a.vfxInfos[index].delay : 0, () => pc.pac.PlayAnimation(a.attackClips[index], 0.01f)));
        
        
        
        Vector3 startPos = pc.transform.forward.FindRadialVector3(pc.playerRadius, 0) + pc.transform.position;
        Quaternion startRot = pc.transform.rotation;
        if (pc.cam.isLockedOn)
            startRot = Quaternion.LookRotation((pc.cam.targetedEnemy.transform.position - pc.transform.position).ZeroVector3Axis());
        
        if (a.vfxInfos[index].rotation.eulerAngles != Vector3.zero)
        {
            startRot *= a.vfxInfos[index].rotation;
        }
            
        OnVFXEvents.Instance.InvokeOnVFX(pc, new TransformInfo(startPos, startRot, Vector3.one), a, index);
        
            
        Timing.RunCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
    }
    
    #endregion
    
    
    #region Air Dash

    [FormerlySerializedAs("dashSpeed")]
    [Header("Air Dash")]
    [SerializeField] private float airDashSpeed;

    [FormerlySerializedAs("maxDashDistance")] [SerializeField] private float maxAirDashDistance = 30f;
    
    private void DashToTarget(PlayerController pc, Attack a)
    {
        Timing.RunCoroutine(AirDash(pc, a));
    }

    IEnumerator<float> AirDash(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        GameObject target = pc.cam.targetedEnemy;
        
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
        Vector3 direction = target.transform.position - pc.transform.position;

        if (direction.y > 0)
        {
            direction = direction.ZeroVector3Axis();
            direction = direction.Rotate(-45, Vector3.Cross(direction, Vector3.up));
        }


        pc.pac.animancer.gameObject.transform.LookAt(pc.transform.position + direction);
        
        if (target.TryGetComponent(out Collider c)) pc.IgnoreCollision(c, true);
        
        Timing.RunCoroutine(pc.rb.TraverseWithVelocity(direction.normalized, airDashSpeed, loopCondition));
        yield return Timing.WaitUntilTrue(() => !loopCondition());
        
        pc.rb.linearVelocity = Vector3.zero;

        pc.pac.PlayAnimation(a.attackClips[2], 0.01f);
        Timing.RunCoroutine(ResumeMoving(pc, a, a.attackClips[2].length));
        
        pc.pac.animancer.gameObject.transform.rotation = originalRotation;
        if (target.TryGetComponent(out Collider c2)) pc.IgnoreCollision(c2, false);
        
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
        Timing.RunCoroutine(DashAttack(pc, a));
    }

    IEnumerator<float> DashAttack(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        GameObject target = pc.cam.targetedEnemy;
        
        if (target == null) yield break;
        
        ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = false;
        
        KeyBind[] releaseKeys = InputManager.GetReleaseable(a.keyBinds);
        
        float startTime = Time.time;
        
        yield return Timing.WaitUntilTrue(() => releaseKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
        
        float elapsedTime = Time.time - startTime;
        
        
        pc.pac.ExitTimeAnimation(a.attackClips[1], a.attackClips[2]);


        Vector3 direction = (target.transform.position - pc.transform.position).
            WithY(Mathf.Min(target.transform.position.y, pc.transform.position.y) - pc.transform.position.y);
        pc.transform.LookAt(target.transform.position.WithY(pc.transform.position.y));
        
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
            if (target.TryGetComponent(out Collider c)) pc.IgnoreCollision(c, true);
        }
        else
        {
            ((PlayerAttacking) pc.sc.GetCurrentState()).readyToHit = true;
            distance = Mathf.Min(groundDashDistance, direction.magnitude);
        }
        
        yield return Timing.WaitUntilDone(pc.rb.TraverseDistanceInTime(direction.normalized, distance, groundDashTime));
        

        pc.pac.PlayAnimation(a.attackClips[3], 0.01f);
        
        
        Timing.RunCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));
        
        if (collidersInPath != null)
        {
            foreach (var hit in collidersInPath)
            {
                if (hit.collider.TryGetComponent(out IDamageable d)) d.OnHit(pc, a, 1);
                pc.IgnoreCollision(hit.collider, false);
            }
        }
        if (target.TryGetComponent(out Collider c2)) pc.IgnoreCollision(c2, false);
    }
    
    #endregion

    #region Launch Up Attack

    [Header("Launch Up Attack")] 
    [SerializeField] private float launchUpHoldTime;
    
    [SerializeField] private float launchUpTime = 0.5f;
    [SerializeField] private float launchUpHeight = 10f;
    
    private void LaunchUp(PlayerController pc, Attack a)
    {
        Timing.RunCoroutine(BeginLaunchUp(pc, a));
    }
    
    private IEnumerator<float> BeginLaunchUp(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        KeyBind[] holdKeys = InputManager.GetHoldable(a.keyBinds);
        
        yield return Timing.WaitForSeconds(launchUpHoldTime);
        
        if (!holdKeys.Any(k => InputManager.KeyMap[k].holdAction())) yield break;
        
        pc.pac.PlayAnimation(a.attackClips[1], 0.01f, false);
        
        Timing.RunCoroutine(pc.rb.TraverseDistanceInTime(Vector3.up, launchUpHeight, launchUpTime));
        
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
        Timing.RunCoroutine(BeginPlungeAttack(pc, a));
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
        
        Timing.RunCoroutine(pc.rb.TraverseWithVelocity(Vector3.down, plungeSpeed, loopCondition));
        
        yield return Timing.WaitUntilTrue(() => !loopCondition());
        pc.rb.linearVelocity = Vector3.zero;

        if (pc.psm.IsGrounded)
        {
            pc.pac.PlayAnimation(a.attackClips[2], 0.01f);
        }
        
        Timing.RunCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown));
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
        Timing.RunCoroutine(BeginEnemyStep(pc, a));
    }
    
    private IEnumerator<float> BeginEnemyStep(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        Vector3 enemyPos = pc.psm.GetEnemyInRadius(pc.playerRadius) != null ? 
            pc.psm.GetEnemyInRadius(pc.playerRadius).transform.position : pc.transform.position;
        Vector3 direction = (pc.transform.position - enemyPos).ZeroVector3Axis().normalized;
        direction = (Vector3.up + direction * enemyStepPushBack).normalized;
        
        pc.rb.linearVelocity = pc.rb.linearVelocity.ZeroVector3Axis();
        
        Debug.Log(direction + " " + enemyStepHeight + " " + enemyStepTime);
		
        Timing.RunCoroutine(pc.rb.TraverseDistanceInTime(direction, enemyStepHeight, enemyStepTime));
    }
    
    
    #endregion

    #region Dodge

    [Header("Dodge")]
    
    [SerializeField] private float dodgeDistance;

    [SerializeField] private float dodgeTime;
    
    private void Dodge(PlayerController pc, Attack a)
    {
        Timing.RunCoroutine(BeginDodge(pc, a));
    }
    
    IEnumerator<float> BeginDodge(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.animDelay);
        
        Vector3 dodgeDirection = pc.psm.moveDirection.ZeroVector3Axis().normalized;
        float distance = dodgeDistance;
        float moveTime = dodgeTime;
        

        if (pc.psm.StandardizedMoveDir.IsInDirectionCone(new Vector2(0, -1), 92f) && pc.psm.IsMidair && pc.cam.isLockedOn)
        {
            Vector3 targetPos = dodgeDirection * dodgeDistance + pc.transform.position;
            
            bool isGround = Physics.Raycast(targetPos, Vector3.down, out RaycastHit hit, 100f, pc.psm.groundLayer);
            
            Vector3 point = isGround ? hit.point : targetPos;
            
            dodgeDirection = point - pc.transform.position;
            distance = dodgeDirection.magnitude;
            dodgeDirection.Normalize();
        }
        else if (pc.cam.isLockedOn && pc.psm.StandardizedMoveDir.IsInDirectionCone(new Vector2(0, -1), 92f))
        {
            dodgeDirection = -pc.transform.forward.ZeroVector3Axis();
            dodgeDirection.Normalize();
        }
        else if (pc.cam.isLockedOn && (pc.psm.StandardizedMoveDir.magnitude < 0.1f || pc.psm.StandardizedMoveDir.IsInDirectionCone(new Vector2(0, 1), 92f)))
        {
            Vector3 teleportedPosition = pc.cam.targetedEnemy.transform.position + 
                                         (pc.transform.position - pc.cam.targetedEnemy.transform.position).ZeroVector3Axis().normalized * pc.playerRadius +
                                         Vector3.up * pc.playerRadius;
            dodgeDirection = teleportedPosition - pc.transform.position;
            
            distance = dodgeDirection.magnitude;
            dodgeDirection.Normalize();
            
            moveTime = dodgeTime / 2;
        }
        else if (pc.psm.StandardizedMoveDir.magnitude < 0.1f)
        {
            dodgeDirection = pc.psm.IsMidair ? Vector3.down : Vector3.up;
        }

        
        Timing.RunCoroutine(pc.rb.TraverseDistanceInTime(dodgeDirection, distance, moveTime), Segment.FixedUpdate);
    }
    

    #endregion

    #region Mash Attack

    [Header("Mash Attack")] 
    [SerializeField] private float mashInterval;
    [SerializeField] private float mashDuration;
    private void MashAttack(PlayerController pc, Attack a)
    {
        Timing.RunCoroutine(BeginMashAttack(pc, a));
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
            Timing.RunCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown, () => pc.psm.pauseComboReset = false));
            pc.pac.PlayAnimation(a.attackClips[1], 0.01f);
        }
        else
        {
            Timing.RunCoroutine(ResumeMoving(pc, a, a.hitInfo.attackCoolDown * 2, () => pc.psm.pauseComboReset = false));
            pc.pac.PlayAnimation(a.attackClips[2], 0.01f);
        }
    }



    #endregion
}
