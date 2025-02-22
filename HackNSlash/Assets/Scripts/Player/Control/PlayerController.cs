using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(StateController))]
public class PlayerController : MonoBehaviour
{
    #region State Machine
    [HideInInspector] public StateController sc;
    #endregion
    
    #region Components
    
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public CapsuleCollider col;
    
    [HideInInspector] public CameraController cam;
    
    [HideInInspector] public WeaponController wc;
    [HideInInspector] public PlayerAnimator pac;
    [HideInInspector] public PlayerStateMachine psm;

    public PlayerAnimListener model;
    
    public float playerRadius = 3f;
    
    #endregion
    
    
    #region MonoBehaviour Callbacks

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        wc = GetComponent<WeaponController>();
        pac = GetComponent<PlayerAnimator>();
        psm = GetComponent<PlayerStateMachine>();
        
        sc = GetComponent<StateController>();
        sc.parent = this;
        
        if (Camera.main != null)
            Camera.main.TryGetComponent(out cam);
        
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        model.pc = this;
    }
    
    
    #endregion
    
}