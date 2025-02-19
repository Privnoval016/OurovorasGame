using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using Animancer;
using ExtensionUtils;
using MEC;
using UnityEngine.Serialization;

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
    
    [HideInInspector]
    public WeaponController wc;
    
    public float playerRadius = 3f;
    
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
    
    [HideInInspector] public Vector3 discreteVelocity;
    [FormerlySerializedAs("moveAcceleration")] [HideInInspector] public Vector3 discreteAcceleration;
    private Vector3 lastPosition, lastVelocity;

    public Vector2 StandardizedMoveDir => moveInput.Rotate(-transform.right.ToVector2().ToAngle()).
                                                Rotate(cam.transform.right.ToVector2().ToAngle()).normalized;
    #endregion
    
    #region GROUND CHECK PARAMETERS
   
    [Header("Checks")] 
    [SerializeField] public Transform groundCheckPoint;
    [SerializeField] public Vector3 groundCheckSize = new Vector3(0.49f, 0.3f, 0.49f);
    
    #endregion

    #region ATTACK PARAMETERS

    [HideInInspector] public bool canAttack;
    [HideInInspector] public Attack currentAttack;

    public Dictionary<KeyBind, KeyBindData> KeyMap;
    
    [HideInInspector] public List<ComboAction> comboChain = new();
    [HideInInspector] public float comboResetTimer = 0;
    [HideInInspector] public bool pauseComboReset = false;

    public GameObject NearestEnemy
    {
        get
        {
            return cam.isLockedOn ? cam.targetedEnemy :
                Physics.OverlapSphere(transform.position, playerRadius, enemyLayer).
                    Where(e =>
                        (e.transform.position - transform.position).IsInDirectionCone(transform.forward, 190f))
                    .OrderBy(e => Vector3.Distance(transform.position, e.transform.position)).FirstOrDefault()?.gameObject;
        }
    }

    #endregion

    #region WEAPON PARAMETERS

    
    
    [HideInInspector] public float dodgeTimer = 0;
    #endregion

    #region ANIMATION PARAMETERS

    [HideInInspector] public AnimancerState currentAnimState;

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
        wc = GetComponent<WeaponController>();
        
        if (Camera.main != null)
            Camera.main.TryGetComponent(out cam);
        
        rb.useGravity = false;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        animancer.TryGetComponent(out rootMotion);
        
        KeyMap = InputManager.KeyMap;
        
        lastPosition = transform.position;
        lastVelocity = rb.linearVelocity;

        wc.player = this;
    }

    private void Start()
    {
        stateController = GetComponent<StateController>();
        stateController.parent = this;
        
        stateController.ChangeState(new PlayerMoving());
    }

    private void Update()
    {
        SetMoveValues();
        
        CheckAttackAction();

        UpdateAnimatorState();
    }
    
    private void FixedUpdate()
    {
        ApplyGravity();
    }
    
    #endregion
    
    #region Info Methods
    
    private void SetMoveValues()
    {
        moveInput = InputManager.Instance.movement.ReadValue<Vector2>();
        
        discreteVelocity = (transform.position - lastPosition) / Time.deltaTime;
        discreteAcceleration = (discreteVelocity - lastVelocity) / Time.deltaTime;
        
        lastPosition = transform.position;
        lastVelocity = discreteVelocity;
        
        // Debug.Log(" Discrete Velocity: " + discreteVelocity + " Discrete Acceleration: " + discreteAcceleration);
        
    }
    
    #endregion
    
    #region Animator Methods

    private void UpdateAnimatorState()
    {
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
    
    public AnimancerState PlayAnimation(AnimationClip clip, float fadeDuration = -1F, bool canInterrupt = true, FadeMode mode = FadeMode.FixedSpeed)
    {
        if (canInterrupt && animancer.States.Current.Clip == clip)
        {
            currentAnimState.Time = 0;
            return currentAnimState;
        }
        
        currentAnimState = animancer.Play(clip, fadeDuration, mode);
        return currentAnimState;
    }
    
    public AnimancerState PlayAnimation(TransitionAsset clip)
    {
        currentAnimState = animancer.Play(clip);
        return currentAnimState;
    }
    
    public AnimancerState PlayAnimation(ITransition clip)
    {
        currentAnimState = animancer.Play(clip);
        return currentAnimState;
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
        AnimancerState state = PlayAnimation(currentAnim, -1F, false);
        state.Events(this).OnEnd ??= () => OnAnimExit(nextAnim, onExit);
    }
	
    public void OnAnimExit(AnimationClip nextAnim, Action onExit = null)
    {
        if (nextAnim != null) PlayAnimation(nextAnim, -1F, false);
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
    
    public void InvokeOnVFX(TransformInfo start, Attack a, int actionIndex = 0)
    {
        OnVFXEvents.OnVFXActionMap[a.vfxAttack.vfxActions[actionIndex]](this, start, a);
    }
    
    private void CheckAttackAction()
    {

        if (!pauseComboReset)
        {
            //Debug.Log(comboChain != null && comboChain.Count > 0 ? comboChain.Last().actionType.ToString() : "No Combo Chain");
            comboResetTimer += Time.deltaTime;
        }
        else
        {
            comboResetTimer = 0;
        }

        //Debug.Log("Combo Reset Timer: " + comboResetTimer + " Combo Chain: " + comboChain.Count);
        
        if (comboResetTimer > attackData.comboResetTime)
        {
            comboChain.Clear();
        }
        
        #region Dodge
        
        dodgeTimer += Time.deltaTime;

        if (dodgeTimer > attackData.dodgeCoolDown)
        {
            foreach (Attack attack in attackData.dodgeAttacks)
            {
                if (!AttackIsAvailable(attack)) continue;

                dodgeTimer = 0;
                BeginAttack(attack);
                return;
            }
        }

        #endregion
        
        if (!canAttack) return;
                
        #region Special Attacks

        foreach (Attack attack in attackData.specialAttacks)
        {
            if (!AttackIsAvailable(attack)) continue;
            
            comboChain.Clear();
            BeginAttack(attack);
            return;
        }
        
        #endregion

        #region Combo Attacks
        
        List<ComboAction> possibleActions = new();
        
        foreach (ComboConfig combo in attackData.comboAttacks)
        {
            if (!combo.isEnabled) continue;
            if (comboChain.Count >= combo.comboActions.Length) continue;

            List<Attack> attacks = combo.comboActions.Select(a => a.attack).ToList();
            attacks = attacks.Take(comboChain.Count).ToList();
            List<Attack> comboAttacks = comboChain.Select(a => a.attack).ToList();
            if (comboAttacks.Except(attacks).Any()) continue;
            
            ComboAction nextAction = combo.comboActions[comboChain.Count];
            switch (nextAction.actionType)
            {
                case ComboActionType.Press:
                case ComboActionType.Release:
                    if (!AttackIsAvailable(nextAction.attack)) continue;
                    possibleActions.Add(nextAction);
                    
                    break;
                
                case ComboActionType.Hold:
                    KeyBind pressedKey = nextAction.attack.keyBinds.Contains(KeyBind.LightAttack) ? KeyBind.LightAttack : KeyBind.HeavyAttack;
                    if (KeyMap[pressedKey].holdTime > nextAction.time)
                    {
                        possibleActions.Add(nextAction);
                    }

                    break;
                
                case ComboActionType.Pause:
                    if (comboResetTimer > nextAction.time)
                    {
                        comboChain.Add(nextAction);
                    }
                    
                    break;
                
                case ComboActionType.Mash:
                    KeyBind mashKey = nextAction.attack.keyBinds.Contains(KeyBind.LightAttack) ? KeyBind.LightAttack : KeyBind.HeavyAttack;
                    
                    if (KeyMap[mashKey].lastTime < nextAction.time && KeyMap[mashKey].action())
                    {
                        possibleActions.Add(nextAction);
                    }
                    
                    break;
            }
        }
        
        if (possibleActions.Count > 0)
        {
            ComboAction action = possibleActions.OrderBy(a => ComboConfig.ComboActionPriority.IndexOf(a.actionType)).First();
            BeginComboAttack(action);
            return;
        }

        #endregion
        
        
        #region Combo Starters
        
        if (IsMidair && KeyMap[KeyBind.LightAttack].action())
        {
            comboChain.Clear();
            BeginAttack(attackData.midairAttacks[0], ComboActionType.Press);

            return;
        }
        if (KeyMap[KeyBind.LightAttack].action())
        {
            comboChain.Clear();
            BeginAttack(attackData.lightComboAttacks[0], ComboActionType.Press);
            return;
        }
        if (KeyMap[KeyBind.HeavyAttack].action())
        {
            comboChain.Clear();
            BeginAttack(attackData.heavyComboAttacks[0], ComboActionType.Press);
            return;
        }
        
        #endregion
        
    }

    private void BeginComboAttack(ComboAction action)
    {
        comboResetTimer = 0;
        comboChain.Add(action);
        
        if (action == null) return;
        
        currentAttack = action.attack;

        if (stateController.GetCurrentState() is PlayerMoving)
        {
            stateController.Interrupt(new PlayerAttacking(action.attack));
        }
        else if (stateController.GetCurrentState() is PlayerAttacking)
        {
            Timing.KillCoroutines(OnAttackEvents.Instance.GetInstanceID());
            stateController.ChangeState(new PlayerAttacking(action.attack));
        }
    }

    private void BeginAttack(Attack attack, ComboActionType type = ComboActionType.Special, float actionTime = 0)
    {
        comboResetTimer = 0;
        comboChain.Add(new ComboAction()
        {
            actionType = type,
            attack = attack,
            time = actionTime
        });
        
        currentAttack = attack;
        
        if (attack == null) return;
        
        if (stateController.GetCurrentState() is PlayerMoving)
        {
            stateController.Interrupt(new PlayerAttacking(attack));
        }
        else if (stateController.GetCurrentState() is PlayerAttacking)
        {
            Timing.KillCoroutines(OnAttackEvents.Instance.GetInstanceID());
            stateController.ChangeState(new PlayerAttacking(attack));
        }
    }
    
    private bool AttackIsAvailable(Attack attack)
    {
        if (!attack.isEnabled) return false;
        
        if (attack.isLockedOn && !cam.isLockedOn) return false;
        
        if (attack.isMidair != NBool.Both && IsMidair != attack.isMidair.IsTrue()) return false;

        if (!attack.keyBinds.Any(k => KeyMap[k].action())) return false;
        
        Vector2 direction = attack.applyTargetDirection ? StandardizedMoveDir : moveInput;
        if (attack.inputDirection != Vector2.zero && !direction.IsInDirectionCone(attack.inputDirection, 92f)) return false;

        return true;
    }

    #endregion
    
    
    #region Gravity Methods

    public void CalculateGravity()
    {
        Debug.Log(rb.linearVelocity.y);
        if (IsMidair && Mathf.Abs(rb.linearVelocity.y) < playerData.jumpHangTimeThreshold)
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