using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using ExtensionUtils;
using MEC;
using UnityEngine;

public enum OnAttackActions
{
    None,
    DashToTarget,
    LaunchUp,
    PlungeAttack,
    DodgeMove,
    FloorDash,
    MashAttack
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
        
        
    }
    
    IEnumerator<float> ResumeMoving(PlayerController pc, Attack a, float time, Action action = null)
    {
        yield return Timing.WaitForSeconds(time);
        
        if (action != null) action();
        pc.canAttack = true;
    }
    
    
    #region Air Dash

    [Header("Air Dash")]
    [SerializeField] private float dashSpeed;
    
    private void DashToTarget(PlayerController pc, Attack a)
    {
        Timing.RunCoroutine(AirDash(pc, a));
    }

    IEnumerator<float> AirDash(PlayerController pc, Attack a)
    {
        GameObject target = pc.cam.targetedEnemy;
        
        pc.PlayAnimation(a.attackClips[0], 0.01f);
        yield return Timing.WaitForSeconds(a.attackClips[0].length);
        
        Quaternion originalRotation = pc.animancer.gameObject.transform.rotation;
        
        pc.animancer.gameObject.transform.LookAt(target.transform.position);
        pc.PlayAnimation(a.attackClips[1], 0.01f);
       
        pc.rb.linearVelocity = Vector3.zero;
        float minAnimTime = 0.05f;
        float startTime = Time.time;
        
        Func<bool> exitCondition = () => Time.time - startTime < minAnimTime || pc.IsMidair &&
            pc.StandardizedMoveDir.normalized != Vector2.zero &&
            Vector2.Dot(pc.StandardizedMoveDir.normalized, a.inputDirection.normalized) > 0.69f;
        Vector3 direction = target.transform.position - pc.transform.position;
        
        if (target.TryGetComponent(out Collider c)) Physics.IgnoreCollision(pc.col, c, true);
        
        yield return Timing.WaitUntilDone(GameManager.TraverseWithVelocity(pc.rb, direction.normalized, dashSpeed, exitCondition), Segment.FixedUpdate);
        
        pc.rb.linearVelocity = Vector3.zero;

        pc.PlayAnimation(a.attackClips[2], 0.01f);
        Timing.RunCoroutine(ResumeMoving(pc, a, a.attackClips[2].length));
        
        pc.animancer.gameObject.transform.rotation = originalRotation;
        if (target.TryGetComponent(out Collider c2)) Physics.IgnoreCollision(pc.col, c2, false);
        
    }
    
    #endregion

    #region Dash Attack
    
    [Header("Dash Attack")]
    
    [SerializeField] private float dashDistance;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashTimeThreshold = 0.4f;

    private void FloorDash(PlayerController pc, Attack a)
    {
        Timing.RunCoroutine(DashAttack(pc, a));
    }

    IEnumerator<float> DashAttack(PlayerController pc, Attack a)
    {
        GameObject target = pc.cam.targetedEnemy;
        
        if (target == null) yield break;
        
        
        KeyBind[] releaseKeys = InputManager.GetReleaseable(a.keyBinds);
        
        float startTime = Time.time;
        
        yield return Timing.WaitUntilTrue(() => releaseKeys.Any(k => InputManager.KeyMap[k].releaseAction()));
        
        float elapsedTime = Time.time - startTime;
        
        
        pc.ExitTimeAnimation(a.attackClips[1], a.attackClips[2]);


        Vector3 direction = (target.transform.position - pc.transform.position).WithY(Mathf.Min(target.transform.position.y, pc.transform.position.y));
        pc.transform.LookAt(target.transform.position.WithY(pc.transform.position.y));
        
        float distance;
        RaycastHit[] collidersInPath = null;

        if (elapsedTime >= dashTimeThreshold)
        {
            distance = Mathf.Max(Mathf.Min(dashDistance * 2, direction.magnitude * 2), dashDistance * 0.7f);

            collidersInPath = Physics.SphereCastAll(pc.transform.position, pc.col.radius, direction, distance * 2f, pc.enemyLayer);

            foreach (var hit in collidersInPath)
            {
                Physics.IgnoreCollision(pc.col, hit.collider, true);
            }
            if (target.TryGetComponent(out Collider c)) Physics.IgnoreCollision(pc.col, c, true);
        }
        else
        {
            distance = Mathf.Min(dashDistance, direction.magnitude);
        }
        
        yield return Timing.WaitUntilDone(GameManager.TraverseDistanceInTime(pc.rb, direction.normalized, distance, dashTime), Segment.FixedUpdate);
        

        pc.PlayAnimation(a.attackClips[3], 0.01f);
        
        Timing.RunCoroutine(ResumeMoving(pc, a, a.attackCoolDown));
        
        if (collidersInPath != null)
        {
            foreach (var hit in collidersInPath)
            {
                Physics.IgnoreCollision(pc.col, hit.collider, false);
            }
        }
        if (target.TryGetComponent(out Collider c2)) Physics.IgnoreCollision(pc.col, c2, false);
    }
    
    #endregion

    #region Launch Up Attack

    [Header("Launch Up Attack")] 
    [SerializeField] private float launchUpForce = 40f;
    
    [SerializeField] private float launchUpHoldTime;
    
    private void LaunchUp(PlayerController pc, Attack a)
    {
        Timing.RunCoroutine(BeginLaunchUp(pc, a));
    }
    
    private IEnumerator<float> BeginLaunchUp(PlayerController pc, Attack a)
    {
        KeyBind[] holdKeys = InputManager.GetHoldable(a.keyBinds);
        
        yield return Timing.WaitForSeconds(launchUpHoldTime);
        
        if (!holdKeys.Any(k => InputManager.KeyMap[k].holdAction())) yield break;
        
        pc.PlayAnimation(a.attackClips[1], 0.01f);

        float force = launchUpForce;

        Debug.Log(force + " a");
        if (pc.rb.linearVelocity.y < 0)
            force -= pc.rb.linearVelocity.y;
		
        pc.rb.AddForce(Vector3.up * force * pc.rb.mass, ForceMode.Impulse);
        pc.isJumping = true;
        
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
        pc.PlayAnimation(a.attackClips[0], 0.01f);
        yield return Timing.WaitForSeconds(a.attackClips[0].length);
        
        pc.PlayAnimation(a.attackClips[1], 0.01f);
        
        pc.rb.linearVelocity = Vector3.zero;
        
        float minAnimTime = 0.05f;
        float startTime = Time.time;

        Func<bool> exitCondition = () => Time.time - startTime < minAnimTime || pc.IsMidair &&
            pc.StandardizedMoveDir.normalized.IsInDirectionCone(a.inputDirection.normalized, 92f);
        
        yield return Timing.WaitUntilDone(GameManager.TraverseWithVelocity(pc.rb, Vector3.down, plungeSpeed, exitCondition), Segment.FixedUpdate);
        
        pc.rb.linearVelocity = Vector3.zero;

        if (pc.IsGrounded)
        {
            pc.PlayAnimation(a.attackClips[2], 0.01f);
        }
        else
        {
            pc.PlayAnimation(pc.moveAnimData.fallClip.LoopClip);
        }
        
        Timing.RunCoroutine(ResumeMoving(pc, a, a.attackCoolDown));
    }
    
    #endregion

    #region Dodge

    [Header("Dodge")]
    
    [SerializeField] private float dodgeDistance;
    [SerializeField] private float dodgeTime = 0.2f;
    
    private void Dodge(PlayerController pc, Attack a)
    {
        Vector3 dodgeDirection = pc.moveDirection.ZeroVector3Axis().normalized;
        float distance = dodgeDistance;
        

        if (pc.cam.isLockedOn && pc.StandardizedMoveDir.magnitude < 0.1f)
        {
            Vector3 teleportedPosition = pc.cam.targetedEnemy.transform.position + 
                                         (pc.transform.position - pc.cam.targetedEnemy.transform.position).ZeroVector3Axis().normalized * pc.itsCalledAuraBro;
            dodgeDirection = teleportedPosition - pc.transform.position;
            
            distance = dodgeDirection.magnitude;
            dodgeDirection.Normalize();
        }
        else if (pc.StandardizedMoveDir.magnitude < 0.1f)
        {
            dodgeDirection = pc.IsMidair ? Vector3.down : Vector3.up;
        }
        else if (pc.StandardizedMoveDir.IsInDirectionCone(new Vector2(0, -1), 92f) && pc.IsMidair && pc.cam.isLockedOn)
        {
            Vector3 targetPos = dodgeDirection * dodgeDistance + pc.transform.position;
            
            bool isGround = Physics.Raycast(targetPos, Vector3.down, out RaycastHit hit, 100f, pc.groundLayer);
            
            Vector3 point = isGround ? hit.point : targetPos;
            
            dodgeDirection = point - pc.transform.position;
            distance = dodgeDirection.magnitude;
            dodgeDirection.Normalize();
        }

        
        Timing.RunCoroutine(GameManager.TraverseDistanceInTime(pc.rb, dodgeDirection, distance, dodgeTime), Segment.FixedUpdate);
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
        float timeSinceLastClick = 0;
        float startTime = Time.time;
        
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
            Timing.RunCoroutine(ResumeMoving(pc, a, a.attackCoolDown));
            pc.PlayAnimation(a.attackClips[1], 0.01f);
        }
        else
        {
            Timing.RunCoroutine(ResumeMoving(pc, a, a.attackCoolDown * 2));
            pc.PlayAnimation(a.attackClips[2], 0.01f);
        }
    }



    #endregion
}
