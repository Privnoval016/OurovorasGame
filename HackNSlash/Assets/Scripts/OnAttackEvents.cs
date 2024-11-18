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
    FloorDash
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
        
    }
    
    IEnumerator<float> ResumeMoving(PlayerController pc, Attack a, float time)
    {
        yield return Timing.WaitForSeconds(time);
        
        pc.canAttack = true;
    }
    
    #region Air Dash

    [SerializeField] private float dashForce = 50;
    
    private void DashToTarget(PlayerController pc, Attack a)
    {
        GameObject target = pc.cam.targetedEnemy;

        if (target != null)
        {
            Vector3 distance = target.transform.position - pc.transform.position;
            distance.Normalize();
            
            pc.transform.LookAt(target.transform);
            
            pc.rb.linearVelocity = pc.rb.linearVelocity.ZeroVector3Axis();
            
            pc.rb.AddForce(distance * dashForce, ForceMode.Impulse);
        }
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
        
        
        KeyBind[] releaseKeys = InputManager.GetReleaseVersion(a.keyBinds);
        
        float startTime = Time.time;
        
        yield return Timing.WaitUntilTrue(() => releaseKeys.Any(k => InputManager.KeyMap[k]()));
        
        float elapsedTime = Time.time - startTime;
        
        
        pc.ExitTimeAnimation(a.attackClips[1], a.attackClips[2]);


        Vector3 direction = (target.transform.position - pc.transform.position).WithY(Mathf.Min(target.transform.position.y, pc.transform.position.y));
        pc.transform.LookAt(target.transform.position.WithY(pc.transform.position.y));
        
        float distance;
        RaycastHit[] collidersInPath = null;

        if (elapsedTime >= dashTimeThreshold)
        {
            distance = dashDistance * 2;

            collidersInPath = Physics.SphereCastAll(pc.transform.position, pc.col.radius, direction, distance * 1.2f, pc.enemyLayer);

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
        
        Timing.RunCoroutine(ResumeMoving(pc, a, dashTime + a.attackClips[3].length));
        
        yield return Timing.WaitUntilDone(GameManager.TraverseDistanceInTime(pc.rb, direction.normalized, distance, dashTime), Segment.FixedUpdate);
        

        pc.ExitTimeAnimation(a.attackClips[3], null, () => pc.canAttack = true);
        
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
        KeyBind[] holdKeys = InputManager.GetHoldVersion(a.keyBinds);
        
        yield return Timing.WaitForSeconds(launchUpHoldTime);
        
        if (!holdKeys.Any(k => InputManager.KeyMap[k]())) yield break;
        
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
        
        while (Time.time - startTime < minAnimTime || pc.IsMidair && 
               pc.StandardizedMoveDir.normalized != Vector2.zero && Vector2.Dot(pc.StandardizedMoveDir.normalized, a.inputDirection.normalized) > 0.69f)
        {
            pc.rb.linearVelocity = Vector3.down * plungeSpeed;
            
            yield return Timing.WaitForOneFrame;
        }
        
        pc.rb.linearVelocity = Vector3.zero;

        if (pc.IsGrounded)
        {
            pc.PlayAnimation(a.attackClips[2], 0.01f);

            Timing.RunCoroutine(ResumeMoving(pc, a, a.attackClips[2].length));
        }
        else
        {
            pc.PlayAnimation(pc.moveAnimData.fallClip.LoopClip);
            Timing.RunCoroutine(ResumeMoving(pc, a, 0.1f));
        }
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
        
        if (pc.StandardizedMoveDir.magnitude < 0.1f)
        {
            if (pc.cam.isLockedOn)
            {
                Vector3 teleportedPosition = pc.cam.targetedEnemy.transform.position + 
                                             (pc.transform.position - pc.cam.targetedEnemy.transform.position).ZeroVector3Axis().normalized * pc.itsCalledAuraBro;
                dodgeDirection = teleportedPosition - pc.transform.position;
                
                distance = dodgeDirection.magnitude;
                dodgeDirection.Normalize();
            }
            else
            {
                dodgeDirection = pc.IsMidair ? Vector3.down : Vector3.zero;
            }
        }
        
        Timing.RunCoroutine(GameManager.TraverseDistanceInTime(pc.rb, dodgeDirection, distance, dodgeTime), Segment.FixedUpdate);
    }
    

    #endregion
}
