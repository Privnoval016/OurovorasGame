using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.StateMachine;
using Extensions.Utils;
using MEC;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(StateController<PlayerState>))]
public class PlayerController : KinematicBehaviour
{
    #region State Machine
    [HideInInspector] public StateController<PlayerState> sc;
    #endregion
    
    #region Components
    
    [HideInInspector] public Rigidbody rb;
    public CapsuleCollider mainCol;
    [HideInInspector] public CapsuleCollider[] allCols;
    
    [HideInInspector] public CameraController cam;
    
    [HideInInspector] public WeaponController wc;
    [HideInInspector] public PlayerAnimator pac;
    [HideInInspector] public PlayerStateMachine psm;
    
    public ElementalSpirit spirit;
    
    public Transform cameraFollowTarget;

    public PlayerAnimListener model;
    
    #endregion
    
    public ElementEffect CurrentElementEffect = ElementEffect.None;
    public ElementData CurrentElementData => GameManager.ElementMap[CurrentElementEffect]();
    
    public ElementEffect[] elementEffects = Array.Empty<ElementEffect>();
    public CircularList<ElementEffect> elementEffectOrder;
    
    
    #region MonoBehaviour Callbacks

    private void Awake()
    {
        SetKinematicAttributes();
        
        rb = GetComponent<Rigidbody>();
        wc = GetComponent<WeaponController>();
        pac = GetComponent<PlayerAnimator>();
        psm = GetComponent<PlayerStateMachine>();

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

        elementEffectOrder = new CircularList<ElementEffect>(elementEffects.ToList());
        
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
    
    
    #endregion
    
    public bool IgnoreCollision(Collider col, bool ignore)
    {
        if (col == null) return false;
        foreach (CapsuleCollider c in allCols)
        {
            Physics.IgnoreCollision(c, col, ignore);
        }
        return true;
    }
    
}