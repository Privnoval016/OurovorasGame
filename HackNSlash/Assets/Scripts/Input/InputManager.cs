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

    public readonly Dictionary<KeyBind, Func<bool>> KeyMap = new();

    public float holdTime = 0.4f;

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
        KeyMap.Add(KeyBind.LightAttackHold, () => lightAttackHoldTime > holdTime);
        
        heavyAttack = playerInputActions.Player.HeavyAttack;
        heavyAttack.performed += ctx => heavyAttacking = true;
        heavyAttack.canceled += ctx => heavyAttacking = false;
        KeyMap.Add(KeyBind.HeavyAttack, () => heavyAttack.triggered);
        KeyMap.Add(KeyBind.HeavyAttackHold, () => heavyAttackHoldTime > holdTime);
        
        KeyMap.Add(KeyBind.AnyAttack, () => lightAttack.triggered || heavyAttack.triggered);
        KeyMap.Add(KeyBind.AnyAttackHold, () => AnyAttackHoldTime > holdTime);
        
        
        playerInputActions.Player.Enable();
    }
    
    void Update()
    {
        lightAttackHoldTime = lightAttacking ? lightAttackHoldTime + Time.deltaTime : 0;
        heavyAttackHoldTime = heavyAttacking ? heavyAttackHoldTime + Time.deltaTime : 0;
    }
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
    Dodge
}
