using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ExtensionUtils;
using MEC;

public enum OnAttackActions
{
    None,
    DashToTarget,
    LaunchUp,
    PlungeAttack,
    DodgeMove
}

public class ActionEvents : MonoBehaviour
{
    public static ActionEvents Instance { get; private set; }
    
    public static Dictionary<OnAttackActions, Action<PlayerController, Attack>> OnAttackActionMap;
    private static Dictionary<KeyBind, KeyBind> AttackToHoldAttackMap;

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
    
    
    #region OnAttack Instantaneous Actions

    private void AddOnAttackMethods()
    {
        if (OnAttackActionMap != null) return;
        
        OnAttackActionMap = new Dictionary<OnAttackActions, Action<PlayerController, Attack>>();
        
        OnAttackActionMap.Add(OnAttackActions.None, (pc, a) => { });

        OnAttackActionMap.Add(OnAttackActions.DashToTarget, DashToTarget);
        OnAttackActionMap.Add(OnAttackActions.LaunchUp, LaunchUp);
        OnAttackActionMap.Add(OnAttackActions.PlungeAttack, PlungeAttack);
        OnAttackActionMap.Add(OnAttackActions.DodgeMove, Dodge);
        
        
        AttackToHoldAttackMap = new();
        AttackToHoldAttackMap.Add(KeyBind.LightAttack, KeyBind.LightAttackHold);
        AttackToHoldAttackMap.Add(KeyBind.HeavyAttack, KeyBind.HeavyAttackHold);
        AttackToHoldAttackMap.Add(KeyBind.AnyAttack, KeyBind.AnyAttackHold);
    }
    
    #endregion

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
            
            Debug.Log(distance);
            
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
        KeyBind[] holdKeys = GetHoldVersion(a.keyBinds);
        
        yield return Timing.WaitForSeconds(InputManager.Instance.holdTime);
        
        if (!holdKeys.Any(k => InputManager.Instance.KeyMap[k]())) yield break;
        
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
    
    #region Other Methods

    public static KeyBind[] GetHoldVersion(KeyBind[] keys)
    {
        HashSet<KeyBind> holdKeys = new();
        
        foreach (KeyBind key in keys)
        {
            if (AttackToHoldAttackMap.TryGetValue(key, out KeyBind holdKey))
            {
                holdKeys.Add(holdKey);
            }
            
            if (AttackToHoldAttackMap.ContainsValue(key))
            {
                holdKeys.Add(key);
            }
        }
        
        return holdKeys.ToArray();
    }
    
    #endregion

    
}

