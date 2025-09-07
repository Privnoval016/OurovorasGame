using System.Collections.Generic;
using System.Linq;
using Extensions.StateMachine;
using Extensions.Utils;
using UnityEngine;
using Pathfinding;
using VTabs.Libs;

[RequireComponent(typeof(StateController<EnemyState>))]
public class StandardEnemy : PhysicsEnemy
{
    #region State Machine
    [HideInInspector] public StateController<EnemyState> sc;
    #endregion
    
    #region Components
    
    [Header("Enemy Components")]
    
    public EnemyData enemyData;
    public EnemyAnimData enemyAnimData;
    
    public PhysicsNavigator nav;

    [HideInInspector] public EnemyAnimator ea;

    [HideInInspector] public PlayerController pc;

    public EnemyAttackConfig attackConfig;
    
    #endregion
    
    #region Movement Properties
    
    [Header("Movement")]
    
    [HideInInspector] public Vector3 moveDirection;
    
    public Transform[] wanderPoints;
    [HideInInspector] public Vector3 currentWanderPoint;
    [HideInInspector] public int wanderIndex = 0;
    
    public bool CanMove => physicsInteract;
    
    #endregion

    public override void OnStart() 
    {
        base.OnStart();
        
        sc = new StateController<EnemyState>(this);
        nav = GetComponent<PhysicsNavigator>();
        ea = GetComponent<EnemyAnimator>();
        
        sc.ChangeState(new EnemyIdle());
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        sc.PrintStates();
    }

    public override void OnHit(ElementEffect element, PlayerController pc, Attack a, Transform attackerTransform, int actionIndex = 0)
    {
        sc.Interrupt(new EnemyHit());
        base.OnHit(element, pc, a, attackerTransform, actionIndex);
    }
    
    #region Movement Methods
    
    public void MoveInDirection(Vector3 direction, float lerpAmount = 1)
    {
        if (CombatManager.Instance.entitiesStopped) return;
        
        if (!CanMove) return;
        
        moveDirection = direction;
        
        Vector3 targetSpeed = direction * enemyData.speed;
        targetSpeed = Vector3.Lerp(rb.linearVelocity, targetSpeed, lerpAmount);


        float accelRate = (Mathf.Abs(targetSpeed.magnitude) > 0.01f) ? enemyData.runAccelAmount : enemyData.runDecelAmount;
		
        Vector3 speedDiff = targetSpeed - rb.linearVelocity.ZeroVector3Axis();
		
        Vector3 movementForce = speedDiff * accelRate;
		
        rb.AddForce(movementForce, ForceMode.Acceleration);
        TurnToLook();
    }

    public void TurnToLook()
    {
        if (moveDirection.magnitude == 0) return;

        transform.rotation =
            EaseUtil.DampQuaternion(transform.rotation, Quaternion.LookRotation(moveDirection.ZeroVector3Axis()), 5f, 0.1f);
        
    }
    
    #endregion
    
    #region Check Methods

    public void CheckToFollowPlayer(float viewRadius, float viewAngle)
    {
        if (sc.GetCurrentState() is EnemyHit) return;
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, viewRadius);
        
        foreach (Collider c in colliders)
        {
            if ((c.transform.position - transform.position).IsInDirectionCone(transform.forward, viewAngle))
            {
                if (c.TryGetComponent(out PlayerController player))
                {
                    pc = player;
                    sc.ChangeState(new EnemyFollow());
                    return;
                }
            }
        }
    }

    public bool TargetInRange(Vector3 position, float distance, float viewAngle)
    {
        if (Vector3.Distance(position, transform.position) > distance) return false;
        if (!(position - transform.position).IsInDirectionCone(transform.forward, viewAngle)) return false;
        
        return true;
    }
    
    #endregion
    
    #region Attack Methods

    public EnemyAttack? CheckForAvailableAttack(PlayerController player)
    {
        if (player == null) return null;
        
        List<EnemyAttackInfo> availableAttacks = new();

        foreach (var info in attackConfig.infos)
        {
            if (!TargetInRange(player.transform.position, info.triggerInfo.distanceToTrigger,
                    info.triggerInfo.angleToTrigger)) continue;
            
            availableAttacks.Add(info);
        }
        
        if (availableAttacks.Count == 0) return null;

        EnemyAttackInfo a = availableAttacks[0];

        foreach (var info in availableAttacks)
        {
            if (info.triggerInfo.distanceToTrigger >= a.triggerInfo.distanceToTrigger) a = info;
        }

        return a.attack;
    }
    
    #endregion
}
