using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.EventBus;
using Extensions.Pathfinding;
using Unity.Entities;
using UnityEngine;

public class EnemyController : MonoBehaviour, IVFXSpawnLocationOwner
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
    
    public ElementColorChanger elementColorChanger;

    public EnemyAnimListener animListener;
    
    public DamageableComponentBase[] damageableComponents;

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
        elementColorChanger ??= GetComponent<ElementColorChanger>();
    
        if (animListener != null) 
            animListener.ts = this;
    
        foreach (var hitbox in attackHitboxes)
        {
            if ( hitbox != null)
                hitbox.ts = this;
        }
    }

    private void Start()
    {
        // doing this in start to ensure all components have had OnEnable called.
        foreach (var component in damageableComponents)
        {
            if (component?.isActiveAndEnabled != true) continue; // skip null or disabled components
            stats.DamageableComponents.AddComponent(component);
        }
        
        elementColorChanger.Initialize(() => stats.currentElementEffect);
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
    
    #region IVFXSpawnLocationOwner Implementation
    
    public List<IVFXSpawnLocation> GetVFXSpawnLocations()
    {
        return attackHitboxes.Cast<IVFXSpawnLocation>().ToList();
    }

    public Transform GetTransform()
    {
        return transform;
    }
    
    #endregion
}

