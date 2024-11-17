using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    
    #region Input Actions Instances
    private PlayerInputActions playerInputActions;
    
    public InputAction movement;
    public InputAction cameraMove;
    
    public InputAction jump;
    public InputAction dodge;
    
    public InputAction lockOn;
    public InputAction retarget;

    public InputAction lightAttack;
    public InputAction heavyAttack;

    private bool lightAttacking, heavyAttacking;
    private float lightAttackHoldTime, heavyAttackHoldTime;
    private float AnyAttackHoldTime => Mathf.Max(lightAttackHoldTime, heavyAttackHoldTime);
    
    #endregion

    public static Dictionary<KeyBind, Func<bool>> KeyMap = new();
    public static Dictionary<KeyBind, KeyBind> AttackToHoldAttackMap = new();
    public static Dictionary<KeyBind, KeyBind> AttackToReleaseAttackMap = new();
    
    public float holdTime;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        KeyMap.Add(KeyBind.None, () => true);

        playerInputActions = new PlayerInputActions();
        movement = playerInputActions.Player.Move;


        cameraMove = playerInputActions.Player.Camera;


        jump = playerInputActions.Player.Jump;

        dodge = playerInputActions.Player.Dodge;
        KeyMap.Add(KeyBind.Dodge, () => dodge.triggered);


        lockOn = playerInputActions.Player.LockOn;

        retarget = playerInputActions.Player.Retarget;


        lightAttack = playerInputActions.Player.LightAttack;
        lightAttack.performed += ctx => lightAttacking = true;
        lightAttack.canceled += ctx => lightAttacking = false;
        KeyMap.Add(KeyBind.LightAttack, () => lightAttack.triggered);
        KeyMap.Add(KeyBind.LightAttackRelease, () => lightAttack.WasReleasedThisFrame());
        KeyMap.Add(KeyBind.LightAttackHold, () => lightAttacking);
        
        heavyAttack = playerInputActions.Player.HeavyAttack;
        heavyAttack.performed += ctx => heavyAttacking = true;
        heavyAttack.canceled += ctx => heavyAttacking = false;
        KeyMap.Add(KeyBind.HeavyAttack, () => heavyAttack.triggered);
        KeyMap.Add(KeyBind.HeavyAttackRelease, () => heavyAttack.WasReleasedThisFrame());
        KeyMap.Add(KeyBind.HeavyAttackHold, () => heavyAttacking);
        
        KeyMap.Add(KeyBind.AnyAttack, () => KeyMap[KeyBind.LightAttack]() || KeyMap[KeyBind.HeavyAttack]());
        KeyMap.Add(KeyBind.AnyAttackRelease, () => KeyMap[KeyBind.LightAttackRelease]() || KeyMap[KeyBind.LightAttackRelease]());
        KeyMap.Add(KeyBind.AnyAttackHold, () => lightAttacking || heavyAttacking);
        
        
        playerInputActions.Player.Enable();
        
        
        AttackToHoldAttackMap.Add(KeyBind.LightAttack, KeyBind.LightAttackHold);
        AttackToHoldAttackMap.Add(KeyBind.HeavyAttack, KeyBind.HeavyAttackHold);
        AttackToHoldAttackMap.Add(KeyBind.AnyAttack, KeyBind.AnyAttackHold);
        
        AttackToReleaseAttackMap.Add(KeyBind.LightAttack, KeyBind.LightAttackRelease);
        AttackToReleaseAttackMap.Add(KeyBind.HeavyAttack, KeyBind.HeavyAttackRelease);
        AttackToReleaseAttackMap.Add(KeyBind.AnyAttack, KeyBind.AnyAttackRelease);
    }
    
    void Update()
    {
        lightAttackHoldTime = lightAttacking ? lightAttackHoldTime + Time.deltaTime : 0;
        heavyAttackHoldTime = heavyAttacking ? heavyAttackHoldTime + Time.deltaTime : 0;
    }

    public void ReleaseHoldAttacks()
    {
        lightAttacking = false;
        heavyAttacking = false;
    }
    
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
    
    public static KeyBind[] GetReleaseVersion(KeyBind[] keys)
    {
        HashSet<KeyBind> releaseKeys = new();
        
        foreach (KeyBind key in keys)
        {
            if (AttackToReleaseAttackMap.TryGetValue(key, out KeyBind releaseKey))
            {
                releaseKeys.Add(releaseKey);
            }
            
            if (AttackToReleaseAttackMap.ContainsValue(key))
            {
                releaseKeys.Add(key);
            }
        }
        
        return releaseKeys.ToArray();
    }
    
    #endregion
}



public enum KeyBind
{
    None,
    LightAttack,
    LightAttackHold,
    HeavyAttack,
    HeavyAttackHold,
    AnyAttack,
    AnyAttackHold,
    Dodge,
    LightAttackRelease,
    HeavyAttackRelease,
    AnyAttackRelease
}
