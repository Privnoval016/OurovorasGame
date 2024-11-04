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
    PlungeAttack
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
        
        
        AttackToHoldAttackMap = new Dictionary<KeyBind, KeyBind>();
        AttackToHoldAttackMap.Add(KeyBind.LightAttack, KeyBind.LightAttackHold);
        AttackToHoldAttackMap.Add(KeyBind.HeavyAttack, KeyBind.HeavyAttackHold);
        AttackToHoldAttackMap.Add(KeyBind.AnyAttack, KeyBind.AnyAttackHold);
    }
    
    #endregion

    #region Dash Attack
    
    [Header("Dash Attack")]
    
    [SerializeField] private float dashForce = 50;

    private void DashToTarget(PlayerController pc, Attack a)
    {
        GameObject target = pc.cam.targetedEnemy;

        if (target != null)
        {
            Vector3 distance = target.transform.position - pc.transform.position;
            
            pc.transform.LookAt(target.transform);
            
            pc.rb.linearVelocity = pc.rb.linearVelocity.ZeroVector3Axis();
            
            pc.rb.AddForce(distance.normalized * dashForce, ForceMode.Impulse);
        }
    }
    
    #endregion

    #region Launch Up Attack
    
    [Header("Launch Up Attack")]
    
    [SerializeField] private float launchUpForce = 50;
    
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
        pc.rb.AddForce(Vector3.up * launchUpForce, ForceMode.Impulse);
        
        yield return Timing.WaitUntilTrue(() => pc.canAttack);
        
        pc.rb.linearVelocity = pc.rb.linearVelocity.ZeroVector3Axis();
        pc.gravityScale = pc.playerData.gravityScale;
        
    }
    
    #endregion
    
    #region Plunge Attack
    
    [Header("Plunge Attack")]
    
    [SerializeField] private float plungeForce = 50;
    
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

