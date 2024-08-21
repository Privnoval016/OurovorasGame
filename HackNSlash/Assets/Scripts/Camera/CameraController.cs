using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public CameraController Instance { get; private set; }
    
    #region Inspector Settings

    [Header("Camera Movement Settings")] 
    [SerializeField] private float baseXSensitivity = 300;
    [SerializeField] private float baseYSensitivity = 2;
    [SerializeField, Range(0, 5)] private float xSensitivityMultiplier = 1;
    [SerializeField, Range(0, 5)] private float ySensitivityMultiplier = 1;
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;
    
    [Header("Target Settings")]
    [SerializeField] private CinemachineFreeLook playerUnlockCamera;
    [SerializeField] private GameObject playerLockCameraTemplate;
    [SerializeField] private GameObject player;
    [SerializeField] private Transform playerFollowTarget;

    private CinemachineFreeLook playerCamera;
    private CinemachineTargetGroup currentTargetGroup;
    private CinemachineBrain camBrain;
    
    private PlayerController playerController;

    private float destroyDelay = 0.2f;
    
    #endregion
    
    #region Accessible Properties

    public bool isLockedOn;
    public GameObject targetedEnemy;
    
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
        
        playerUnlockCamera.gameObject.SetActive(true);
        playerCamera = playerUnlockCamera;
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
            Debug.Log("Lock On Pressed");
            CheckForLockOn();
        }
        else
        {
            Debug.Log("Lock On Released");
            targetedEnemy = null;
            isLockedOn = false;
        }
    }
    
    private void OnRetargetAction(InputAction.CallbackContext context)
    {
        if (isLockedOn)
        {
            CheckForLockOn();
        }
    }
    
    #endregion
    
    #region Camera Methods
   
    private void SetMovementSettings()
    {
        playerCamera.m_XAxis.m_MaxSpeed = baseXSensitivity * xSensitivityMultiplier;
        playerCamera.m_YAxis.m_MaxSpeed = baseYSensitivity * ySensitivityMultiplier;
        playerCamera.m_XAxis.m_InvertInput = invertX;
        playerCamera.m_YAxis.m_InvertInput = invertY;
    }
    
    private void CheckForLockOn()
    {
        Collider[] collidersInRange =
            Physics.OverlapSphere(playerFollowTarget.position, playerController.playerData.lockOnRange);
        
        if (collidersInRange.Length == 0) return;
        
        print (collidersInRange.Length);

        GameObject nearestEnemy = null;
        
        foreach (var c in collidersInRange)
        {
            if (c.gameObject == targetedEnemy || !c.CompareTag("Enemy") || !IsOnScreen(c.transform.position))
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
            
            print (c.gameObject);
        }
        
        Debug.Log(nearestEnemy);
        
        if (nearestEnemy != null)
        {
            targetedEnemy = nearestEnemy;
            isLockedOn = true;
        }
    }
    
    private void ValidateLockedOnTarget()
    {
        if (isLockedOn && Vector3.Distance(playerFollowTarget.position, targetedEnemy.transform.position) >
            playerController.playerData.lockOnRange)
        {
            targetedEnemy = null;
        }
        
        isLockedOn = targetedEnemy != null;

        if (isLockedOn)
        {
            if (currentTargetGroup != null && currentTargetGroup.m_Targets[1].target == targetedEnemy.transform)
            {
                return;
            }
            
            CinemachineFreeLook oldCamera = playerCamera;
            Instantiate(playerLockCameraTemplate, transform).transform.GetChild(0).TryGetComponent(out playerCamera);
            playerCamera.transform.parent.GetChild(1).TryGetComponent(out currentTargetGroup);
            currentTargetGroup.m_Targets[1].target = targetedEnemy.transform;
            
            playerCamera.transform.parent.gameObject.SetActive(true);

            if (oldCamera != playerUnlockCamera)
            {
                oldCamera.transform.parent.gameObject.SetActive(false);
                StartCoroutine(DestroyCamera(oldCamera, destroyDelay));
            }
            else
            {
                playerUnlockCamera.gameObject.SetActive(false);
            }
        }
        else
        {
            CinemachineFreeLook oldCamera = playerCamera;
            playerCamera = playerUnlockCamera;
            playerCamera.gameObject.SetActive(true);
            
            if (oldCamera != playerUnlockCamera)
            {
                oldCamera.transform.parent.gameObject.SetActive(false);
                StartCoroutine(DestroyCamera(oldCamera, destroyDelay));
            }
        }

    }
    
    private IEnumerator DestroyCamera(CinemachineFreeLook toDestroy, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(toDestroy.transform.parent.gameObject);
    }
    
    private bool IsOnScreen(Vector3 position)
    {
        Vector3 screenPoint = camBrain.OutputCamera.WorldToViewportPoint(position);
        return screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
    }
    
    #endregion
}
