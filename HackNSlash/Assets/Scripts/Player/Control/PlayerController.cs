using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.StateMachine;
using Extensions.Utils;
using MEC;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(StateController<PlayerState>))]
public class PlayerController : KinematicBehaviour, IService
{
    #region State Machine
    [HideInInspector] public StateController<PlayerState> sc;
    #endregion
    
    #region Components
    
    public CapsuleCollider mainCol;
    [HideInInspector] public CapsuleCollider[] allCols;

    /** <summary>Physics body for separation and slope handling.</summary> */
    [HideInInspector] public CharacterPhysicsBody physicsBody;

    [HideInInspector] public CameraController cam;
    
    [HideInInspector] public WeaponController wc;
    [HideInInspector] public PlayerAnimator pac;
    [HideInInspector] public PlayerStateMachine psm;
    [HideInInspector] public PlayerInventory pi;
    [HideInInspector] public PlayerCombatControl pcc;
    [HideInInspector] public PlayerStats ps;
    [HideInInspector] public OnAttackEvents oae;
    [HideInInspector] public OnHitEvents ohe;
    [HideInInspector] public RuntimePlayerStatus rps;
    
    public PlayerSaveBinding saveBinding;
    
    public ElementalSpirit spirit;
    
    public Transform cameraFollowTarget;

    public PlayerAnimListener model;
    
    #endregion
    
    #region MonoBehaviour Callbacks

    private void Awake()
    {
        SetKinematicAttributes();

        wc = GetComponent<WeaponController>();
        pac = GetComponent<PlayerAnimator>();
        psm = GetComponent<PlayerStateMachine>();
        pi = GetComponent<PlayerInventory>();
        pcc = GetComponent<PlayerCombatControl>();
        physicsBody = GetComponent<CharacterPhysicsBody>();
        oae = GetComponent<OnAttackEvents>();
        ohe = GetComponent<OnHitEvents>();
        ps = GetComponent<PlayerStats>();
        rps = GetComponent<RuntimePlayerStatus>();
        
        saveBinding = GetComponent<PlayerSaveBinding>();

        allCols = GetComponents<CapsuleCollider>();
        
        if (mainCol == null)
        {
            foreach (CapsuleCollider c in allCols)
            {
                if (c.material != null) continue;
                
                mainCol = c;
                break;
            }
        }
        
        spirit.pc = this;
        
        sc = new StateController<PlayerState>(this);
        
        
        if (Camera.main != null)
            Camera.main.TryGetComponent(out cam);
        
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        model.pc = this;
    }
    
    private void Update()
    {
        UpdateKinematicAttributes();
    }

    private void LateUpdate()
    {
        AvoidColliderClipping();
    }

    #endregion
    
    #region Collision Methods
    
    public bool IgnoreCollision(Collider col, bool ignore)
    {
        if (col == null) return false;
        foreach (CapsuleCollider c in allCols)
        {
            Physics.IgnoreCollision(c, col, ignore);
        }
        return true;
    }
    
    public void AvoidColliderClipping()
    {
        // Body separation is handled by CharacterSeparationSystem.
        // Slope correction is handled by SlopeHandler (both self-tick in FixedUpdate).
    }
    
    public bool IgnoreAllCollisionsWithLayer(int? layer, bool ignore)
    {
        if (layer == null) return false;
        
        int layerValue = (int) layer;
        
        if (layerValue < 0 || layerValue > 31) return false;
        foreach (CapsuleCollider c in allCols)
        {
            Physics.IgnoreLayerCollision(c.gameObject.layer, layerValue, ignore);
        }
        return true;
    }
    
    #endregion
    
    #region Utility Methods
    
    public HashSet<LockOnTarget> HitScanEnemies(int numTargets, float radius, float height, float angle, Attack a = null)
    {
        HashSet<LockOnTarget> enemies = new();
        
        if (cam.IsLockedOn) enemies.Add(psm.NearestHEnemy);
        
        int enemiesNeeded = numTargets - enemies.Count;
        
        if (enemiesNeeded > 0)
        {
            var enemyList = psm.GetAllEnemiesInCapsule(radius, height, angle);
            if (enemyList != null && enemyList.Length > 0 && enemyList.Length >= enemiesNeeded)
                enemies = enemies.Union(enemyList[0..enemiesNeeded]).ToHashSet();
            else if (enemyList != null && enemyList.Length > 0)
                enemies = enemies.Union(enemyList).ToHashSet();
        }
        
        if (a != null) enemies.RemoveWhere(e => e.TookDamageThisAction(a));
        
        return enemies;
    }
    
    #endregion
    
}

