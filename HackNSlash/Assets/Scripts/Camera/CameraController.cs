using System;
using Extensions.Utils;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

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
    
    [SerializeField] private GameObject player; 
    
    [SerializeField] private Transform playerTargetTransform;
    [SerializeField] private Transform enemyTargetTransform;
    public bool isFollowingPlayer = true;
    private Vector3 lastPlayerPosition;
    
    [SerializeField] private CinemachineOrbitalFollow playerCamera;
    private CinemachineBrain camBrain;
    
    private PlayerController pc;

    private float lerpTimer;
    public float lerpTime = 0.2f;
    
    #endregion
    
    #region Accessible Properties

    public bool IsLockedOn => TargetedEnemy != null;
    [HideInInspector] public bool lockOnTriggered;
    [HideInInspector] public LockOnTarget TargetedEnemy;
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
        
        playerCamera.transform.parent.gameObject.SetActive(true);
        
        playerTargetTransform.position = pc.cameraFollowTarget.position;

        TargetedEnemy = null;

    }

    void Update()
    {
        UpdateTargets();
        ValidateLockedOnTarget();
    }

    private void LateUpdate()
    {
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
    
    private void UpdateTargets()
    {
        lastPlayerPosition = isFollowingPlayer ? pc.cameraFollowTarget.position : lastPlayerPosition;
        Vector3 enemyTarget = IsLockedOn ? TargetedEnemy.TargetedPosition() : playerTargetTransform.position;

        playerTargetTransform.position = lastPlayerPosition;
        enemyTargetTransform.position = Vector3.Lerp(enemyTargetTransform.position, enemyTarget, Time.deltaTime * 10);
    }
    
    private void CheckForLockOnTarget()
    {
        Collider[] collidersInRange =
            Physics.OverlapSphere(playerTargetTransform.position, pc.psm.playerData.lockOnRange);
        
        if (collidersInRange.Length == 0) return;
        
        LockOnTarget t = null;
        foreach (var c in collidersInRange)
        {
            if (!c.TryGetComponent(out LockOnTarget t1)) continue;
            if (TargetedEnemy == t1) continue;
            if (!IsOnScreen(t1.TargetedPosition(), 0.1f)) continue;
            
            if (t == null || Vector3.Distance(pc.transform.position, t1.TargetedPosition()) <
                     Vector3.Distance(pc.transform.position, t.TargetedPosition()))
            {
                t = t1;
            }
        }

        if (t != null)
        {
            lerpTimer = 0;
            TargetedEnemy = t;
        }
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
        
        if (Vector3.Distance(playerTargetTransform.position, TargetPosition) >
            pc.psm.playerData.lockOnRange)
        {
            TargetedEnemy = null;
        }
    }
    
    
    private bool IsOnScreen(Vector3 position, float edgeOffset)
    {
        float min = edgeOffset;
        float max = 1 - edgeOffset;
        Vector3 screenPoint = camBrain.OutputCamera.WorldToViewportPoint(position);
        return screenPoint.z > min && screenPoint.x > min && screenPoint.x < max && screenPoint.y > min && screenPoint.y < max;
    }
    
    private Vector2 GetViewportOutOfBounds(Vector3 position, float edgeOffset)
    {
        float min = edgeOffset;
        float max = 1 - edgeOffset;
        
        Vector3 screenPoint = camBrain.OutputCamera.WorldToViewportPoint(position);
        
        float x = Math.Max(0, Math.Max(min - screenPoint.x, screenPoint.x - max));
        float y = Math.Max(0, Math.Max(min - screenPoint.y, screenPoint.y - max));
        
        return new Vector2(x, y);
    }
    
    #endregion
}
