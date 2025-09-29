using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    [HideInInspector]
    public PlayerController pc;
    
    #region Inspector Variables
    
    [Header("Locomotion Data")]
    
    public PlayerData playerData;
    
    private Dictionary<MovingStates, AttackConfig> attackDataDict;
    public AttackConfig attackData => movingState != MovingStates.NonCombat ? attackDataDict[movingState] : attackDataDict[MovingStates.DualSword];
    
    
    
    [HideInInspector] public MovingStates movingState;
    
    #endregion
    
    #region MOVE PARAMETERS
     
    [HideInInspector] public bool pauseMovement;
    
    [HideInInspector] public float lastOnGroundTime;
    [HideInInspector] public float lastDoubleJumpTime;
    [HideInInspector] public float lastPressedJumpTime;
    [HideInInspector] public float walkingTime;

    public bool IsGrounded =>
        Physics.CheckBox(groundCheckPoint.position, groundCheckSize, Quaternion.identity, GameManager.Instance.groundLayer);
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
    [HideInInspector] public bool isElementAttacking;

    [HideInInspector] public PlayerAttack currentPlayerAttack;
    
    [HideInInspector] public HashSet<LockOnTarget> enemiesHitThisAction = new();
    
    [HideInInspector] public List<ComboAction> comboChain = new();
    [HideInInspector] public float comboResetTimer = 0;
    [HideInInspector] public bool pauseComboReset = false;
    [HideInInspector] public float timeSinceLastAttack = 0;
    
    
    [HideInInspector] public int numMidairAttacks;
    public Dictionary<Attack, int> NumActionsUsed = new();

    public LockOnTarget NearestHEnemy => pc.cam.IsLockedOn ? pc.cam.TargetedEnemy : 
        GetClosestEnemyInCapsule(playerData.mediumRadius, playerData.heightRadius, 300f);
    
    [HideInInspector] public float dodgeTimer = 0;
    
    public Vector3 TruePlayerForward => pc.pac.animancer.transform.forward; // Use the model's forward for more accurate direction during attacks
    
    public HashSet<LockOnTarget> EnemiesInHit = new();
    public HashSet<EnemyHitbox> ParriedHitboxes = new();
    public HashSet<VFXHitbox> ParriedProjectiles = new();
    
    public bool PlayHitStopThisAction => EnemiesInHit.Count > 0;
    
    #endregion
    
    public Dictionary<KeyBind, KeyBindData> KeyMap;
    
    
    #region MonoBehaviour Callbacks
    
    
    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        
        pc.rb.useGravity = false;
        
        KeyMap = InputManager.KeyMap;
        
        attackDataDict = new Dictionary<MovingStates, AttackConfig>();
        foreach (AttackConfig attackConfig in pc.pi.attackDatas)
        {
            attackDataDict.TryAdd(attackConfig.movingState, attackConfig);
        }
        
        pc.sc.ChangeState(new PlayerMoving());
        
        InputManager.Instance.debug.performed += OnDebugInput;
        InputManager.Instance.elementAttack.performed += OnElementAttackInput;
        InputManager.Instance.elementAttack.canceled += OnElementAttackInput;
    }
    
    private void Update()
    {
        SetMoveValues();
        RotateToTarget();
        CheckGrounded();
        CheckAttackAction();
        
        pc.sc.PrintStates();
    }
    
    private void FixedUpdate()
    {
        ApplyGravity();
    }
    
    #endregion
    
    #region Input Callbacks
    
    private void OnDebugInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PlayerIsHit(new HitInstance()
            {
                force = new Vector3(15, 0),
                horizontalDirection = -TruePlayerForward.ToVector2(),
                damage = 20,
            });
        }
    }
    
    private void OnElementAttackInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isElementAttacking = true;
            HUDMenuUI.Instance.ActivateElementalAttackIcons();
        }
        else if (context.canceled)
        {
            isElementAttacking = false;
            HUDMenuUI.Instance.DeactivateElementalAttackIcons();
        }
    }
    
    #endregion
    
    #region Info Methods
    
    private void SetMoveValues()
    {
        if (!CombatManager.Instance.entitiesStopped) moveInput = InputManager.Instance.movement.ReadValue<Vector2>();

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

    public LockOnTarget GetClosestEnemyInCapsule(float radius, float height, float angle = 360f)
    {
        HashSet<LockOnTarget> enemySet = Physics.OverlapCapsule(groundCheckPoint.position - Vector3.up * height, 
            groundCheckPoint.position + Vector3.up * height, radius, GameManager.Instance.enemyLayer).Select(e =>
        {
            e.TryGetComponent(out LockOnTarget d);
            return d;
        }).ToHashSet();
        enemySet.RemoveWhere(e => !e);
        
        if (angle < 360)
        {
            enemySet.RemoveWhere(e => !(e.TargetedPosition() - pc.transform.position).ToVector2().
                IsInDirectionCone(TruePlayerForward.ToVector2(), angle));
        }

        enemySet = enemySet.OrderBy(e => Vector3.Distance(pc.transform.position, e.TargetedPosition())).ToHashSet();
        return enemySet.FirstOrDefault();
    }
    
    public LockOnTarget[] GetAllEnemiesInCapsule(float radius, float height, float angle = 360f)
    {
        HashSet<LockOnTarget> enemySet = Physics.OverlapCapsule(groundCheckPoint.position - Vector3.up * height, 
            groundCheckPoint.position + Vector3.up * height, radius, GameManager.Instance.enemyLayer).Select(e =>
        {
            e.TryGetComponent(out LockOnTarget d);
            return d;
        }).ToHashSet();
        enemySet.RemoveWhere(e => !e);
        
        if (angle < 360)
        {
            enemySet.RemoveWhere(e => !(e.TargetedPosition() - pc.transform.position).ToVector2().
                IsInDirectionCone(TruePlayerForward.ToVector2(), angle));
        }

        enemySet = enemySet.OrderBy(e => Vector3.Distance(pc.transform.position, e.TargetedPosition())).ToHashSet();
        return enemySet.ToArray();
    }
    
    #endregion
    
    #region Attack Methods
    
    public void ResetActions()
    {
        NumActionsUsed.Clear();
        
        numMidairAttacks = 0;
    }

    private void SetActionTimers()
    {
        timeSinceLastAttack += Time.deltaTime;

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

    private bool CheckParryAction()
    {
        if (movingState == MovingStates.NonCombat || pauseMovement) return false;

        PlayerAttack attack = (PlayerAttack) attackData.parryAttack;
        if (!AttackIsAvailable(attack)) return false;
        
        ParriedHitboxes.Clear();
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, playerData.largeRadius, GameManager.Instance.enemyLayer);
        
        bool foundParry = false;
        foreach (var col in colliders)
        {
            if (!col.TryGetComponent(out EnemyHitbox eh)) continue;
            if (!eh.ts.parryWindowActive) continue;
            if (!eh.GetCurrentAttackInfo().attack.isParryable) continue;
            foundParry = true;
            
            ParriedHitboxes.Add(eh);
        }
        
        if (!foundParry) return false;
        
        pc.pi.isInvincible = true;
        BeginAttack(attack);
        return true;
    }

    private bool CheckProjectileParryAction()
    {
        if (movingState == MovingStates.NonCombat || pauseMovement || !canAttack) return false;
        
        PlayerAttack attack = (PlayerAttack) attackData.projectileParryAttack;
        if (!AttackIsAvailable(attack)) return false;
        
        ParriedProjectiles.Clear();
        Collider[] colliders = Physics.OverlapSphere(transform.position, playerData.mediumRadius);
        
        Debug.Log("Checking Projectile Parry: Found " + colliders.Length + " colliders");
        
        bool foundParry = false;
        foreach (var col in colliders)
        {
            if (!col.TryGetComponent(out VFXHitbox h)) continue;
            if (!h.HitDetector.vfx.IsEnemyVFX()) continue;
            if (!h.HitDetector.vfx.vfxEnabled || !h.HitDetector.vfx.activeHitbox) continue;
            foundParry = true;
            
            ParriedProjectiles.Add(h);
        }
        
        if (!foundParry) return false;
        pc.pi.isInvincible = true;
        BeginAttack(attack);
        return true;
    }

    private bool CheckMobilityAction()
    {

        if (movingState != MovingStates.NonCombat && !pauseMovement)
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

                if (IsGrounded || GetClosestEnemyInCapsule(playerData.mediumRadius, playerData.heightRadius) == null) continue;

                ResetActions();
                BeginAttack(attack);
                return true;
            }

            #endregion
        }

        #region Double Jump
        
        if (attackData.doubleJumpEnabled && CanDoubleJump && KeyMap[KeyBind.South].action() && !isElementAttacking)
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
        
        if (CombatManager.Instance.entitiesStopped) return;
        
        if (CheckMobilityAction()) return;
        
        if (CheckParryAction()) return;
        if (CheckProjectileParryAction()) return;
        
        if (movingState == MovingStates.NonCombat) return;
        
        if (pauseMovement) return;
        
        ComboAction possibleCombo = CheckComboAction();
        if (possibleCombo == null && !InputManager.AnyKeyPressed()) return;

        PlayerAttack a = null;
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
        
        #region Elemental Attack

        Attack[] elementAttacks = pc.pi.CurrentLoadout.elementLoadout?.GetElementAttacks(pc.pi.currentElementEffect, movingState);

        SpiritAttack s = null;
        foreach (var e in elementAttacks)
        {
            if (e != null && AttackIsAvailable(e))
            {
                if (e is SpiritAttack spiritAttack)
                {
                    s = spiritAttack;
                }

                if (e is PlayerAttack playerAttack)
                {
                    a = playerAttack;
                }
            }
        }
        
        if (s != null)
        {
            BeginSpiritAttack(s);
        }

        #endregion
        
        if (!s && !a && possibleCombo == null && !starter)
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
                    KeyBind pressedKey = nextAction.playerAttack.keyBinds.Contains(KeyBind.West) ? KeyBind.West : KeyBind.North;
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
                    KeyBind mashKey = nextAction.playerAttack.keyBinds.Contains(KeyBind.West) ? KeyBind.West : KeyBind.North;
                    
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
        
        timeSinceLastAttack = 0f;
        
        if (pc.sc.GetCurrentState() is PlayerMoving)
        {
            pc.sc.Interrupt(new PlayerAttacking(action.playerAttack));
        }
        else if (pc.sc.GetCurrentState() is PlayerAttacking)
        {
            pc.oae.KillObjectCoroutines();
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
        
        timeSinceLastAttack = 0f;
        
        if (pc.sc.GetCurrentState() is PlayerMoving)
        {
            pc.sc.Interrupt(new PlayerAttacking(playerAttack));
        }
        else if (pc.sc.GetCurrentState() is PlayerAttacking)
        {
            pc.oae.KillObjectCoroutines();
            pc.sc.ChangeState(new PlayerAttacking(playerAttack));
        }
    }
    
    public bool AttackIsAvailable(Attack attack)
    {
        if (attack == null) return false;
        
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
        
        if (attack.requireElementTrigger != isElementAttacking) return false;
        
        if (attack.isMidair != NBool.Both && IsMidair != attack.isMidair.IsTrue()) return false;

        if (attack.keyBinds.Any(k => !KeyMap[k].action())) return false;
        
        Vector2 direction = attack.applyTargetDirection ? StandardizedMoveDir : moveInput;
        if (attack.inputDirection != Vector2.zero && !direction.IsInDirectionCone(attack.inputDirection, 92f)) return false;
        
        if (!attack.HasEnoughCharge(pc)) return false;
        
        if (!pc.pi.FinishedElementCooldown(attack)) return false;
        
        if (!pc.pi.CanUseFinisher(attack)) return false;

        return true;
    }
    
    public void SwapToUltimate()
    {
        pc.wc.SwitchWeapon(pc.pac.MovingAnims.ultWeapons, pc.pac.MovingAnims.ultNextState);
		
        movingState = pc.pac.MovingAnims.ultNextState;
    }

    public void SwapToNonCombat()
    {
        if (pc.pac.MovingAnims.swapClip != null)
        {
            pc.psm.pauseMovement = true;

            pc.pac.SwitchAnimState(WalkingAnimStates.Swapping, () =>
            {
                pc.pac.SwitchAnimState(WalkingAnimStates.Idle);
                pc.psm.pauseMovement = false;
            });
        }
        
        pc.wc.SwitchWeapon(pc.pac.MovingAnims.swapWeapons, pc.pac.MovingAnims.swapNextState);
        
        pc.psm.movingState = pc.pac.MovingAnims.swapNextState;
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
            pc.oae.KillObjectCoroutines();
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
            
            if (lookDir.magnitude < 0.1f) return;

            transform.rotation =
                EaseUtil.DampQuaternion(transform.rotation, Quaternion.LookRotation(lookDir), 5f, 0.1f);
        }
    }

    private void RotateToTarget()
    {
        Vector3 lookDir = pc.cam.IsLockedOn && IsMidair
            ? (pc.cam.TargetedEnemy.TargetedPosition() - pc.transform.position).ZeroVector3Axis('x')
            : pc.transform.forward;
        
        float lookLimit = playerData.lockOnRotateLimit * Mathf.Deg2Rad;
        
        if (pc.cam.IsLockedOn && IsMidair && Vector3.Angle(pc.transform.forward, lookDir) > lookLimit)
        {
            lookDir = Vector3.RotateTowards(pc.transform.forward, lookDir, lookLimit, 0.0f);
        }
        
        pc.pac.animancer.transform.rotation =
            EaseUtil.DampQuaternion(pc.pac.animancer.transform.rotation, Quaternion.LookRotation(lookDir), 5f, 0.1f);
    }
    
    #endregion

    #region Collision Methods

    public void CheckEnemyCollision(Collider other)
    {
        if (pc.pi.isInvincible) return;
        if (!other.TryGetComponent(out EnemyHitbox eh)) return;
        if (pc.sc.IsState<PlayerHit>()) return;
        if (!eh.activeHitbox) return;
        
        
        PlayerIsHit(new HitInstance()
                    {
                        force = eh.GetCurrentAttackInfo().attack.attackKnockback,
                        horizontalDirection = (transform.position - eh.ts.transform.position).ToVector2().normalized,
                        damage = eh.GetCurrentAttackInfo().attack.damage * eh.GetCurrentAttackInfo().damageInfo.damageMultiplier
                    });
    }

    public void CheckEnemyProjectileCollision(EnemyVFXHitDetector evhd)
    {
        if (pc.pi.isInvincible) return;
        if (pc.sc.IsState<PlayerHit>()) return;
        if (!evhd.vfx.activeHitbox || !evhd.vfx.vfxEnabled) return;
        
        PlayerIsHit(new HitInstance()
        {
            force = evhd.attackInfo.attack.attackKnockback,
            horizontalDirection = (transform.position - evhd.vfx.transform.position).ToVector2().normalized,
            damage = evhd.attackInfo.attack.damage * evhd.attackInfo.damageInfo.damageMultiplier
        });
    }
    
    private void OnTriggerEnter(Collider other)
    {
        CheckEnemyCollision(other);
    }

    private void OnTriggerStay(Collider other)
    {
        CheckEnemyCollision(other);
    }

    #endregion
}
