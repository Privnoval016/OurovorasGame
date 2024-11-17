using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Animancer;
using ExtensionUtils;
using MEC;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(StateController))]
public class PlayerController : MonoBehaviour
{
    #region State Machine
    [HideInInspector] public StateController stateController;
    #endregion
    
    #region Components
    
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public CapsuleCollider col;
    
    public PlayerData playerData;
    public AttackConfig attackData;
    public MoveAnimData moveAnimData;
    public StringAsset[] parameterNames;
    
    [HideInInspector] public CameraController cam;
    
    public AnimancerComponent animancer;
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
    
    
    [HideInInspector] public float gravityScale;

    #endregion
    
    #region INPUT PARAMETERS
    
    [HideInInspector] public Vector2 moveInput;
    [HideInInspector] public Vector3 moveDirection;
    
    [HideInInspector] public Vector3 velocity;
    private Vector3 lastPosition;

    public Vector2 StandardizedMoveDir => moveInput.Rotate(-transform.right.ToVector2().ToAngle()).Rotate(cam.transform.right.ToVector2().ToAngle());
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

    public GameObject NearestEnemy
    {
        get
        {
            return cam.isLockedOn ? cam.targetedEnemy :
                Physics.OverlapSphere(transform.position, itsCalledAuraBro, enemyLayer).
                    Where(e => 
                        Vector3.Dot((e.transform.position - transform.position).normalized, transform.forward.normalized) > -0.72f)
                    .OrderBy(e => Vector3.Distance(transform.position, e.transform.position)).FirstOrDefault()?.gameObject;
        }
    }

    #endregion

    #region WEAPON PARAMETERS
    
    [Header("Weapons")]
    public GameObject[] weapons;

    public float itsCalledAuraBro = 2f;
    #endregion
    
    #region LAYERS & TAGS

    [Header("Layers & Tags")] 
    [SerializeField] public LayerMask groundLayer;
    
    [SerializeField] public LayerMask enemyLayer;
    #endregion

    
    #region MonoBehaviour Callbacks

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        
        if (Camera.main != null)
            Camera.main.TryGetComponent(out cam);
        
        rb.useGravity = false;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        animancer.TryGetComponent(out rootMotion);
        
        KeyMap = InputManager.KeyMap;
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
        velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
        
        foreach (StringAsset parameterName in parameterNames)
        {
            Parameter<float> param = animancer.Parameters.GetOrCreate<float>(parameterName);
            
            if (parameterName == "MoveX")
            {
                param.Value = EaseUtil.Damp(param.Value, StandardizedMoveDir.normalized.x, 2f, Time.deltaTime);
            }
            else if (parameterName == "MoveZ")
            {
                param.Value = EaseUtil.Damp(param.Value, StandardizedMoveDir.normalized.y, 2f, Time.deltaTime);
            }
        }
        
    }
    
    public AnimancerState PlayAnimation(AnimationClip clip, float fadeDuration = -1F, FadeMode mode = FadeMode.FixedSpeed)
    {
        if (animancer.IsPlaying(clip)) return null;

        return animancer.Play(clip, fadeDuration, mode);
    }
    
    public AnimancerState PlayAnimation(TransitionAsset clip)
    {
        return animancer.Play(clip);
    }
    
    public AnimancerState PlayAnimation(ITransition clip)
    {
        AnimancerState state = animancer.Play(clip);
        return state;
    }
    
    public void ExitTimeAnimation(ITransition currentAnim, ITransition nextAnim, Action onExit = null)
    {
        AnimancerState state = PlayAnimation(currentAnim);
        state.Events(this).OnEnd ??= () => OnAnimExit(nextAnim, onExit);
    }
	
    public void OnAnimExit(ITransition nextAnim, Action onExit = null)
    {
        if (nextAnim != null) PlayAnimation(nextAnim);
        onExit?.Invoke();
    }
    
    public void ExitTimeAnimation(AnimationClip currentAnim, AnimationClip nextAnim, Action onExit = null)
    {
        AnimancerState state = PlayAnimation(currentAnim);
        state.Events(this).OnEnd ??= () => OnAnimExit(nextAnim, onExit);
    }
	
    public void OnAnimExit(AnimationClip nextAnim, Action onExit = null)
    {
        if (nextAnim != null) PlayAnimation(nextAnim);
        onExit?.Invoke();
    }
    
    public void StopCurrentAnimation()
    {
        animancer.Stop();
    }

    #endregion
    
    #region Attack Methods

    public void InvokeOnAttack(Attack a)
    {
        OnAttackEvents.OnAttackActionMap[a.onAttackAction](this, a);
    }
    
    private void CheckAttackAction()
    {
        if (!canAttack) return;
                
        #region Special Attacks

        foreach (Attack attack in attackData.specialAttacks)
        {
            if (!attack.isEnabled) continue;
            
            if (attack.isLockedOn && !cam.isLockedOn) continue;
            
            if (attack.isMidair != NBool.Both && IsMidair != attack.isMidair.IsTrue()) continue;

            Vector2 direction = attack.applyTargetDirection ? StandardizedMoveDir : moveInput;
            
            if (attack.inputDirection.normalized != Vector2.zero && 
                Vector2.Dot(direction.normalized, attack.inputDirection.normalized) < 0.69f) continue;
            
            if (!attack.keyBinds.Any(k => KeyMap[k]())) continue;
            
            KeyBind[] holdKeys = InputManager.GetHoldVersion(attack.keyBinds);
            
            if (holdKeys.Length > 0 && stateController.GetCurrentState() is PlayerAttacking 
                                    && ((PlayerAttacking) stateController.GetCurrentState()).attack == attack)
                continue;
            
            if (holdKeys.Length == 0) InputManager.Instance.ReleaseHoldAttacks();
            
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
   
        if (KeyMap[KeyBind.LightAttack]())
        {
            comboIndex = comboIndex >= attackData.lightComboAttacks.Length - 1 ? 0 : comboIndex + 1;
            BeginAttack(attackData.lightComboAttacks[comboIndex]);
        }
        else if (KeyMap[KeyBind.HeavyAttack]())
        {
            comboIndex = comboIndex >= attackData.heavyComboAttacks.Length - 1 ? 0 : comboIndex + 1;
            BeginAttack(attackData.heavyComboAttacks[comboIndex]);
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
    
    
    #region Gravity Methods

    public void CalculateGravity()
    {
        if (isJumping && Mathf.Abs(rb.linearVelocity.y) < playerData.jumpHangTimeThreshold)
        {
            SetGravityScale(playerData.gravityScale * playerData.jumpHangGravityMult);
        }
        else if (rb.linearVelocity.y < 0)
        {
            //Higher gravity if falling
            SetGravityScale(playerData.gravityScale * playerData.fallGravityMult);
            //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -playerData.maxFallSpeed), rb.linearVelocity.z);
        }
        else
        {
            //Default gravity if standing on a platform or moving upwards
            SetGravityScale(playerData.gravityScale);
        }
    }
    
    public void SetGravityScale(float scale)
    {
        gravityScale = scale;
    }
    
    private void ApplyGravity()
    {
        Vector3 gravity = GameManager.Instance.globalGravity * gravityScale * Vector3.up;
        rb.AddForce(gravity, ForceMode.Acceleration);
    }
    
    #endregion
    
    #region Look Methods
    
    public void TurnToLook()
    {
        if (!IsWalking || moveDirection.magnitude == 0) return;
	    
        if (!cam.isLockedOn)
        {
            transform.rotation =
                EaseUtil.DampQuaternion(transform.rotation, Quaternion.LookRotation(moveDirection), 5f, 0.1f);
        }
        else
        {
            Vector3 lookDir = cam.LockOnDirection.ZeroVector3Axis();
            
            if (lookDir.magnitude < 0.3f) return;

            transform.rotation =
                EaseUtil.DampQuaternion(transform.rotation, Quaternion.LookRotation(lookDir), 5f, 0.1f);
        }
    }
    
    #endregion
}