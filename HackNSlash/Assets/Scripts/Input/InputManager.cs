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
    #endregion
    
    
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
        
        playerInputActions.Player.Enable();
    }
    
}
