using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Animancer;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(StateController))]
public class PlayerController : MonoBehaviour
{
    #region State Machine
    [HideInInspector] public StateController stateController;
    #endregion
    
    #region Components
    
    [HideInInspector] public Rigidbody rb;
    
    public PlayerData playerData;
    public AttackConfig attackData;
    
    [HideInInspector] public CameraController cam;
    
    public HybridAnimancerComponent animancer;
    
    #endregion
    
    #region MOVE PARAMETERS

    //Timers (also all fields, could be private and a method returning a bool could be used)
    [HideInInspector] public float lastOnGroundTime;
    [HideInInspector] public float lastDoubleJumpTime;
    [HideInInspector] public float lastPressedJumpTime;
    [HideInInspector] public float walkingTime;

    public bool IsGrounded =>
        Physics.CheckBox(groundCheckPoint.position, groundCheckSize, Quaternion.identity, groundLayer);
    
    
    public bool CanJump => lastOnGroundTime > 0 || !isJumping;
    public bool CanDoubleJump => lastDoubleJumpTime > playerData.doubleJumpWaitDuration 
                                 && !isDoubleJumpUsed;
    
    //Walk
    public bool IsWalking => moveInput.magnitude > 0;
    public bool IsSprinting => !cam.isLockedOn && IsWalking && walkingTime > playerData.sprintBuildupLength;
    
    //Jump
    [HideInInspector] public bool isJumping;
    [HideInInspector] public bool isJumpFalling;
    [HideInInspector] public bool isDoubleJumpTriggered;
    public bool IsJumpTriggered => lastPressedJumpTime > 0;
    public bool IsPerformingJump => lastPressedJumpTime > -playerData.jumpTimeToApex && lastPressedJumpTime < 0;
        
    [HideInInspector] public bool isDoubleJumpUsed = true;
    

    public float globalGravity = -9.81f;
    [HideInInspector] public float gravityScale;

    #endregion
    
    #region INPUT PARAMETERS
    
    [HideInInspector] public Vector2 moveInput;
    [HideInInspector] public Vector3 moveDirection;
    
    #endregion
    
    #region CHECK PARAMETERS
    [Header("Checks")] 
    [SerializeField] public Transform groundCheckPoint;
    [SerializeField] public Vector3 groundCheckSize = new Vector3(0.49f, 0.3f, 0.49f);
    
    #endregion

    #region ATTACK PARAMETERS
    
    [HideInInspector] public int comboIndex;

    #endregion
    
    #region LAYERS & TAGS

    [Header("Layers & Tags")] 
    [SerializeField] public LayerMask groundLayer;
    #endregion

    
    #region MonoBehaviour Callbacks

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        if (Camera.main != null)
            Camera.main.TryGetComponent(out cam);
        
        rb.useGravity = false;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        InputManager.Instance.lightAttack.performed += OnAttackAction;
        InputManager.Instance.heavyAttack.performed += OnAttackAction;
    }

    private void Start()
    {
        stateController = GetComponent<StateController>();
        stateController.parent = this;
        
        stateController.ChangeState(new PlayerMoving());
    }

    private void Update()
    {
        UpdateAnimatorState();
    }
    
    #endregion

    #region Input Callbacks
    
    private void OnAttackAction(InputAction.CallbackContext context)
    {
        // check for midair attack then run, check for other attack then run, check for basic attacks then run
        // interrupts moving with the specific attack
        
    }

    #endregion
    
    #region Animator Methods

    private void UpdateAnimatorState()
    {
        animancer.SetFloat(Animator.StringToHash("moveX"), moveInput.normalized.x, 0.1f, Time.deltaTime);
        animancer.SetFloat(Animator.StringToHash("moveZ"), moveInput.normalized.y, 0.1f, Time.deltaTime);
    }
    
    #endregion
}