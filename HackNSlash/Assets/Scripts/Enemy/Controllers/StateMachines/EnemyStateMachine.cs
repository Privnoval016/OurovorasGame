using System;
using System.Collections.Generic;
using Extensions.StateMachine;
using Extensions.Timers;
using Extensions.UtilityAI;
using Extensions.Utils;
using UnityEngine;


[RequireComponent(typeof(StateController<EnemyState>))]
public class EnemyStateMachine : AIBrainUser<EnemyAIContextKey>
{
    #region AI Components

    [HideInInspector] public StateController<EnemyState> sc;
    public AIBrain<EnemyAIContextKey> AIBrain;

    [Header("Attack AI")]
    public List<AIAction<EnemyAIContextKey>> actions = new();

    public EnemySensor sensor;

    public TickTimer ThinkTimer;
    
    public EnemyAIActionBase currentAction;
    #endregion

    #region Inspector Components

    [Header("Enemy Components")]

    [HideInInspector] public EnemyController ts;

    public EnemyData enemyData;
    public EnemyAnimData enemyAnimData;

    #endregion

    #region Movement Properties

    [Header("Movement")]

    [HideInInspector] public Vector3 moveDirection;

    public Transform[] wanderPoints;
    [HideInInspector] public Vector3 currentWanderPoint;
    [HideInInspector] public int wanderIndex = 0;

    public bool CanMove => ts.pe.physicsInteract;

    #endregion

    #region Attack Properties

    public bool IsAttacking;

    #endregion

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        sc = new StateController<EnemyState>(this);
        ts = GetComponent<EnemyController>();
        
        AIBrain = new AIBrain<EnemyAIContextKey>(this);
        
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
        //sc.PrintStates();
    }

    #endregion

    #region AI Callbacks

    public override ContextPayload<EnemyAIContextKey>[] OnContextUpdate()
    {
        ContextPayload<EnemyAIContextKey>[] payloads =
        {
            (EnemyAIContextKey.SelfHealth, ts.stats.currentHealth / ts.stats.GetStat(InnateStat.MaxHealth)),
        };
    
        return payloads;
    }

    public override List<AIAction<EnemyAIContextKey>> GetActions() => actions;
    
    public override Sensor<EnemyAIContextKey> GetSensor() => sensor;

    public override void ExecuteNewAction(AIAction<EnemyAIContextKey> action, Context<EnemyAIContextKey> context, float highestUtility)
    {
        Debug.Log($"Enemy {ts.name} executing action {action.name} with utility {highestUtility}");
        
        if (action is EnemyAIActionBase enemyAction)
        {
            var actionClone = Instantiate(enemyAction); // Clone to avoid modifying the original ScriptableObject
            sc.ChangeState(new EnemyActing(actionClone, context));
        }
    }

    #endregion

    #region Inheritance Methods

    protected virtual void HitStateAction(ElementEffect element, PlayerController pc, Attack a, Transform attackerTransform, int actionIndex = 0)
    {
        Debug.Log("Enemy Hit by " + a.name);
    
        if (!ts.pe.knockbackImmune)
        {
            ts.animListener.DeactivateAllHitboxes();
            sc.ChangeState(new EnemyHit());
        }
    }

    protected virtual void StaggerStateAction(ElementEffect element, PlayerController pc, Attack a, Transform attackerTransform, int actionIndex = 0)
    {
        Debug.Log("Enemy Staggered by " + a.name);

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

    public void MoveInDirection(Vector3 direction, float lerpAmount = 1)
    {
        if (CombatManager.Instance.entitiesStopped) return;
    
        if (!CanMove) return;
    
        moveDirection = direction;
    
        Vector3 targetSpeed = direction * enemyData.speed;
        targetSpeed = Vector3.Lerp(ts.pe.rb.linearVelocity, targetSpeed, lerpAmount);


        float accelRate = (Mathf.Abs(targetSpeed.magnitude) > 0.01f) ? enemyData.runAccelAmount : enemyData.runDecelAmount;
	
        Vector3 speedDiff = targetSpeed - ts.pe.rb.linearVelocity.ZeroVector3Axis();
	
        Vector3 movementForce = speedDiff * accelRate;
	
        ts.pe. rb.AddForce(movementForce, ForceMode.Acceleration);
        TurnToLook();
    }

    public void TurnToLook()
    {
        if (moveDirection.magnitude == 0) return;

        transform.rotation =
            EaseUtil.DampQuaternion(transform.rotation, Quaternion.LookRotation(moveDirection.ZeroVector3Axis()), 5f, 0.1f);
    
    }

    public void TurnToPosition(Vector3 position)
    {
        Vector3 lookDirection = (position - transform.position).ZeroVector3Axis();
        if (lookDirection.magnitude == 0) return;

        transform.rotation =
            EaseUtil.DampQuaternion(transform.rotation, Quaternion.LookRotation(lookDirection), 5f, 0.1f);
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
        if (!value) currentAction = null;
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

