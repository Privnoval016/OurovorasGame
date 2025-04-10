using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public bool CanDoubleJump => lastDoubleJumpTime <= 0 && !isDoubleJumpUsed && canAttack && IsMidair;
    
    //Walk
    public bool IsWalking => moveInput.magnitude > 0;
    public bool IsSprinting => !pc.cam.IsLockedOn && IsWalking && walkingTime > playerData.sprintBuildupLength;
    
    //Jump
    [HideInInspector] public bool isJumping;
    [HideInInspector] public bool isJumpFalling;
    public bool IsJumpTriggered => lastPressedJumpTime > 0;
    
    
    
    [HideInInspector] public float gravityScale;

    #endregion
    
    #region INPUT PARAMETERS
    
    public float heldDirResetTime = 0.1f;
    
    [HideInInspector] public Vector2 moveInput;
    [HideInInspector] public Vector3 moveDirection;

    public Vector2 StandardizedMoveDir => moveInput.Rotate(-transform.right.ToVector2().ToAngle()).
                                                Rotate(pc.cam.transform.right.ToVector2().ToAngle()).normalized;

    public Queue<Vector2> inputDirQueue = new();
    public Queue<float> inputTimeQueue = new();
    [HideInInspector] public Vector2 lastInputDir;
    
    #endregion
    
    #region GROUND CHECK PARAMETERS
   
    [Header("Checks")] 
    [SerializeField] public Transform groundCheckPoint;
    [SerializeField] public Vector3 groundCheckSize = new Vector3(0.49f, 0.3f, 0.49f);
    
    #endregion

    #region ATTACK PARAMETERS

    [HideInInspector] public bool canAttack;
    [FormerlySerializedAs("currentAttack")] [HideInInspector] public PlayerAttack currentPlayerAttack;
    
    [HideInInspector] public HashSet<LockOnTarget> enemiesHitThisAction = new();
    
    [HideInInspector] public List<ComboAction> comboChain = new();
    [HideInInspector] public float comboResetTimer = 0;
    [HideInInspector] public bool pauseComboReset = false;
    
    
    [HideInInspector] public int numMidairAttacks;
    public Dictionary<Attack, int> NumActionsUsed = new();

    public LockOnTarget NearestHEnemy => pc.cam.IsLockedOn ? pc.cam.TargetedEnemy : GetClosestEnemyInRadius(playerData.mediumRadius, 190f);
    
    [HideInInspector] public float dodgeTimer = 0;
    
    #endregion

    
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
        
        pc.sc.ChangeState(new PlayerMoving());

        InputManager.Instance.swapElementLeft.performed += OnSwapElementLeft;
        InputManager.Instance.swapElementRight.performed += OnSwapElementRight;
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
    
    #region Input Callbacks

    private void OnSwapElementLeft(InputAction.CallbackContext context)
    {
        int index = pc.elementEffectOrder.IndexOf(pc.CurrentElementEffect);
        pc.CurrentElementEffect = pc.elementEffectOrder.ShiftIndex(index, -1);
    }
    
    private void OnSwapElementRight(InputAction.CallbackContext context)
    {
        int index = pc.elementEffectOrder.IndexOf(pc.CurrentElementEffect);
        pc.CurrentElementEffect = pc.elementEffectOrder.ShiftIndex(index, 1);
    }
    
    #endregion
    
    #region Info Methods
    
    private void SetMoveValues()
    {
        moveInput = InputManager.Instance.movement.ReadValue<Vector2>();

        inputDirQueue.Enqueue(StandardizedMoveDir);
        inputTimeQueue.Enqueue(Time.time);

        while (inputTimeQueue.Peek() < Time.time - heldDirResetTime)
        {
            inputDirQueue.Dequeue();
            inputTimeQueue.Dequeue();
        }

        lastInputDir = inputDirQueue.Peek();
        
        //Debug.Log(lastInputDir);

    }

    public LockOnTarget GetClosestEnemyInRadius(float radius, float angle = 360f)
    {
        HashSet<LockOnTarget> enemySet = Physics.OverlapSphere(transform.position, radius, enemyLayer).Select(e =>
        {
            e.TryGetComponent(out LockOnTarget d);
            return d;
        }).ToHashSet();
        enemySet.RemoveWhere(e => !e);
        
        if (angle < 360)
        {
            enemySet.RemoveWhere(e => !(e.TargetedPosition() - pc.transform.position).ToVector2().
                IsInDirectionCone(pc.transform.forward.ToVector2(), angle));
        }

        enemySet = enemySet.OrderBy(e => Vector3.Distance(pc.transform.position, e.TargetedPosition())).ToHashSet();
        return enemySet.FirstOrDefault();
    }
    
    public LockOnTarget[] GetAllEnemiesInRadius(float radius, float angle = 360f)
    {
        HashSet<LockOnTarget> enemySet = Physics.OverlapSphere(transform.position, radius, enemyLayer).Select(e =>
        {
            e.TryGetComponent(out LockOnTarget d);
            return d;
        }).ToHashSet();
        enemySet.RemoveWhere(e => !e);
        
        if (angle < 360)
        {
            enemySet.RemoveWhere(e => !(e.TargetedPosition() - pc.transform.position).ToVector2().
                IsInDirectionCone(pc.transform.forward.ToVector2(), angle));
        }

        enemySet = enemySet.OrderBy(e => Vector3.Distance(pc.transform.position, e.TargetedPosition())).ToHashSet();
        return enemySet.ToArray();
    }
    
    #endregion
    
    #region Attack Methods

    public void InvokeOnAttack(PlayerAttack a)
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

            List<PlayerAttack> validDodges = new();
            if (dodgeTimer > attackData.dodgeCoolDown)
            {
                foreach (PlayerAttack attack in attackData.dodgeAttacks)
                {
                    if (!AttackIsAvailable(attack)) continue;

                    dodgeTimer = 0;
                    validDodges.Add(attack);
                }
            }
            if (validDodges.Count > 0)
            {
                PlayerAttack action = validDodges.OrderBy(d => Attack.AttackTypePriority.IndexOf(d.attackType)).First();
                if (action != null)
                {
                    foreach (PlayerAttack attack in attackData.dodgeAttacks)
                    {
                        if (attack != action && NumActionsUsed.ContainsKey(attack)) NumActionsUsed[attack] = attack.maxUses;
                    }
                    BeginAttack(action);
                    return true;
                }
            }

            #endregion

            #region Enemy Step

            foreach (PlayerAttack attack in attackData.enemyStepAttacks)
            {
                if (!AttackIsAvailable(attack)) continue;
                
                if (comboChain.LastOrDefault()?.playerAttack == attack) continue;

                if (IsGrounded || GetClosestEnemyInRadius(playerData.mediumRadius) == null) continue;

                ResetActions();
                BeginAttack(attack);
                return true;
            }

            #endregion
        }

        #region Double Jump
        
        if (attackData.doubleJumpEnabled && CanDoubleJump && KeyMap[KeyBind.Jump].action())
        {
            Debug.Log("Double Jump");
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

        PlayerAttack a = null;
        ComboAction possibleCombo = CheckComboAction();
        PlayerAttack starter = null;
        
        #region Directional Attacks

        foreach (PlayerAttack attack in attackData.AttackMap[AttackTypes.Directional])
        {
            if (!AttackIsAvailable(attack)) continue;
            
            a = attack;
        }
        
        #endregion
                
        #region Special Attacks

        if (a == null)
        {
            foreach (PlayerAttack attack in attackData.AttackMap[AttackTypes.Special])
            {
                if (!AttackIsAvailable(attack)) continue;

                a = attack;
            }
        }

        #endregion
        
        #region Combo Starters

        if (possibleCombo == null)
        {
            foreach (PlayerAttack attack in attackData.AttackMap[AttackTypes.Heavy])
            {
                if (!AttackIsAvailable(attack)) continue;

                starter = attack;
            }

            foreach (PlayerAttack attack in attackData.AttackMap[AttackTypes.Light])
            {
                if (!AttackIsAvailable(attack)) continue;

                starter = attack;
            }

            foreach (PlayerAttack attack in attackData.AttackMap[AttackTypes.Midair])
            {
                if (!AttackIsAvailable(attack)) continue;

                starter = attack;
            }
        }

        #endregion
        
        if (!a && possibleCombo == null && !starter)
            pc.spirit.CheckSpiritAction();
        
        if (!canAttack) return;

        if (a != null)
        {
            comboChain.Clear();
            BeginAttack(a);
            return;
        }
        
        if (possibleCombo != null)
        {
            BeginComboAttack(possibleCombo);
            return;
        }
        
        if (starter != null)
        {
            comboChain.Clear();
            BeginAttack(starter, ComboActionType.Press);
        }

    }

    private ComboAction CheckComboAction()
    {
        List<ComboAction> possibleActions = new();
        
        foreach (ComboConfig combo in attackData.comboAttacks)
        {
            if (!combo.isEnabled) continue;
            if (comboChain.Count >= combo.comboActions.Length) continue;

            List<PlayerAttack> attacks = combo.comboActions.Select(a => a.playerAttack).ToList();
            attacks = attacks.Take(comboChain.Count).ToList();
            List<PlayerAttack> comboAttacks = comboChain.Select(a => a.playerAttack).ToList();
            if (comboAttacks.Except(attacks).Any()) continue;
            
            ComboAction nextAction = combo.comboActions[comboChain.Count];
            switch (nextAction.actionType)
            {
                case ComboActionType.Press:
                case ComboActionType.Release:
                    if (!AttackIsAvailable(nextAction.playerAttack)) continue;
                    possibleActions.Add(nextAction);
                    
                    break;
                
                case ComboActionType.Hold:
                    KeyBind pressedKey = nextAction.playerAttack.keyBinds.Contains(KeyBind.LightAttack) ? KeyBind.LightAttack : KeyBind.HeavyAttack;
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
                    KeyBind mashKey = nextAction.playerAttack.keyBinds.Contains(KeyBind.LightAttack) ? KeyBind.LightAttack : KeyBind.HeavyAttack;
                    
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
            return action;
        }
        
        return null;
    }
    
    private void BeginSpiritAttack(SpiritAttack spiritAttack)
    {
        if (spiritAttack == null) return;
        
        pc.spirit.InvokeOnSpiritAttack(spiritAttack);
    }

    private void BeginComboAttack(ComboAction action)
    {
        comboResetTimer = 0;
        comboChain.Add(action);
        
        if (action == null) return;
        
        currentPlayerAttack = action.playerAttack;
        
        pc.rb.linearVelocity = Vector3.zero;
        
        if (NumActionsUsed.ContainsKey(action.playerAttack)) NumActionsUsed[action.playerAttack]++;
        
        if (pc.sc.GetCurrentState() is PlayerMoving)
        {
            pc.sc.Interrupt(new PlayerAttacking(action.playerAttack));
        }
        else if (pc.sc.GetCurrentState() is PlayerAttacking)
        {
            OnAttackEvents.Instance.KillObjectCoroutines();
            pc.sc.ChangeState(new PlayerAttacking(action.playerAttack));
        }
    }

    private void BeginAttack(PlayerAttack playerAttack, ComboActionType type = ComboActionType.Special, float actionTime = 0)
    {
        comboResetTimer = 0;
        comboChain.Add(new ComboAction()
        {
            actionType = type,
            playerAttack = playerAttack,
            time = actionTime
        });
        
        currentPlayerAttack = playerAttack;
        
        if (playerAttack == null) return;
        
        pc.rb.linearVelocity = Vector3.zero;
        
        if (NumActionsUsed.ContainsKey(playerAttack)) NumActionsUsed[playerAttack]++;
        
        if (pc.sc.GetCurrentState() is PlayerMoving)
        {
            pc.sc.Interrupt(new PlayerAttacking(playerAttack));
        }
        else if (pc.sc.GetCurrentState() is PlayerAttacking)
        {
            OnAttackEvents.Instance.KillObjectCoroutines();
            pc.sc.ChangeState(new PlayerAttacking(playerAttack));
        }
    }
    
    public bool AttackIsAvailable(Attack attack)
    {
        if (!attack.isEnabled) return false;
        
        if (!NumActionsUsed.ContainsKey(attack) && attack.maxUses > 0)
        {
            NumActionsUsed.Add(attack, 0);
        }

        if (attack.comboDirection != Vector2.zero)
        {
            if (!attack.comboDirection.IsInDirectionCone(lastInputDir, 92f)) return false;
        }
        
        if (NumActionsUsed.ContainsKey(attack) && NumActionsUsed[attack] >= attack.maxUses) return false;
        
        if (attack.isLockedOn && !pc.cam.IsLockedOn) return false;
        
        if (attack.isMidair != NBool.Both && IsMidair != attack.isMidair.IsTrue()) return false;

        if (!attack.keyBinds.Any(k => KeyMap[k].action())) return false;
        
        Vector2 direction = attack.applyTargetDirection ? StandardizedMoveDir : moveInput;
        if (attack.inputDirection != Vector2.zero && !direction.IsInDirectionCone(attack.inputDirection, 92f)) return false;

        return true;
    }

    #endregion
    
    #region Hit Methods

    public void PlayerIsHit(HitInstance hit)
    {
        if (pc.sc.GetCurrentState() is PlayerMoving)
        {
            pc.sc.Interrupt(new PlayerHit(hit));
        }
        else
        {
            OnAttackEvents.Instance.KillObjectCoroutines();
            pc.sc.ChangeState(new PlayerHit(hit));
        }
    }
    
    #endregion
    
    #region Gravity Methods

    public void Jump(float force, bool switchAnim = true, WalkingAnimStates animState = WalkingAnimStates.Jumping)
    {
        if (switchAnim) pc.pac.SwitchAnimState(animState, () => pc.pac.SwitchAnimState(WalkingAnimStates.Falling));

        #region Perform Jump
        // if (pc.rb.linearVelocity.y < 0)
        //     force -= pc.rb.linearVelocity.y;
        
        pc.rb.linearVelocity = pc.rb.linearVelocity.ZeroVector3Axis();
		
        pc.rb.AddForce(Vector3.up * force, ForceMode.VelocityChange);
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
    }
    
    public void CalculateGravity()
    {
        if (IsMidair && Mathf.Abs(pc.rb.linearVelocity.y) < playerData.jumpHangSpeedThreshold)
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
        if (moveDirection.magnitude == 0) return;
	    
        if (!pc.cam.IsLockedOn)
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
