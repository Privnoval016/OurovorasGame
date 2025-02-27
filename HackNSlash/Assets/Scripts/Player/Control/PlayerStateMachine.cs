using System.Collections.Generic;
using System.Linq;
using ExtensionUtils;
using MEC;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerStateMachine : MonoBehaviour
{
    [HideInInspector]
    public PlayerController pc;
    
    #region Inspector Variables
    
    [Header("Locomotion Data")]
    
    public PlayerData playerData;
    public AttackConfig attackData;
    
    [HideInInspector] public MovingStates movingState;
    
    #endregion
    
     #region MOVE PARAMETERS
     
     [HideInInspector] public bool pauseMovement;
    
     [HideInInspector] public float lastOnGroundTime;
     [HideInInspector] public float lastDoubleJumpTime;
     [HideInInspector] public float lastPressedJumpTime;
     [HideInInspector] public float walkingTime;

     public bool IsGrounded =>
        Physics.CheckBox(groundCheckPoint.position, groundCheckSize, Quaternion.identity, groundLayer);
     public bool IsMidair => !IsGrounded;

     public bool CanJump => lastOnGroundTime > 0 && !isJumping;
     
     [HideInInspector] public bool isDoubleJumpUsed;
     public bool CanDoubleJump => lastDoubleJumpTime <= 0 && !isDoubleJumpUsed && canAttack;

     [HideInInspector] public bool activateMidairEntry;
    
     //Walk
     public bool IsWalking => moveInput.magnitude > 0;
     public bool IsSprinting => !pc.cam.isLockedOn && IsWalking && walkingTime > playerData.sprintBuildupLength;
    
     //Jump
     [HideInInspector] public bool isJumping;
     [HideInInspector] public bool isJumpFalling;
     public bool IsJumpTriggered => lastPressedJumpTime > 0;
    
    
    
     [HideInInspector] public float gravityScale;

    #endregion
    
    #region INPUT PARAMETERS
    
    [HideInInspector] public Vector2 moveInput;
    [HideInInspector] public Vector3 moveDirection;
    
    [HideInInspector] public Vector3 discreteVelocity;
    [FormerlySerializedAs("moveAcceleration")] [HideInInspector] public Vector3 discreteAcceleration;
    private Vector3 lastPosition, lastVelocity;

    public Vector2 StandardizedMoveDir => moveInput.Rotate(-transform.right.ToVector2().ToAngle()).
                                                Rotate(pc.cam.transform.right.ToVector2().ToAngle()).normalized;
    #endregion
    
    #region GROUND CHECK PARAMETERS
   
    [Header("Checks")] 
    [SerializeField] public Transform groundCheckPoint;
    [SerializeField] public Vector3 groundCheckSize = new Vector3(0.49f, 0.3f, 0.49f);
    
    #endregion

    #region ATTACK PARAMETERS

    [HideInInspector] public bool canAttack;
    [HideInInspector] public Attack currentAttack;
    
    [HideInInspector] public List<ComboAction> comboChain = new();
    [HideInInspector] public float comboResetTimer = 0;
    [HideInInspector] public bool pauseComboReset = false;
    
    
    [HideInInspector] public int numMidairAttacks;
    [HideInInspector] public Dictionary<Attack, int> NumActionsUsed = new();

    public GameObject NearestEnemy
    {
        get
        {
            return pc.cam.isLockedOn ? pc.cam.targetedEnemy :
                Physics.OverlapSphere(transform.position, pc.playerRadius, enemyLayer).
                    Where(e =>
                        (e.transform.position - transform.position).IsInDirectionCone(transform.forward, 190f))
                    .OrderBy(e => Vector3.Distance(transform.position, e.transform.position)).FirstOrDefault()?.gameObject;
        }
    }

    #endregion
    
    [HideInInspector] public float dodgeTimer = 0;

    
    #region LAYERS & TAGS

    [Header("Layers & Tags")] 
    [SerializeField] public LayerMask groundLayer;
    
    [SerializeField] public LayerMask enemyLayer;
    #endregion
    
    public Dictionary<KeyBind, KeyBindData> KeyMap;
    
    
    #region MonoBehaviour Callbacks
    
    
    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        
        pc.rb.useGravity = false;
        
        KeyMap = InputManager.KeyMap;
        
        lastPosition = transform.position;
        lastVelocity = pc.rb.linearVelocity;
        
        pc.sc.ChangeState(new PlayerMoving());
    }
    
    private void Update()
    {
        SetMoveValues();
        CheckGrounded();
        CheckAttackAction();
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
    
    public GameObject GetEnemyInRadius(float radius)
    {
        return Physics.OverlapSphere(transform.position, radius, enemyLayer)
            .OrderBy(e => Vector3.Distance(transform.position, e.transform.position)).FirstOrDefault()?.gameObject;
    }
    
    #endregion
    
    #region Attack Methods

    public void InvokeOnAttack(Attack a)
    {
        OnAttackEvents.OnAttackActionMap[a.onAttackAction](pc, a);
    }
    
    public void ResetActions()
    {
        NumActionsUsed.Clear();
        
        numMidairAttacks = 0;
    }

    private void SetActionTimers()
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
        
        dodgeTimer += Time.deltaTime;
    }

    private bool CheckMobilityAction()
    {

        if (movingState == MovingStates.Combat && !pauseMovement)
        {
            #region Dodge

            if (dodgeTimer > attackData.dodgeCoolDown)
            {
                foreach (Attack attack in attackData.dodgeAttacks)
                {
                    if (!AttackIsAvailable(attack)) continue;

                    dodgeTimer = 0;
                    BeginAttack(attack);
                    return true;
                }
            }

            #endregion

            #region Enemy Step

            foreach (Attack attack in attackData.enemyStepAttacks)
            {
                if (!AttackIsAvailable(attack)) continue;
                
                if (comboChain.LastOrDefault()?.attack == attack) continue;

                if (IsGrounded || GetEnemyInRadius(pc.playerRadius) == null) continue;

                ResetActions();
                BeginAttack(attack);
                return true;
            }

            #endregion
        }

        #region Double Jump
        
        if (attackData.doubleJumpEnabled && CanDoubleJump && KeyMap[KeyBind.Jump].action())
        {
            lastDoubleJumpTime = 0;
            isDoubleJumpUsed = true;
            Jump(playerData.doubleJumpForce, true, WalkingAnimStates.DoubleJumping);
            return true;
        }
        
        #endregion

        return false;
    }
    
    private void CheckAttackAction()
    {
        SetActionTimers();
        
        if (CheckMobilityAction()) return;
        
        if (movingState != MovingStates.Combat || pauseMovement) return;
        
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
        
        pc.rb.linearVelocity = Vector3.zero;
        
        if (NumActionsUsed.ContainsKey(action.attack)) NumActionsUsed[action.attack]++;
        
        if (pc.sc.GetCurrentState() is PlayerMoving)
        {
            pc.sc.Interrupt(new PlayerAttacking(action.attack));
        }
        else if (pc.sc.GetCurrentState() is PlayerAttacking)
        {
            OnAttackEvents.Instance.EndObjectCoroutines();
            pc.sc.ChangeState(new PlayerAttacking(action.attack));
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
        
        pc.rb.linearVelocity = Vector3.zero;
        
        if (NumActionsUsed.ContainsKey(attack)) NumActionsUsed[attack]++;
        
        if (pc.sc.GetCurrentState() is PlayerMoving)
        {
            pc.sc.Interrupt(new PlayerAttacking(attack));
        }
        else if (pc.sc.GetCurrentState() is PlayerAttacking)
        {
            OnAttackEvents.Instance.EndObjectCoroutines();
            pc.sc.ChangeState(new PlayerAttacking(attack));
        }
    }
    
    private bool AttackIsAvailable(Attack attack)
    {
        if (!attack.isEnabled) return false;
        
        if (!NumActionsUsed.ContainsKey(attack) && attack.maxUses > 0)
        {
            NumActionsUsed.Add(attack, 0);
        }
        
        if (NumActionsUsed.ContainsKey(attack) && NumActionsUsed[attack] >= attack.maxUses) return false;
        
        if (attack.isLockedOn && !pc.cam.isLockedOn) return false;
        
        if (attack.isMidair != NBool.Both && IsMidair != attack.isMidair.IsTrue()) return false;

        if (!attack.keyBinds.Any(k => KeyMap[k].action())) return false;
        
        Vector2 direction = attack.applyTargetDirection ? StandardizedMoveDir : moveInput;
        if (attack.inputDirection != Vector2.zero && !direction.IsInDirectionCone(attack.inputDirection, 92f)) return false;

        return true;
    }

    #endregion
    
    
    #region Gravity Methods

    public void Jump(float force, bool switchAnim = true, WalkingAnimStates animState = WalkingAnimStates.Jumping)
    {
        if (switchAnim) pc.pac.SwitchAnimState(animState, () => pc.pac.SwitchAnimState(WalkingAnimStates.Falling));

        #region Perform Jump
        if (pc.rb.linearVelocity.y < 0)
            force -= pc.rb.linearVelocity.y;
        
        pc.rb.linearVelocity = pc.rb.linearVelocity.ZeroVector3Axis();
		
        pc.rb.AddForce(Vector3.up * force, ForceMode.Impulse);
        #endregion
    }

    public void CheckGrounded()
    {
        
        if (pc.psm.IsGrounded)
        {
            pc.psm.lastOnGroundTime = pc.psm.playerData.coyoteTime;
            isDoubleJumpUsed = false;
            pc.psm.lastDoubleJumpTime = playerData.doubleJumpWaitDuration;
            ResetActions();
        }
        else
        {
            pc.psm.lastDoubleJumpTime -= Time.deltaTime;
        }
	    
        if (pc.rb.linearVelocity.y < -0.1f && pc.psm.isJumping)
        {
            pc.psm.isJumping = false;
            pc.psm.isJumpFalling = true;
        }
	    
        if (pc.psm.activateMidairEntry)
        {
            pc.psm.isJumping = true;
            pc.psm.lastPressedJumpTime = 0;
            pc.psm.lastOnGroundTime = 0;
            pc.psm.isJumpFalling = false;
		    
            pc.psm.activateMidairEntry = false;
        }
    }
    
    public void CalculateGravity()
    {
        if (IsMidair && Mathf.Abs(pc.rb.linearVelocity.y) < playerData.jumpHangTimeThreshold)
        {
            SetGravityScale(playerData.gravityScale * playerData.jumpHangGravityMult);
        }
        else if (pc.rb.linearVelocity.y < 0)
        {
            SetGravityScale(playerData.gravityScale * playerData.fallGravityMult);
            pc.rb.linearVelocity = new Vector3(pc.rb.linearVelocity.x, Mathf.Max(pc.rb.linearVelocity.y, -playerData.maxFallSpeed), pc.rb.linearVelocity.z);
        }
        else
        {
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
        //Debug.Log(gravity);
        pc.rb.AddForce(gravity, ForceMode.Acceleration);
    }

    public float GetMidairGravity()
    {
        return Mathf.Pow((float) numMidairAttacks / playerData.maxMidairAtks, playerData.midairAtkGravScale);
    }
    
    #endregion
    
    #region Look Methods
    
    public void TurnToLook()
    {
        if (!IsWalking || moveDirection.magnitude == 0) return;
	    
        if (!pc.cam.isLockedOn)
        {
            transform.rotation =
                EaseUtil.DampQuaternion(transform.rotation, Quaternion.LookRotation(moveDirection), 5f, 0.1f);
        }
        else
        {
            Vector3 lookDir = pc.cam.LockOnDirection.ZeroVector3Axis();
            
            if (lookDir.magnitude < 0.3f) return;

            transform.rotation =
                EaseUtil.DampQuaternion(transform.rotation, Quaternion.LookRotation(lookDir), 5f, 0.1f);
        }
    }
    
    #endregion
}
