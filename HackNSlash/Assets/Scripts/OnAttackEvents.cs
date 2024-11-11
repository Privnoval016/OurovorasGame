using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionUtils;
using MEC;
using UnityEngine;

public enum OnAttackActions
{
    None,
    DashToTarget,
    LaunchUp,
    PlungeAttack,
    DodgeMove
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
        
    }

    #region Dash Attack
    
    [Header("Dash Attack")]
    
    [SerializeField] private float dashForce;

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

    #region Launch Up Attack
    
    [Header("Launch Up Attack")]
    
    [SerializeField] private float launchUpHeight;

    [SerializeField] private float launchUpTimeToApex;
    
    private void LaunchUp(PlayerController pc, Attack a)
    {
        Timing.RunCoroutine(BeginLaunchUp(pc, a));
    }
    
    private IEnumerator<float> BeginLaunchUp(PlayerController pc, Attack a)
    {
        KeyBind[] holdKeys = InputManager.GetHoldVersion(a.keyBinds);
        
        yield return Timing.WaitForSeconds(InputManager.Instance.holdTime);
        
        if (!holdKeys.Any(k => InputManager.KeyMap[k]())) yield break;
        
        pc.PlayAnimationClip(a.attackClips[1], 0.01f);
        
        float gravityStrength = -(2 * launchUpHeight) / 
                                (launchUpTimeToApex * launchUpTimeToApex);
        
        float jumpForce = Mathf.Abs(gravityStrength) * launchUpTimeToApex;

        float force = jumpForce;
        if (pc.rb.linearVelocity.y < 0)
            force -= pc.rb.linearVelocity.y;
		
        pc.rb.AddForce(Vector3.up * force, ForceMode.Impulse);
        pc.isJumping = true;
        
    }
    
    #endregion
    
    #region Plunge Attack
    
    [Header("Plunge Attack")]
    
    [SerializeField] private float plungeForce;
    
    private void PlungeAttack(PlayerController pc, Attack a)
    {
        Timing.RunCoroutine(BeginPlungeAttack(pc, a));
    }
    
    private IEnumerator<float> BeginPlungeAttack(PlayerController pc, Attack a)
    {
        yield return Timing.WaitForSeconds(a.attackClips[0].length);
        
        pc.rb.AddForce(Vector3.down * plungeForce, ForceMode.Impulse);
    }
    
    #endregion

    #region Dodge

    [Header("Dodge")]
    
    [SerializeField] private float dodgeForce;
    [SerializeField] private float dodgeDistance;
    
    private void Dodge(PlayerController pc, Attack a)
    {
        Debug.Log("Dodge");
        Timing.RunCoroutine(BeginDodge(pc, a));
    }

    private IEnumerator<float> BeginDodge(PlayerController pc, Attack a)
    {
        // dodge in the direction of movement relative to the camera
        Vector3 dodgeDirection = pc.moveDirection.ZeroVector3Axis().normalized;
        
        Vector3 startPosition = pc.transform.position;
        
        if (dodgeDirection == Vector3.zero)
        {
            dodgeDirection = pc.IsMidair ? Vector3.down : Vector3.up;
        }
        
        pc.rb.AddForce(dodgeDirection * dodgeForce, ForceMode.Impulse);
        
        Debug.Log(dodgeDirection);
        
        yield return Timing.WaitUntilTrue(() => Vector3.Distance(startPosition, pc.transform.position) >= dodgeDistance);
        
        pc.rb.linearVelocity = Vector3.zero;
    }
    

    #endregion
}
