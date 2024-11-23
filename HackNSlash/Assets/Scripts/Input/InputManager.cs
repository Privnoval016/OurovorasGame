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
    
    #endregion
    
    public static readonly Dictionary<KeyBind, KeyBindData> KeyMap = new();

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

        playerInputActions = new PlayerInputActions();
        movement = playerInputActions.Player.Move;
        cameraMove = playerInputActions.Player.Camera;
        jump = playerInputActions.Player.Jump;
        dodge = playerInputActions.Player.Dodge;
        lockOn = playerInputActions.Player.LockOn;
        retarget = playerInputActions.Player.Retarget;

        KeyMap.Add(KeyBind.None, new KeyBindData() {action = () => true});

        lightAttack = playerInputActions.Player.LightAttack;
        lightAttack.performed += ctx => lightAttacking = true;
        lightAttack.canceled += ctx => lightAttacking = false;
        
        KeyMap.Add(KeyBind.LightAttack, new KeyBindData
        {
            action = () => lightAttack.triggered,
            holdAction = () => lightAttacking,
            releaseAction = () => lightAttack.WasReleasedThisFrame(),
        });
        
        heavyAttack = playerInputActions.Player.HeavyAttack;
        heavyAttack.performed += ctx => heavyAttacking = true;
        heavyAttack.canceled += ctx => heavyAttacking = false;
        
        KeyMap.Add(KeyBind.HeavyAttack, new KeyBindData
        {
            action = () => heavyAttack.triggered,
            holdAction = () => heavyAttacking,
            releaseAction = () => heavyAttack.WasReleasedThisFrame(),
        });
        
        KeyMap.Add(KeyBind.AnyAttack, new KeyBindData
        {
            action = () => KeyMap[KeyBind.LightAttack].action() || KeyMap[KeyBind.HeavyAttack].action(),
            holdAction = () => KeyMap[KeyBind.LightAttack].holdAction() || KeyMap[KeyBind.HeavyAttack].holdAction(),
            releaseAction = () => KeyMap[KeyBind.LightAttack].releaseAction() || KeyMap[KeyBind.HeavyAttack].releaseAction(),
        });
        
        KeyMap.Add(KeyBind.Dodge, new KeyBindData {action = () => dodge.triggered});
        
        playerInputActions.Player.Enable();
    }
    
    private void Update()
    {
        
    }

    private void LateUpdate()
    {
        KeyMap[KeyBind.LightAttack].holdTime = lightAttacking ? KeyMap[KeyBind.LightAttack].holdTime + Time.deltaTime : 0;
        KeyMap[KeyBind.HeavyAttack].holdTime = heavyAttacking ? KeyMap[KeyBind.HeavyAttack].holdTime + Time.deltaTime : 0;
        
        KeyMap[KeyBind.LightAttack].lastTime = lightAttack.triggered ? 0 : KeyMap[KeyBind.LightAttack].lastTime + Time.deltaTime;
        KeyMap[KeyBind.HeavyAttack].lastTime = heavyAttack.triggered ? 0 : KeyMap[KeyBind.HeavyAttack].lastTime + Time.deltaTime;
    }

    public void ReleaseHoldAttacks()
    {
        lightAttacking = false;
        heavyAttacking = false;
    }
    
    #region Other Methods

    public static KeyBind[] GetHoldable(KeyBind[] keys)
    {
        HashSet<KeyBind> holdKeys = new();
        
        foreach (KeyBind key in keys)
        {
            if (KeyMap[key].holdAction != null)
            {
                holdKeys.Add(key);
            }
        }
        
        return holdKeys.ToArray();
    }
    
    public static KeyBind[] GetReleaseable(KeyBind[] keys)
    {
        HashSet<KeyBind> releaseKeys = new();
        
        foreach (KeyBind key in keys)
        {
            if (KeyMap[key].releaseAction != null)
            {
                releaseKeys.Add(key);
            }
        }
        
        return releaseKeys.ToArray();
    }
    
    #endregion
}

public class KeyBindData
{
    public Func<bool> action;
    public Func<bool> holdAction;
    public Func<bool> releaseAction;
    public float holdTime;
    public float lastTime;
}

public enum KeyBind
{
    None,
    LightAttack,
    HeavyAttack,
    AnyAttack,
    Dodge,
}

public enum AttackTypes
{
    LightAttack,
    HeavyAttack,
    MidairLightAttack,
    MidairHeavyAttack,
    SpecialAttack,
    Other
}
