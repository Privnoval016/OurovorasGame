using System;
using System.Collections;
using System.Collections.Generic;
using ExtensionUtils;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    public CameraController Instance { get; private set; }
    
    #region Inspector Settings

    [Header("Camera Movement Settings")] 
    
    [SerializeField] private float orbitRadiusChangeMultiplier = 1;
    
    [SerializeField] private float baseXSensitivity = 300;
    [SerializeField] private float baseYSensitivity = 2;
    
    [SerializeField, Range(0, 5)] private float xSensitivityMultiplier = 1;
    [SerializeField, Range(0, 5)] private float ySensitivityMultiplier = 1;
    
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;
    
    [Header("Target Settings")]
    
    [SerializeField] private Transform cameraContainer;
    [SerializeField] private GameObject defaultCamera;
    [SerializeField] private GameObject player;
    [SerializeField] private Transform playerFollowTarget;
    
    private CinemachineOrbitalFollow playerCamera;
    private CinemachineTargetGroup currentTargetGroup;
    private CinemachineBrain camBrain;
    
    private PlayerController pc;

    private float destroyDelay = 0.2f;

    private float targetRadiusMultiplier = 1f;
    
    #endregion
    
    #region Accessible Properties

    public bool IsLockedOn => TargetedEnemy != null;
    public bool lockOnTriggered;
    public LockOnTarget TargetedEnemy;
    public Vector3 LockOnDirection => (TargetedEnemy.TargetedPosition() - pc.transform.position).ZeroVector3Axis().normalized;
    
    public Vector3 TargetPosition => IsLockedOn ? TargetedEnemy.TargetedPosition() : pc.transform.position;
    
    #endregion

    #region MonoBehaviour Callbacks
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        player.TryGetComponent(out pc);
        TryGetComponent(out camBrain);
        
        InputManager.Instance.lockOn.performed += OnLockOnAction;
        InputManager.Instance.lockOn.canceled += OnLockOnAction;
        InputManager.Instance.retarget.performed += OnRetargetAction;
        
        defaultCamera.SetActive(false);
        
        Instantiate(defaultCamera, cameraContainer).transform.GetChild(0).TryGetComponent(out playerCamera);
        playerCamera.transform.parent.GetChild(1).TryGetComponent(out currentTargetGroup);
        
        
        playerCamera.transform.parent.gameObject.SetActive(true);

        TargetedEnemy = null;

    }

    void Update()
    {
        SetMovementSettings();
        ValidateLockedOnTarget();
        
        Debug.Log(TargetedEnemy);
    }
    
    #endregion
    
    #region Input Callbacks
    
    private void OnLockOnAction(InputAction.CallbackContext context)
    {
        if (!context.canceled)
        {
            lockOnTriggered = true;
        }
        else
        {
            TargetedEnemy = null;
            lockOnTriggered = false;
        }
    }
    
    private void OnRetargetAction(InputAction.CallbackContext context)
    {
        if (IsLockedOn)
        {
            CheckForLockOnTarget();
        }
    }
    
    #endregion
    
    #region Camera Methods
   
    private void SetMovementSettings()
    {

        float radiusIncrease = Math.Max(GetViewportOutOfBounds(IsLockedOn ? TargetPosition : playerFollowTarget.position),
            GetViewportOutOfBounds(playerFollowTarget.position));

        targetRadiusMultiplier = 1 + radiusIncrease + 0.1f;

        playerCamera.RadialAxis.Value = targetRadiusMultiplier;
    }
    
    private void CheckForLockOnTarget()
    {
        Collider[] collidersInRange =
            Physics.OverlapSphere(playerFollowTarget.position, pc.psm.playerData.lockOnRange);
        
        if (collidersInRange.Length == 0) return;
        
        LockOnTarget t = null;
        foreach (var c in collidersInRange)
        {
            if (!c.TryGetComponent(out LockOnTarget t1)) continue;
            Debug.Log("1");
            if (TargetedEnemy == t1) continue;
            Debug.Log("2");
            if (!IsOnScreen(t1.TargetedPosition())) continue;
            Debug.Log("3");
            
            if (!IsLockedOn || (t != null && Vector3.Distance(pc.transform.position, t1.TargetedPosition()) <
                     Vector3.Distance(pc.transform.position, t.TargetedPosition())))
            {
                t = t1;
            }
        }
        
        if (t != null) TargetedEnemy = t;
    }
    
    private void ValidateLockedOnTarget()
    {
        if (lockOnTriggered && !IsLockedOn)
        {
            CheckForLockOnTarget();
        }

        if (!lockOnTriggered)
        {
            TargetedEnemy = null;
        }
        
        if (Vector3.Distance(playerFollowTarget.position, TargetPosition) >
            pc.psm.playerData.lockOnRange)
        {
            TargetedEnemy = null;
        }

        if (TargetedEnemy != null && currentTargetGroup.Targets[1].Object == TargetedEnemy.transform)
        {
            return;
        }

        if (TargetedEnemy == null && currentTargetGroup.Targets[1].Object == playerFollowTarget.transform)
        {
            return;
        }
        
        
        CinemachineOrbitalFollow oldCamera = playerCamera;
        Instantiate(defaultCamera, cameraContainer).transform.GetChild(0).TryGetComponent(out playerCamera);
        
        playerCamera.transform.parent.GetChild(1).TryGetComponent(out currentTargetGroup);
        Transform targetTransform = TargetedEnemy != null ? TargetedEnemy.transform : playerFollowTarget;
        currentTargetGroup.Targets[1].Object = targetTransform;
        
        playerCamera.transform.parent.gameObject.SetActive(true);

        Destroy(oldCamera.transform.parent.gameObject, destroyDelay);

    }
    
    
    private bool IsOnScreen(Vector3 position)
    {
        Vector3 screenPoint = camBrain.OutputCamera.WorldToViewportPoint(position);
        return screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
    }
    
    private float GetViewportOutOfBounds(Vector3 position)
    {
        Vector3 screenPoint = camBrain.OutputCamera.WorldToViewportPoint(position);
        
        float x1 = screenPoint.x < 0 ? -screenPoint.x : 0;
        float x2 = screenPoint.x > 1 ? screenPoint.x - 1 : 0;
        float y1 = screenPoint.y < 0 ? -screenPoint.y : 0;
        float y2 = screenPoint.y > 1 ? screenPoint.y - 1 : 0;
        
        return Mathf.Max(x1, x2, y1, y2);
    }
    
    #endregion
}
