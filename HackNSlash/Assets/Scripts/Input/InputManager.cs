using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using OnActionCallbacks;

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
    #endregion
    
    public Dictionary<KeyBind, Func<bool>> KeyMap = new Dictionary<KeyBind, Func<bool>>();
    
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
        KeyMap.Add(KeyBind.Dodge, () => dodge.triggered);
        
        
        lockOn = playerInputActions.Player.LockOn;
        KeyMap.Add(KeyBind.LockOn, () => Camera.main.GetComponent<CameraController>().isLockedOn);
        
        retarget = playerInputActions.Player.Retarget;
        
        
        lightAttack = playerInputActions.Player.LightAttack;
        KeyMap.Add(KeyBind.LightAttack, () => lightAttack.triggered);
        KeyMap.Add(KeyBind.LightAttackHold, () => lightAttack.ReadValue<float>() > 0);
        
        heavyAttack = playerInputActions.Player.HeavyAttack;
        KeyMap.Add(KeyBind.HeavyAttack, () => heavyAttack.triggered);
        KeyMap.Add(KeyBind.HeavyAttackHold, () => heavyAttack.ReadValue<float>() > 0);
        
        KeyMap.Add(KeyBind.AnyAttack, () => lightAttack.triggered || heavyAttack.triggered);
        
        
        ActionEvents.AddOnAttackMethods();
        
        playerInputActions.Player.Enable();
    }
}

public enum KeyBind
{
    LockOn,
    LightAttack,
    LightAttackHold,
    HeavyAttack,
    HeavyAttackHold,
    AnyAttack,
    Dodge
}
