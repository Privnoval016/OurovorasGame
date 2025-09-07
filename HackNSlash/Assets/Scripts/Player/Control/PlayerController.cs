using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.StateMachine;
using Extensions.Utils;
using MEC;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(StateController<PlayerState>))]
public class PlayerController : KinematicBehaviour
{
    #region State Machine
    [HideInInspector] public StateController<PlayerState> sc;
    #endregion
    
    #region Components
    
    public CapsuleCollider mainCol;
    [HideInInspector] public CapsuleCollider[] allCols;
    [HideInInspector] public CollisionListener cl;
    
    [HideInInspector] public CameraController cam;
    
    [HideInInspector] public WeaponController wc;
    [HideInInspector] public PlayerAnimator pac;
    [HideInInspector] public PlayerStateMachine psm;
    [HideInInspector] public PlayerInventory pi;
    
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
        cl = GetComponentInChildren<CollisionListener>();

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
        if (cl == null || !cl.activeMover) return;
        if (!psm.canAttack && !psm.pauseMovement) return;
        if (rb.linearVelocity.y > 0) return; //Avoid applying pushback when jumping
        
        Vector3 direction = cl.GetCombinedDirection();
        
        if (direction != Vector3.zero)
        {
            //rb.AddForce(direction * psm.playerData.pushbackForce, ForceMode.VelocityChange);
        }
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

