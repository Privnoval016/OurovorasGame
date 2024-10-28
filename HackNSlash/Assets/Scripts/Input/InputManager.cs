using System.Collections;
using System.Collections.Generic;
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
    
    public InputAction lockOn;
    public InputAction retarget;

    public InputAction lightAttack;
    public InputAction heavyAttack;
    #endregion
    
    public Dictionary<KeyBind,InputAction> KeyMap = new Dictionary<KeyBind, InputAction>();
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
        
        
        lockOn = playerInputActions.Player.LockOn;
        KeyMap.Add(KeyBind.LockOn, lockOn);
        
        retarget = playerInputActions.Player.Retarget;
        
        
        lightAttack = playerInputActions.Player.LightAttack;
        KeyMap.Add(KeyBind.LightAttack, lightAttack);
        
        heavyAttack = playerInputActions.Player.HeavyAttack;
        KeyMap.Add(KeyBind.HeavyAttack, heavyAttack);
        
        
        playerInputActions.Player.Enable();
    }
}

public enum KeyBind
{
    LockOn,
    LightAttack,
    HeavyAttack
}
