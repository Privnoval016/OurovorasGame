using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(StateController))]
public class PlayerController : MonoBehaviour
{
    #region State Machine
    [HideInInspector] public StateController stateController;
    #endregion
    
    #region Components
    
    [HideInInspector] public Rigidbody rb;
    public PlayerData playerData;
    
    #endregion
    
    #region STATE PARAMETERS

    public bool IsJumping;

    //Timers (also all fields, could be private and a method returning a bool could be used)
    public float lastOnGroundTime;

    //Jump
    public bool isJumpCut;
    public bool isJumpFalling;
    
    public bool CanJump => lastOnGroundTime > 0 && !IsJumping;

    #endregion
    
    #region INPUT PARAMETERS
    public Vector2 moveInput;

    public float lastPressedJumpTime;
    #endregion
    
    #region CHECK PARAMETERS
    [Header("Checks")] 
    [SerializeField] public Transform groundCheckPoint;
    [SerializeField] public Vector3 groundCheckSize = new Vector3(0.49f, 0.3f, 0.49f);
    #endregion

    #region LAYERS & TAGS

    [Header("Layers & Tags")] 
    [SerializeField] public LayerMask groundLayer;
    #endregion


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        stateController = GetComponent<StateController>();
        stateController.parent = this;
        
        stateController.AddNewState(new PlayerMoving());
    }
}