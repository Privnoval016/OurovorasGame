using System.Collections.Generic;
using Extensions.StateMachine;
using Extensions.Timers;
using Extensions.UtilityAI;
using UnityEngine;

[RequireComponent(typeof(StateController<EnemyState>))]
public class EnemyStateMachine : AIBrainUser
{
    #region AI Components

    [HideInInspector] public StateController<EnemyState> sc;
    public AIBrain AIBrain;

    [Header("Attack AI")]
    public List<AIActionBase> actions = new();

    public EnemySensor sensor;
    public TickTimer ThinkTimer;
    public EnemyAIActionBase currentAction;

    [Header("Context Settings")]
    [Tooltip("Normalise TimeSinceLastAction against this window (seconds). " +
             "e.g. window=2 means threshold 0.5 = 1s cooldown between actions.")]
    public float actionCooldownWindow = 2f;
    [Tooltip("Normalise SameActionStreak against this cap.")]
    public int streakCap = 4;

    #endregion

    #region Inspector Components

    [Header("Enemy Components")]
    [HideInInspector] public EnemyController ts;
    [SerializeField] private EnemyData enemyData;
    public EnemyAnimData enemyAnimData;

    #endregion

    #region Movement Properties

    [Header("Movement")]
    [HideInInspector] public Vector3 moveDirection;
    public Transform[] wanderPoints;
    [HideInInspector] public Vector3 currentWanderPoint;
    [HideInInspector] public int wanderIndex = 0;

    public float MoveSpeed =>
        enemyData.speed * ts.stats.EvaluatedStats.GetStatusEffectMultiplier(StatusEffectTargets.Speed);

    public bool CanMove => ts.pe.physicsInteract;

    #endregion

    #region Attack / History

    public bool IsAttacking;

    private string _lastActionName = string.Empty;
    private string _secondLastActionName = string.Empty;
    private int _sameActionStreak = 0;
    private float _timeSinceLastAction = 0f;

    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        sc = new StateController<EnemyState>(this);
        ts = GetComponent<EnemyController>();
        AIBrain = new AIBrain(this);
        ThinkTimer = new TickTimer(0.1f);
    }

    private void Start()
    {
        AIBrain.Initialize();
        sc.ChangeState(new EnemyInitialState());
        ts.pe.onHit += HitStateAction;
        ts.pe.onStagger += StaggerStateAction;
        ThinkTimer.Reset();
        ThinkTimer.OnTick += PerformBestAction;
        ThinkTimer.Start();
    }

    private void Update()
    {
        _timeSinceLastAction += Time.deltaTime;
    }

    #endregion

    #region AIBrainUser overrides

    public override ContextPayload[] OnContextUpdate()
    {
        float maxHealth = ts.stats.GetStat(InnateStat.MaxHealth);
        Transform playerTransform = sensor?.GetNearestTarget(EnemyContextKeys.Player);

        float distNorm = 0f, angleNorm = 0f;
        bool playerAirborne = false, playerAttacking = false, playerDodging = false;

        if (playerTransform != null)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            float radius = sensor != null ? sensor.detectionRadius : 20f;
            distNorm = Mathf.Clamp01(new Vector3(toPlayer.x, 0f, toPlayer.z).magnitude / radius);
            float dot = Vector3.Dot(transform.forward, toPlayer.normalized);
            angleNorm = Mathf.Clamp01(1f - (dot + 1f) * 0.5f);

            if (playerTransform.TryGetComponent(out PlayerController pc))
            {
                playerAirborne  = pc.psm.isJumping || pc.psm.isJumpFalling;
                playerAttacking = pc.psm.canAttack == false; // canAttack is false while mid-attack
                playerDodging   = !pc.psm.DodgeTimer.IsFinished;
            }
        }

        return new ContextPayload[]
        {
            (EnemyContextKeys.SelfHealthNorm,          ts.stats.CurrentHealth / Mathf.Max(maxHealth, 1f)),
            (EnemyContextKeys.DistanceToPlayerNorm,    distNorm),
            (EnemyContextKeys.AngleToPlayerNorm,       angleNorm),
            (EnemyContextKeys.TimeSinceLastActionNorm, Mathf.Clamp01(_timeSinceLastAction / Mathf.Max(actionCooldownWindow, 0.1f))),
            (EnemyContextKeys.SameActionStreakNorm,    Mathf.Clamp01((float)_sameActionStreak / Mathf.Max(streakCap, 1))),
            (EnemyContextKeys.IsShielded,              ResolveIsShielded()),
            (EnemyContextKeys.PlayerIsAirborne,        playerAirborne),
            (EnemyContextKeys.IsStaggered,             sc.IsState<EnemyStagger>()),
            (EnemyContextKeys.IsAggro,                 IsAttacking),
            (EnemyContextKeys.PlayerIsAttacking,       playerAttacking),
            (EnemyContextKeys.PlayerIsDodging,         playerDodging),
            (EnemyContextKeys.LastActionName,          _lastActionName),
            (EnemyContextKeys.SecondLastActionName,    _secondLastActionName),
            (EnemyContextKeys.SameActionStreak,        _sameActionStreak),
        };
    }

    /** <summary>
     * Returns true when the enemy has a <see cref="ShieldComponent"/> whose current
     * percentage is greater than zero, i.e. the shield has not been fully broken.
     * </summary>
     */
    private bool ResolveIsShielded()
    {
        if (ts.stats == null) return false;
        var shield = ts.stats.DamageableComponents?.GetComponent<ShieldComponent>();
        if (shield == null) return false;
        return shield.CurrentShieldPercentage.Value > 0f;
    }

    public override List<AIActionBase> GetActions() => actions;
    public override ISensor GetSensor() => sensor;
    public override AIBrain GetBrain() => AIBrain;
    public override string CurrentActionName => currentAction != null ? currentAction.name : null;

    public override void ExecuteNewAction(AIActionBase action, EnemyContext context, float utility)
    {
        // Only execute when the current action has finished (state returned to EnemyInitialState).
        // This prevents the brain tick from interrupting a running movement action.
        if (sc.IsState<EnemyActing>()) return;

        if (action is EnemyAIActionBase enemyAction)
        {
            // Only reset cooldown timer / streak for committed actions (attacks, stun, idle).
            // Movement fill actions override ResetsActionTimer = false so attack cooldowns
            // keep accumulating through orbit and close-dash phases.
            if (enemyAction.ResetsActionTimer)
            {
                string starting = action.name;
                _secondLastActionName = _lastActionName;
                _sameActionStreak = starting == _lastActionName ? _sameActionStreak + 1 : 1;
                _lastActionName = starting;
                _timeSinceLastAction = 0f;
            }

            sc.ChangeState(new EnemyActing(Instantiate(enemyAction), context));
        }
    }

    #endregion

    #region Inheritance Methods

    protected virtual void HitStateAction(ElementEffect element, PlayerController pc, Attack a, Transform attackerTransform, int actionIndex = 0)
    {
        if (ts.pe.knockbackImmune) return;
        sc.ChangeState(new EnemyHit());
    }

    protected virtual void StaggerStateAction(ElementEffect element, PlayerController pc, Attack a, Transform attackerTransform, int actionIndex = 0)
    {

        ts.animListener.DeactivateAllHitboxes();
        sc.ChangeState(new EnemyStagger());
    }

    public virtual void HitboxActivate(int hitboxIndex = 0)
    {
        if (!IsAttacking) return;

        ts.attackHitboxes[hitboxIndex].activeHitbox = true;
    }

    public virtual void HitboxDeactivate(int hitboxIndex = 0)
    {
        ts.attackHitboxes[hitboxIndex].activeHitbox = false;
    }

    public virtual void AllHitboxesActivate()
    {
        if (!IsAttacking) return;

        foreach (EnemyHitbox eh in ts.attackHitboxes)
        {
            eh.activeHitbox = true;
        }
    }

    public virtual void AllHitboxesDeactivate()
    {
        foreach (EnemyHitbox eh in ts.attackHitboxes)
        {
            eh.activeHitbox = false;
        }
    }

    public virtual void ParryWindowActivate()
    {
        if (!IsAttacking) return;

        ts.parryWindowActive = true;
    }

    public virtual void ParryWindowDeactivate()
    {
        ts.parryWindowActive = false;
    }

    #endregion

    #region Movement Methods

    /** <summary>
     * Move the enemy toward <paramref name="destination"/> using A* path steering.
     * Sets the nav destination and feeds the resulting steering direction into the motor.
     * </summary>
     */
    public void MoveToDestination(Vector3 destination, float speedMultiplier = 1f)
    {
        if (Services.Get<CombatSystem>().EntitiesStopped) return;
        if (!CanMove) return;

        ts.nav.SetDestination(destination);
        Vector3 dir = ts.nav.GetSteeringDirection();
        if (dir == Vector3.zero) return;
        moveDirection = dir;
        ts.motor.Move(dir, speedMultiplier);
    }

    /** <summary>
     * Move in a raw <paramref name="direction"/> (normalised XZ) without pathfinding.
     * Used for short-range or direct movement primitives.
     * </summary>
     */
    public void MoveInDirection(Vector3 direction, float speedMultiplier = 1f)
    {
        if (Services.Get<CombatSystem>().EntitiesStopped) return;
        if (!CanMove) return;

        moveDirection = direction;
        ts.motor.Move(direction, speedMultiplier);
    }

    /** <summary>Strafe perpendicular to <paramref name="pivot"/> while facing it.</summary> */
    public void StrafeAround(Vector3 pivot, float sign = 1f, float speedMultiplier = 1f)
    {
        if (!CanMove) return;
        moveDirection = ts.motor.CurrentMoveDirection;
        ts.motor.Strafe(pivot, sign, speedMultiplier);
    }

    /** <summary>Orbit <paramref name="center"/> at <paramref name="angularSpeed"/> degrees/s.</summary> */
    public void OrbitAround(Vector3 center, float angularSpeed = 60f)
    {
        if (!CanMove) return;
        ts.motor.Orbit(center, angularSpeed);
        moveDirection = ts.motor.CurrentMoveDirection;
    }

    /** <summary>Launch backward away from <paramref name="threatPosition"/>.</summary> */
    public void BackJump(Vector3 threatPosition)
    {
        if (!CanMove) return;
        ts.motor.BackJump(threatPosition);
    }

    /** <summary>Move directly away from <paramref name="threatPosition"/>.</summary> */
    public void RetreatFrom(Vector3 threatPosition, float speedMultiplier = 1f)
    {
        if (!CanMove) return;
        ts.motor.RetreatFrom(threatPosition, speedMultiplier);
        moveDirection = ts.motor.CurrentMoveDirection;
    }

    /** <summary>Charge straight toward <paramref name="target"/>, bypassing pathfinding.</summary> */
    public void ChargeToward(Vector3 target, float speedMultiplier = 1f)
    {
        if (!CanMove) return;
        ts.motor.ChargeToward(target, speedMultiplier);
        moveDirection = ts.motor.CurrentMoveDirection;
    }

    /** <summary>Tick wander; returns the desired direction or zero when idle between targets.</summary> */
    public Vector3 Wander()
    {
        if (!CanMove) return Vector3.zero;
        Vector3 dir = ts.motor.TickWander();
        if (dir != Vector3.zero) MoveInDirection(dir);
        return dir;
    }

    /** <summary>Decelerate smoothly to a stop.</summary> */
    public void Brake() => ts.motor.Brake();

    /** <summary>Hard-stop all horizontal movement.</summary> */
    public void StopMovement() => ts.motor.Stop();

    /** <summary>Rotate to face the current <see cref="moveDirection"/>.</summary> */
    public void TurnToLook()
    {
        if (moveDirection.sqrMagnitude < 0.0001f) return;
        ts.motor.RotateToward(moveDirection);
    }

    /** <summary>Rotate to face a world-space <paramref name="position"/>.</summary> */
    public void TurnToPosition(Vector3 position)
    {
        ts.motor.FacePosition(position);
    }

    #endregion

    #region Check Methods

    // public void CheckToFollowPlayer(float viewRadius, float viewAngle)
    // {
    //     if (sc.GetCurrentState() is EnemyHit) return;
    //
    //     Collider[] colliders = Physics.OverlapSphere(transform.position, viewRadius);
    //
    //     foreach (Collider c in colliders)
    //     {
    //         if ((c.transform.position - transform.position).IsInDirectionCone(transform.forward, viewAngle))
    //         {
    //             if (c.TryGetComponent(out PlayerController player))
    //             {
    //                 ts.pc = player;
    //                 sc.ChangeState(new EnemyFollow());
    //                 return;
    //             }
    //         }
    //     }
    // }
    //
    // public bool TargetInRange(Vector3 position, float distance, float viewAngle)
    // {
    //     if (Vector3.Distance(position, transform.position) > distance) return false;
    //     if (!(position - transform.position).IsInDirectionCone(transform.forward, viewAngle)) return false;
    //
    //     return true;
    // }

    #endregion

    #region Attack Methods
    
    public void SetIsAttacking(bool value)
    {
        if (!value && currentAction != null)
        {
            // Only record streak/history for committed actions (attacks, stun, idle).
            // Movement fill actions (ResetsActionTimer=false) must not overwrite _lastActionName
            // or the streak counter, otherwise Sweep's anti-spam gate misfires.
            if (currentAction.ResetsActionTimer)
            {
                string finishing = currentAction.name;
                _secondLastActionName = _lastActionName;
                _sameActionStreak = finishing == _lastActionName ? _sameActionStreak + 1 : 1;
                _lastActionName = finishing;
            }
            currentAction = null;
        }

        IsAttacking = value;
        PauseUtilityAITimer(value);
    }
    
    public void PauseUtilityAITimer(bool pause)
    {
        if (pause) ThinkTimer.Pause();
        else ThinkTimer.Resume();
    }

    public void PerformBestAction()
    {
        if (IsAttacking) return;
        if (sc.IsState<EnemyHit>() || sc.IsState<EnemyStagger>()) return;
        
        AIBrain.UpdateContext();
        AIBrain.CalculateBestAction();
    }

    // public EnemyAttackInfo CheckForAvailableAttack(PlayerController player)
    // {
    //     if (player == null) return null;
    //     
    //     if (sc.IsState<EnemyAttacking>()) return null;
    //     
    //     List<EnemyAttackInfo> availableAttacks = new();
    //
    //     foreach (var info in ts.attackConfig.infos)
    //     {
    //         if (!TargetInRange(player.transform.position, info.triggerInfo.distanceToTrigger,
    //                 info.triggerInfo.angleToTrigger)) continue;
    //         
    //         availableAttacks.Add(info);
    //     }
    //     
    //     if (availableAttacks.Count == 0) return null;
    //
    //     EnemyAttackInfo a = availableAttacks[0];
    //
    //     foreach (var info in availableAttacks)
    //     {
    //         if (info.triggerInfo.distanceToTrigger <= a.triggerInfo.distanceToTrigger) a = info;
    //     }
    //
    //     return a;
    // }

    // public void CheckToAttack()
    // {
    //     var a = CheckForAvailableAttack(ts.pc);
    //
    //     if (a != null)
    //     {
    //         sc.Interrupt(new EnemyAttacking(a));
    //     }
    // }

    #endregion
}

