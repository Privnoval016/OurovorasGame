using System;
using Extensions.EventBus;
using Extensions.Pathfinding;
using Unity.Entities;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Components")]

    [HideInInspector] public PhysicsEnemy pe;

    /** <summary>Path planner — computes A* paths and provides steering direction.</summary> */
    [HideInInspector] public NavPathPlanner nav;

    /** <summary>Movement motor — executes all physical locomotion primitives.</summary> */
    [HideInInspector] public NavMotor motor;

    [HideInInspector] public EnemyAnimator ea;
    
    [HideInInspector] public EnemyStateMachine esm;

    [HideInInspector] public EnemyStats stats;

    [HideInInspector] public OnEnemyEvents onEnemyEvents;

    public EnemyAnimListener animListener;

    public EnemyHitbox[] attackHitboxes;
    

    #region Attack Properties
    
    public bool parryWindowActive = false;

    #endregion

    #region Monobehaviour Callbacks

    private void Awake()
    {
        nav = GetComponent<NavPathPlanner>();
        motor = GetComponent<NavMotor>();
        ea = GetComponent<EnemyAnimator>();
        pe = GetComponent<PhysicsEnemy>();
        onEnemyEvents = GetComponent<OnEnemyEvents>();
        esm = GetComponent<EnemyStateMachine>();
        stats = GetComponent<EnemyStats>();
    
        animListener.ts = this;
    
        foreach (var hitbox in attackHitboxes)
        {
            hitbox.ts = this;
        }
    }

    private void OnEnable()
    {
        EntityManager.Instance.RegisterEnemy(this);
    }
    
    private void OnDestroy()
    {
        EntityManager.Instance.UnregisterEnemy(this);
    }

    #endregion
}

