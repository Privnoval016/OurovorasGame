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
    private CinemachineInputAxisController playerInputAxisController;
    private CinemachineTargetGroup currentTargetGroup;
    private CinemachineBrain camBrain;
    
    private PlayerController playerController;

    private float destroyDelay = 0.2f;

    private float targetRadiusMultiplier = 1f;
    
    #endregion
    
    #region Accessible Properties

    public bool isLockedOn, lockOnTriggered;
    public GameObject targetedEnemy; // The enemy the player is currently locked on to, or the player if not locked on
    public Vector3 LockOnDirection => (targetedEnemy.transform.position - playerFollowTarget.position).ZeroVector3Axis().normalized;
    
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
        
        player.TryGetComponent(out playerController);
        TryGetComponent(out camBrain);
        
        InputManager.Instance.lockOn.performed += OnLockOnAction;
        InputManager.Instance.lockOn.canceled += OnLockOnAction;
        InputManager.Instance.retarget.performed += OnRetargetAction;
        
        defaultCamera.SetActive(false);
        
        Instantiate(defaultCamera, cameraContainer).transform.GetChild(0).TryGetComponent(out playerCamera);
        defaultCamera.transform.GetChild(0).TryGetComponent(out playerInputAxisController);
        playerCamera.transform.parent.GetChild(1).TryGetComponent(out currentTargetGroup);
        
        
        playerCamera.transform.parent.gameObject.SetActive(true);
        
        targetedEnemy = playerFollowTarget.gameObject;
        
    }

    void Update()
    {
        SetMovementSettings();
        ValidateLockedOnTarget();
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
            targetedEnemy = playerFollowTarget.gameObject;
            isLockedOn = false;
            lockOnTriggered = false;
        }
    }
    
    private void OnRetargetAction(InputAction.CallbackContext context)
    {
        if (isLockedOn)
        {
            CheckForLockOnTarget();
        }
    }
    
    #endregion
    
    #region Camera Methods
   
    private void SetMovementSettings()
    {

        float radiusIncrease = Math.Max(GetViewportOutOfBounds(targetedEnemy.transform.position),
            GetViewportOutOfBounds(playerFollowTarget.position));

        targetRadiusMultiplier = 1 + radiusIncrease + 0.1f;

        playerCamera.RadialAxis.Value = targetRadiusMultiplier;
    }
    
    private void CheckForLockOnTarget()
    {
        Collider[] collidersInRange =
            Physics.OverlapSphere(playerFollowTarget.position, playerController.playerData.lockOnRange);
        
        if (collidersInRange.Length == 0) return;
        
        GameObject nearestEnemy = null;
        
        foreach (var c in collidersInRange)
        {
            if (c.gameObject == targetedEnemy || !c.TryGetComponent(out ITargetable t) || !IsOnScreen(c.transform.position))
            {
                continue;
            }
            
            if (nearestEnemy == null)
            {
                nearestEnemy = c.gameObject;
            }
            else if (Vector3.Distance(playerFollowTarget.position, c.transform.position) <
                     Vector3.Distance(playerFollowTarget.position, nearestEnemy.transform.position))
            {
                nearestEnemy = c.gameObject;
            }
        }
        
        if (nearestEnemy != null)
        {
            targetedEnemy = nearestEnemy;
            isLockedOn = true;
        }
    }
    
    private void ValidateLockedOnTarget()
    {
        if (lockOnTriggered && !isLockedOn)
        {
            CheckForLockOnTarget();
        }
        
        if (Vector3.Distance(playerFollowTarget.position, targetedEnemy.transform.position) >
            playerController.playerData.lockOnRange)
        {
            targetedEnemy = playerFollowTarget.gameObject;
        }
        
        isLockedOn = targetedEnemy != playerFollowTarget.gameObject;
        

        if (currentTargetGroup.Targets[1].Object == targetedEnemy.transform)
        {
            return;
        }
        
        
        CinemachineOrbitalFollow oldCamera = playerCamera;
        Instantiate(defaultCamera, cameraContainer).transform.GetChild(0).TryGetComponent(out playerCamera);
        defaultCamera.transform.GetChild(0).TryGetComponent(out playerInputAxisController);
        
        playerCamera.transform.parent.GetChild(1).TryGetComponent(out currentTargetGroup);
        currentTargetGroup.Targets[1].Object = targetedEnemy.transform;
        
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
