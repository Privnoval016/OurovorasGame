using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions.Patterns;
using Extensions.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using static PlayerInputActions;

public class InputManager : Singleton<InputManager>, IPlayerActions, IMenuActions, IStateControlActions
{
    private PlayerInputActions InputMap;
    
    public event Action<InputAction.CallbackContext> onPause = delegate { };
    public event Action<InputAction.CallbackContext> onDebug = delegate { };
    
    public static readonly Dictionary<GameState, InputActionMap> GameStateInputs = new();
    
    #region Player Keybinds
    public Vector2 Movement => InputMap.Player.Move.ReadValue<Vector2>();
    public Vector2 CameraMove => InputMap.Player.Camera.ReadValue<Vector2>();
    
    public event Action<InputAction.CallbackContext> onJump = delegate { };
    public event Action<InputAction.CallbackContext> onLockOn = delegate { };
    public event Action<InputAction.CallbackContext> onRetarget = delegate { };
    public event Action<InputAction.CallbackContext> onElementAttack = delegate { };
    public event Action<InputAction.CallbackContext> onSwapMode = delegate { };
    public event Action<InputAction.CallbackContext> onUltimateMode = delegate { };
    public event Action<InputAction.CallbackContext> onElementMenuOpen = delegate { };
    
    public event Action<InputAction.CallbackContext> onElementSwapLeft = delegate { };
    public event Action<InputAction.CallbackContext> onElementSwapRight = delegate { };

    
    private InputAction westButtonPressed;
    private InputAction northButtonPressed;
    private InputAction eastButtonPressed;
    private InputAction southButtonPressed;
    private bool westButtonHeld, northButtonHeld, southButtonHeld, eastButtonHeld;
    
    #endregion
    
    #region Menu Keybinds
    
    public event Action<InputAction.CallbackContext> onNavigate = delegate { };
    public event Action<InputAction.CallbackContext> onSelect = delegate { };
    public event Action<InputAction.CallbackContext> onBack = delegate { };
    public event Action<InputAction.CallbackContext> onSort = delegate { };
    public event Action<InputAction.CallbackContext> onTabLeft = delegate { };
    public event Action<InputAction.CallbackContext> onTabRight = delegate { };
    public event Action<InputAction.CallbackContext> onScroll = delegate { };
    
    #endregion
    
    
    public static readonly Dictionary<KeyBind, KeyBindData> KeyMap = new();

    #region MonoBehaviour Callbacks
    
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
        
        InputMap.Player.SetCallbacks(this);
        InputMap.Menu.SetCallbacks(this);
        InputMap.StateControl.SetCallbacks(this);
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        
    }

    private void LateUpdate()
    {
        KeyMap[KeyBind.West].holdTime = westButtonHeld ? KeyMap[KeyBind.West].holdTime + Time.deltaTime : 0;
        KeyMap[KeyBind.North].holdTime = northButtonHeld ? KeyMap[KeyBind.North].holdTime + Time.deltaTime : 0;
        KeyMap[KeyBind.South].holdTime = southButtonHeld ? KeyMap[KeyBind.South].holdTime + Time.deltaTime : 0;
        KeyMap[KeyBind.East].holdTime = eastButtonHeld ? KeyMap[KeyBind.East].holdTime + Time.deltaTime : 0;
        
        KeyMap[KeyBind.West].lastTime = westButtonPressed.triggered ? 0 : KeyMap[KeyBind.West].lastTime + Time.deltaTime;
        KeyMap[KeyBind.North].lastTime = northButtonPressed.triggered ? 0 : KeyMap[KeyBind.North].lastTime + Time.deltaTime;
        KeyMap[KeyBind.South].lastTime = southButtonHeld ? 0 : KeyMap[KeyBind.South].lastTime + Time.deltaTime;
        KeyMap[KeyBind.East].lastTime = eastButtonHeld ? 0 : KeyMap[KeyBind.East].lastTime + Time.deltaTime;
    }

    private void OnEnable()
    {
        InputMap.Enable();
    }
    
    private void OnDisable()
    {
        InputMap.Disable();
    }

    #endregion

    public void ReleaseHoldAttacks()
    {
        westButtonHeld = false;
        northButtonHeld = false;
        southButtonHeld = false;
    }
    
    #region Setup Keybinds

    private void AddGameStateInputs()
    {
        InputMap.StateControl.Enable();
        
        GameStateInputs.Add(GameState.PlayerControl, InputMap.Player);
        GameStateInputs.Add(GameState.Menu, InputMap.Menu);
    }
    
    private void SetPlayerKeybinds()
    {
        KeyMap.Add(KeyBind.None, new KeyBindData() {action = () => true});

        
        westButtonPressed = InputMap.Player.LightAttack;
        westButtonPressed.performed += ctx => westButtonHeld = true;
        westButtonPressed.canceled += ctx => westButtonHeld = false;
        
        KeyMap.Add(KeyBind.West, new KeyBindData
        {
            action = () => westButtonPressed.triggered,
            holdAction = () => westButtonHeld,
            releaseAction = () => westButtonPressed.WasReleasedThisFrame(),
        });
        
        
        northButtonPressed = InputMap.Player.HeavyAttack;
        northButtonPressed.performed += ctx => northButtonHeld = true;
        northButtonPressed.canceled += ctx => northButtonHeld = false;
        
        KeyMap.Add(KeyBind.North, new KeyBindData
        {
            action = () => northButtonPressed.triggered,
            holdAction = () => northButtonHeld,
            releaseAction = () => northButtonPressed.WasReleasedThisFrame(),
        });
        
        
        KeyMap.Add(KeyBind.AnyAttack, new KeyBindData
        {
            action = () => KeyMap[KeyBind.West].action() || KeyMap[KeyBind.North].action(),
            holdAction = () => KeyMap[KeyBind.West].holdAction() || KeyMap[KeyBind.North].holdAction(),
            releaseAction = () => KeyMap[KeyBind.West].releaseAction() || KeyMap[KeyBind.North].releaseAction(),
        });
        
        eastButtonPressed = InputMap.Player.Dodge;
        KeyMap.Add(KeyBind.East, new KeyBindData
        {
            action = () => eastButtonPressed.triggered,
            holdAction = () => eastButtonHeld,
            releaseAction = () => eastButtonPressed.WasReleasedThisFrame(),
        });
        
        southButtonPressed = InputMap.Player.Jump;
        KeyMap.Add(KeyBind.South, new KeyBindData
        {
            action = () => southButtonPressed.triggered,
            holdAction = () => southButtonHeld,
            releaseAction = () => southButtonPressed.WasReleasedThisFrame(),
        });
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
    
    #region Player Input Callbacks
    
    public void OnMove(InputAction.CallbackContext context) { }

    public void OnCamera(InputAction.CallbackContext context) { }

    public void OnJump(InputAction.CallbackContext context)
    {
        onJump?.Invoke(context);
        southButtonHeld = context.performed;
    }
    
    public void OnDodge(InputAction.CallbackContext context) { }
    
    public void OnLockOn(InputAction.CallbackContext context)
    {
        onLockOn?.Invoke(context);
    }
    
    public void OnRetarget(InputAction.CallbackContext context)
    {
        onRetarget?.Invoke(context);
    }

    public void OnLightAttack(InputAction.CallbackContext context) { }

    public void OnHeavyAttack(InputAction.CallbackContext context) { }
    
    public void OnElementAttack(InputAction.CallbackContext context)
    {
        onElementAttack?.Invoke(context);
    }
    
    public void OnEnterCombat(InputAction.CallbackContext context)
    {
        Debug.Log("Enter Combat");
        onSwapMode?.Invoke(context);
    }
    
    public void OnActivateUltimate(InputAction.CallbackContext context)
    {
        onUltimateMode?.Invoke(context);
    }
    
    public void OnElementMenu(InputAction.CallbackContext context)
    {
        onElementMenuOpen?.Invoke(context);
    }

    public void OnElementSwapLeft(InputAction.CallbackContext context)
    {
        onElementSwapLeft?.Invoke(context);
    }
    
    public void OnElementSwapRight(InputAction.CallbackContext context)
    {
        onElementSwapRight?.Invoke(context);
    }
    
    #endregion
    
    #region Menu Input Callbacks
    
    public void OnNavigate(InputAction.CallbackContext context)
    {
        onNavigate?.Invoke(context);
    }
    
    public void OnSelect(InputAction.CallbackContext context)
    {
        onSelect?.Invoke(context);
    }
    
    public void OnDeselect(InputAction.CallbackContext context)
    {
        onBack?.Invoke(context);
    }
    
    public void OnTabLeft(InputAction.CallbackContext context)
    {
        onTabLeft?.Invoke(context);
    }
    
    public void OnTabRight(InputAction.CallbackContext context)
    {
        onTabRight?.Invoke(context);
    }
    
    public void OnSort(InputAction.CallbackContext context)
    {
        onSort?.Invoke(context);
    }
    
    public void OnScroll(InputAction.CallbackContext context)
    {
        onScroll?.Invoke(context);
    }
    
    #endregion
    
    #region State Control Input Callbacks
    
    public void OnPause(InputAction.CallbackContext context)
    {
        onPause.Invoke(context);
    }
    
    public void OnDebug(InputAction.CallbackContext context)
    {
        onDebug.Invoke(context);
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
