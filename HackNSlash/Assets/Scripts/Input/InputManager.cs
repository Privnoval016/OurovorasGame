using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    
    
    private PlayerInputActions InputMap;
    public InputAction pause;
    
    // dictionary of gamestates and their respective input actions
    public static readonly Dictionary<GameState, InputActionMap> GameStateInputs = new();

    
    #region Player Keybinds
    
    public InputAction movement;
    public InputAction cameraMove;
    
    public InputAction jump;
    public InputAction dodge;
    
    public InputAction lockOn;
    public InputAction retarget;

    public InputAction lightAttack;
    public InputAction heavyAttack;

    public InputAction swapMode;

    private bool lightAttacking, heavyAttacking;
    
    #endregion
    
    #region Menu Keybinds
    
    public InputAction navigate;
    public InputAction select;
    public InputAction back;
    
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

        InputMap = new PlayerInputActions();
        AddGameStateInputs();
        
        
        SetPlayerKeybinds();
        SetMenuKeybinds();
        
        InputMap.Player.Enable();
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
    
    #region Setup Keybinds

    private void AddGameStateInputs()
    {
        pause = InputMap.StateControl.Pause;
        InputMap.StateControl.Enable();
        
        GameStateInputs.Add(GameState.PlayerControl, InputMap.Player);
        GameStateInputs.Add(GameState.Menu, InputMap.Menu);
    }
    
    private void SetPlayerKeybinds()
    {
        movement = InputMap.Player.Move;
        cameraMove = InputMap.Player.Camera;
        jump = InputMap.Player.Jump;
        dodge = InputMap.Player.Dodge;
        lockOn = InputMap.Player.LockOn;
        retarget = InputMap.Player.Retarget;
        swapMode = InputMap.Player.EnterCombat;

        KeyMap.Add(KeyBind.None, new KeyBindData() {action = () => true});

        lightAttack = InputMap.Player.LightAttack;
        lightAttack.performed += ctx => lightAttacking = true;
        lightAttack.canceled += ctx => lightAttacking = false;
        
        KeyMap.Add(KeyBind.LightAttack, new KeyBindData
        {
            action = () => lightAttack.triggered,
            holdAction = () => lightAttacking,
            releaseAction = () => lightAttack.WasReleasedThisFrame(),
        });
        
        heavyAttack = InputMap.Player.HeavyAttack;
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
        KeyMap.Add(KeyBind.Jump, new KeyBindData {action = () => jump.triggered});
        
        
    }
    
    private void SetMenuKeybinds()
    {
        navigate = InputMap.Menu.Navigate;
        select = InputMap.Menu.Select;
        back = InputMap.Menu.Deselect;
    }
    
    #endregion
    
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
    Jump,
}

public enum AttackTypes
{
    LightAttack,
    HeavyAttack,
    MidairAttack,
    DirectionalAttack,
    SpecialAttack,
    Other
}
