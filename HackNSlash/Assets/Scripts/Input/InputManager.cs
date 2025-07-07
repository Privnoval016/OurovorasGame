using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class InputManager : Singleton<InputManager>
{
    private PlayerInputActions InputMap;
    public InputAction pause;
    public InputAction debug;
    
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
    public InputAction elementAttack;

    public InputAction swapMode;
    public InputAction ultimateMode;

    public InputAction elementMenuOpen;

    private bool lightAttacking, heavyAttacking, jumping;
    
    #endregion
    
    #region Menu Keybinds
    
    public InputAction navigate;
    public InputAction select;
    public InputAction back;
    public InputAction sort;
    
    public InputAction tabLeft;
    public InputAction tabRight;

    public InputAction scroll;
    
    #endregion
    
    
    public static readonly Dictionary<KeyBind, KeyBindData> KeyMap = new();

    protected override void Awake()
    {
        base.Awake();
        
        InputMap = new PlayerInputActions();
        AddGameStateInputs();
        
        InputSystem.onDeviceChange +=
            (device, change) =>
            {
                if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed)
                {
                    Debug.Log($"Device '{device}' was {change}");
                }
            };
        
        
        SetPlayerKeybinds();
        SetMenuKeybinds();
        
        InputMap.Player.Enable();
    }
    
    private void Update()
    {
        
    }

    private void LateUpdate()
    {
        KeyMap[KeyBind.West].holdTime = lightAttacking ? KeyMap[KeyBind.West].holdTime + Time.deltaTime : 0;
        KeyMap[KeyBind.North].holdTime = heavyAttacking ? KeyMap[KeyBind.North].holdTime + Time.deltaTime : 0;
        KeyMap[KeyBind.South].holdTime = jumping ? KeyMap[KeyBind.South].holdTime + Time.deltaTime : 0;
        
        KeyMap[KeyBind.West].lastTime = lightAttack.triggered ? 0 : KeyMap[KeyBind.West].lastTime + Time.deltaTime;
        KeyMap[KeyBind.North].lastTime = heavyAttack.triggered ? 0 : KeyMap[KeyBind.North].lastTime + Time.deltaTime;
        KeyMap[KeyBind.South].lastTime = jumping ? 0 : KeyMap[KeyBind.South].lastTime + Time.deltaTime;
    }

    public void ReleaseHoldAttacks()
    {
        lightAttacking = false;
        heavyAttacking = false;
        jumping = false;
    }
    
    #region Setup Keybinds

    private void AddGameStateInputs()
    {
        pause = InputMap.StateControl.Pause;
        debug = InputMap.StateControl.Debug;
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
        ultimateMode = InputMap.Player.ActivateUltimate;
        elementMenuOpen = InputMap.Player.ElementMenu;

        KeyMap.Add(KeyBind.None, new KeyBindData() {action = () => true});

        
        lightAttack = InputMap.Player.LightAttack;
        lightAttack.performed += ctx => lightAttacking = true;
        lightAttack.canceled += ctx => lightAttacking = false;
        
        KeyMap.Add(KeyBind.West, new KeyBindData
        {
            action = () => lightAttack.triggered,
            holdAction = () => lightAttacking,
            releaseAction = () => lightAttack.WasReleasedThisFrame(),
        });
        
        
        heavyAttack = InputMap.Player.HeavyAttack;
        heavyAttack.performed += ctx => heavyAttacking = true;
        heavyAttack.canceled += ctx => heavyAttacking = false;
        
        KeyMap.Add(KeyBind.North, new KeyBindData
        {
            action = () => heavyAttack.triggered,
            holdAction = () => heavyAttacking,
            releaseAction = () => heavyAttack.WasReleasedThisFrame(),
        });
        
        
        KeyMap.Add(KeyBind.AnyAttack, new KeyBindData
        {
            action = () => KeyMap[KeyBind.West].action() || KeyMap[KeyBind.North].action(),
            holdAction = () => KeyMap[KeyBind.West].holdAction() || KeyMap[KeyBind.North].holdAction(),
            releaseAction = () => KeyMap[KeyBind.West].releaseAction() || KeyMap[KeyBind.North].releaseAction(),
        });
        
        
        KeyMap.Add(KeyBind.East, new KeyBindData {action = () => dodge.triggered});
        KeyMap.Add(KeyBind.South, new KeyBindData
        {
            action = () => jump.triggered,
            holdAction = () => jumping,
            releaseAction = () => jump.WasReleasedThisFrame(),
        });
        
        
        elementAttack = InputMap.Player.ElementAttack;
    }
    
    private void SetMenuKeybinds()
    {
        navigate = InputMap.Menu.Navigate;
        select = InputMap.Menu.Select;
        back = InputMap.Menu.Deselect;
        tabLeft = InputMap.Menu.TabLeft;
        tabRight = InputMap.Menu.TabRight;
        
        sort = InputMap.Menu.Sort;
        scroll = InputMap.Menu.Scroll;
    }

    public void EnableStateInputs(GameState state)
    {
        foreach (var input in GameStateInputs.Values)
        {
            input.Disable();
        }
        
        GameStateInputs[state].Enable();
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
    
    public static bool AnyKeyPressed(KeyBind[] keys = null)
    {

        keys ??= KeyMap.Keys.ToArray();
        
        foreach (KeyBind key in keys)
        {
            if (key != KeyBind.None && KeyMap[key].action != null && KeyMap[key].action())
            {
                return true;
            }
        }
        
        return false;
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
    West,
    North,
    AnyAttack,
    East,
    South,
}
