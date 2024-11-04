using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Animancer;
using ExtensionUtils;
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
    public RedirectRootMotionToRigidbody rootMotion;
    
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
    
    public bool IsMidair => !IsGrounded;
        
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
    
    [HideInInspector] public int comboIndex = -1;

    [HideInInspector] public bool canAttack;

    public Dictionary<KeyBind, Func<bool>> KeyMap;

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

        animancer.TryGetComponent(out rootMotion);
        
        KeyMap = InputManager.Instance.KeyMap;
    }

    private void Start()
    {
        stateController = GetComponent<StateController>();
        stateController.parent = this;
        
        stateController.ChangeState(new PlayerMoving());
    }

    private void Update()
    {
        moveInput = InputManager.Instance.movement.ReadValue<Vector2>();
        
        CheckAttackAction();

        UpdateAnimatorState();
    }
    
    private void FixedUpdate()
    {
        ApplyGravity();
    }
    
    #endregion

    
    #region Animator Methods

    private void UpdateAnimatorState()
    {
        animancer.SetFloat(Animator.StringToHash("moveX"), (transform.rotation * moveInput.normalized).x, 0.1f, Time.deltaTime);
        animancer.SetFloat(Animator.StringToHash("moveZ"), (transform.rotation * moveInput.normalized).y, 0.1f, Time.deltaTime);
    }
    
    public void PlayAnimationClip(AnimationClip clip, float fadeDuration = -1F, FadeMode mode = FadeMode.FixedSpeed)
    {
        if (animancer.IsPlaying(clip)) return;

        animancer.Play(clip, fadeDuration, mode);
    }
    
    public void CrossFadeAnimation(int stateNameHash, float fadeDuration = -1F, int layer = -1, float normalizedTime = Single.NegativeInfinity)
    {
        if (animancer.IsPlaying(stateNameHash)) return;
	    
        animancer.CrossFade(stateNameHash, fadeDuration, layer, normalizedTime);
    }
    
    public void StopCurrentAnimation()
    {
        animancer.Stop();
    }

    #endregion
    
    #region Attack Methods

    public void InvokeOnAttack(Attack a)
    {
        ActionEvents.OnAttackActionMap[a.onAttackAction](this, a);
    }
    
    private void CheckAttackAction()
    {
        if (!canAttack) return;
                
        #region Special Attacks

        foreach (Attack attack in attackData.specialAttacks)
        {
            if (attack.isLockedOn && !cam.isLockedOn) continue;
            if (attack.isMidair != IsMidair) continue;
            
            Vector2 direction = attack.applyTargetDirection ? transform.rotation * attack.inputDirection : attack.inputDirection;
            
            if (attack.inputDirection.normalized != Vector2.zero && 
                Vector2.Dot(moveInput.normalized, direction.normalized) < 0.91f) continue;
            
            if (!attack.keyBinds.Any(k => KeyMap[k]())) continue;
            
            KeyBind[] holdKeys = ActionEvents.GetHoldVersion(attack.keyBinds);
            
            if (holdKeys.Length > 0 && stateController.GetCurrentState() is PlayerAttacking 
                                    && ((PlayerAttacking) stateController.GetCurrentState()).attack == attack)
                continue;
            
            BeginAttack(attack);
            return;
        }
        
        #endregion


        
        #region Midair Combo Attacks
        
        if (IsMidair && KeyMap[KeyBind.AnyAttack]())
        {
            comboIndex = comboIndex >= attackData.midairAttacks.Length - 1 ? 0 : comboIndex + 1;
            BeginAttack(attackData.midairAttacks[comboIndex]);

            return;
        }
        
        #endregion
        
        #region Regular Combo Attacks
   
        if (KeyMap[KeyBind.AnyAttack]())
        {
            comboIndex = comboIndex >= attackData.lightComboAttacks.Length - 1 ? 0 : comboIndex + 1;
            BeginAttack(KeyMap[KeyBind.LightAttack]() ? attackData.lightComboAttacks[comboIndex] : 
                                                                attackData.heavyComboAttacks[comboIndex]);
        }
        
        #endregion
        
    }
        
    private void BeginAttack(Attack attack)
    {
        if (stateController.GetCurrentState() is PlayerMoving)
        {
            stateController.Interrupt(new PlayerAttacking(attack));
        }
        else if (stateController.GetCurrentState() is PlayerAttacking)
        {
            stateController.ChangeState(new PlayerAttacking(attack));
        }

    }

    #endregion
    
    #region General Methods
    
    private void ApplyGravity()
    {
        Vector3 gravity = globalGravity * gravityScale * Vector3.up;
        rb.AddForce(gravity, ForceMode.Acceleration);
    }
    
    
    #endregion
}