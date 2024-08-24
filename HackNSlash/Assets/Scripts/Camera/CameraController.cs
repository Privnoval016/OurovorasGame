using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    public CameraController Instance { get; private set; }
    
    #region Inspector Settings

    [Header("Camera Movement Settings")] 
    [SerializeField] private float maxZoomFOV = 70;
    [SerializeField] private float minZoomFOV = 40;
    [SerializeField] private float orbitRadiusChangeMultiplier = 1;
    [SerializeField] private float baseXSensitivity = 300;
    [SerializeField] private float baseYSensitivity = 2;
    [SerializeField, Range(0, 5)] private float xSensitivityMultiplier = 1;
    [SerializeField, Range(0, 5)] private float ySensitivityMultiplier = 1;
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;
    
    [Header("Target Settings")]
    [SerializeField] private GameObject defaultCamera;
    [SerializeField] private GameObject player;
    [SerializeField] private Transform playerFollowTarget;

    private CinemachineFreeLook playerCamera;
    private CinemachineTargetGroup currentTargetGroup;
    private CinemachineBrain camBrain;
    
    private PlayerController playerController;

    private float[] originalOrbitRadii;

    private float destroyDelay = 0.2f;

    private float targetRadiusMultiplier = 1f;
    
    #endregion
    
    #region Accessible Properties

    public bool isLockedOn, lockOnTriggered;
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
        
        defaultCamera.SetActive(false);
        
        Instantiate(defaultCamera, transform).transform.GetChild(0).TryGetComponent(out playerCamera);
        playerCamera.transform.parent.GetChild(1).TryGetComponent(out currentTargetGroup);
        
        playerCamera.transform.parent.gameObject.SetActive(true);
        
        targetedEnemy = playerFollowTarget.gameObject;
        
        originalOrbitRadii = new float[playerCamera.m_Orbits.Length];
        for (int i = 0; i < playerCamera.m_Orbits.Length; i++)
        {
            originalOrbitRadii[i] = playerCamera.m_Orbits[i].m_Radius;
        }
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
        playerCamera.m_XAxis.m_MaxSpeed = baseXSensitivity * xSensitivityMultiplier;
        playerCamera.m_YAxis.m_MaxSpeed = baseYSensitivity * ySensitivityMultiplier;
        playerCamera.m_XAxis.m_InvertInput = invertX;
        playerCamera.m_YAxis.m_InvertInput = invertY;

        float radiusIncrease = Math.Max(GetViewportOutOfBounds(targetedEnemy.transform.position),
            GetViewportOutOfBounds(playerFollowTarget.position));

        targetRadiusMultiplier = 1 + radiusIncrease;
        
        print (playerCamera.m_Orbits[0].m_Radius + " " + playerCamera.m_Orbits[1].m_Radius + " " + playerCamera.m_Orbits[2].m_Radius);
        
        for (int i = 0; i < playerCamera.m_Orbits.Length; i++)
        {
            //playerCamera.m_Orbits[i].m_Radius = Mathf.Lerp(playerCamera.m_Orbits[i].m_Radius, 
                //originalOrbitRadii[i] * targetRadiusMultiplier, Time.deltaTime * orbitRadiusChangeMultiplier);
                
            playerCamera.m_Orbits[i].m_Radius = originalOrbitRadii[i] * targetRadiusMultiplier;
            playerCamera.m_Orbits[i].m_Radius = Mathf.Clamp(playerCamera.m_Orbits[i].m_Radius, originalOrbitRadii[i], 100);
        }
    }
    
    private void CheckForLockOnTarget()
    {
        Collider[] collidersInRange =
            Physics.OverlapSphere(playerFollowTarget.position, playerController.playerData.lockOnRange);
        
        if (collidersInRange.Length == 0) return;
        
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


        if (currentTargetGroup.m_Targets[1].target == targetedEnemy.transform)
        {
            return;
        }
        
        CinemachineFreeLook oldCamera = playerCamera;
        Instantiate(defaultCamera, transform).transform.GetChild(0).TryGetComponent(out playerCamera);
        playerCamera.transform.parent.GetChild(1).TryGetComponent(out currentTargetGroup);
        currentTargetGroup.m_Targets[1].target = targetedEnemy.transform;
        
        playerCamera.transform.parent.gameObject.SetActive(true);
        
        StartCoroutine(RemoveCamera(oldCamera, destroyDelay));


    }
    
    private IEnumerator RemoveCamera(CinemachineFreeLook toDestroy, float delay)
    {
        toDestroy.transform.parent.gameObject.SetActive(false);
        yield return new WaitForSeconds(delay);
        Destroy(toDestroy.transform.parent.gameObject);
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
