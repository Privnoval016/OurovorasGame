using System;
using System.Collections.Generic;
using Extensions.EventBus;
using Extensions.Utils;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    public CameraController Instance { get; private set; }
    
    #region Inspector Settings
    
    [Header("Target Settings")]
    
    [SerializeField] private GameObject player; 
    
    [SerializeField] private Transform playerTargetTransform;
    [SerializeField] private Transform enemyTargetTransform;
    [SerializeField] private Transform finisherTargetTransform;
    public bool isFollowingPlayer = true;
    private Vector3 lastPlayerPosition;

    [SerializeField] private GameObject cameraContainer;
    [SerializeField] private CineCamInfo[] cameraList;
    private Dictionary<PlayerCamStates, CinemachineCamera> cineCams;
    private List<CinemachineInputAxisController> inputAxisControllers;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    private CinemachineBrain camBrain;
    
    private PlayerController pc;
    
    #endregion
    
    #region Accessible Properties

    public bool IsLockedOn => TargetedEnemy != null;
    [HideInInspector] public bool lockOnTriggered;
    [HideInInspector] public LockOnTarget TargetedEnemy;
    [HideInInspector] public LockOnTarget FinisherTarget;
    public Vector3 LockOnDirection => (TargetedEnemy.TargetedPosition() - pc.transform.position).ZeroVector3Axis().normalized;
    
    public Vector3 TargetPosition => IsLockedOn ? TargetedEnemy.TargetedPosition() : pc.transform.position;

    [HideInInspector] public PlayerCamStates currentPlayerCamState;
    public CinemachineCamera CurrentCamera => cineCams[currentPlayerCamState];
    
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

        InputManager.Instance.onLockOn += OnLockOnAction;
        InputManager.Instance.onRetarget += OnRetargetAction;
        
        InitializeCamData();
        
        playerTargetTransform.position = pc.cameraFollowTarget.position;
        
        SetLockOnTarget(null);

        
    }

    void Update()
    {
        PerformStateActions();
    }

    private void LateUpdate()
    {
    }

    #endregion
    
    #region State Methods
    
    private void InitializeCamData()
    {
        cineCams = new Dictionary<PlayerCamStates, CinemachineCamera>();
        
        foreach (CineCamInfo camInfo in cameraList)
        {
            cineCams.Add(camInfo.playerCamState, camInfo.cam);
        }

        if (cineCams.Count == 0)
        {
            Debug.LogError("No cameras found in CameraController!");
        }
        
        cameraContainer.SetActive(true);
        
        inputAxisControllers = new List<CinemachineInputAxisController>();
        foreach (var cam in cineCams.Values)
        {
            if (cam.TryGetComponent(out CinemachineInputAxisController inputAxisController))
            {
                inputAxisControllers.Add(inputAxisController);
            }
        }
        
        currentPlayerCamState = PlayerCamStates.FinisherCloseUp;
        Debug.Log(SwitchState(PlayerCamStates.Free));
    }
    
    private void PerformStateActions()
    {
        switch (currentPlayerCamState)
        {
            case PlayerCamStates.Free:
                if (IsLockedOn) SwitchState(PlayerCamStates.LockedOn);
                UpdateLockOnTargets();
                ValidateLockedOnTarget();
                break;
                
            case PlayerCamStates.LockedOn:
                if (!IsLockedOn) SwitchState(PlayerCamStates.Free);
                UpdateLockOnTargets();
                ValidateLockedOnTarget();
                break;
                
            case PlayerCamStates.FinisherCloseUp:
                UpdateFinisherTargets();
                break;
                
            case PlayerCamStates.OverworldFocus:
                break;
        }
    }
    
    public bool SwitchState(PlayerCamStates state)
    {
        if (currentPlayerCamState == state) return false;
        
        if (!cineCams.TryGetValue(state, out CinemachineCamera cam)) return false;
        
        currentPlayerCamState = state;
        
        cam.gameObject.SetActive(true);
       
        foreach (var c in cineCams.Values)
        {
            if (c == cam) continue;
            c.gameObject.SetActive(false);
        }
        
        

        return true;
    }
    
    public CinemachineCamera GetCamera(PlayerCamStates state)
    {
        if (cineCams.TryGetValue(state, out CinemachineCamera cam))
        {
            return cam;
        }
        
        Debug.LogError($"Camera for state {state} not found!");
        return null;
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
            SetLockOnTarget(null);
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
    
    private void SetLockOnTarget(LockOnTarget target)
    {
        TargetedEnemy = target;
        EventBus<CameraLockOnEvent>.Raise(new CameraLockOnEvent
        {
            IsLockedOn = IsLockedOn,
            Target = TargetedEnemy
        });
    }
    
    private void UpdateLockOnTargets()
    {
        lastPlayerPosition = isFollowingPlayer ? pc.cameraFollowTarget.position : lastPlayerPosition;
        Vector3 enemyTarget = IsLockedOn ? TargetedEnemy.TargetedPosition() : playerTargetTransform.position;

        playerTargetTransform.position = lastPlayerPosition;
        enemyTargetTransform.position = EaseUtil.DampVector3(enemyTargetTransform.position, enemyTarget, 5, 0.1f);
    }

    private void UpdateFinisherTargets()
    {
        lastPlayerPosition = isFollowingPlayer ? pc.cameraFollowTarget.position : lastPlayerPosition;
        
        playerTargetTransform.position = lastPlayerPosition;
        
        finisherTargetTransform.position = FinisherTarget != null
            ? FinisherTarget.TargetedPosition()
            : playerTargetTransform.position;
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
            SetLockOnTarget(t);
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
            SetLockOnTarget(null);
        }
        
        if (Vector3.Distance(playerTargetTransform.position, TargetPosition) >
            pc.psm.playerData.lockOnRange)
        {
            SetLockOnTarget(null);
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
    
    #region Effect Methods
    
    
    public void ShakeCamera(float duration, float magnitude)
    {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulseWithForce(magnitude);
    }

    public void EnableCameraInputDetection(bool on)
    {
        foreach (var inputAxisController in inputAxisControllers)
        {
            inputAxisController.enabled = on;
        }
    }
    
    #endregion

    [Serializable]
    public class CineCamInfo
    {
        [FormerlySerializedAs("cameraState")] public PlayerCamStates playerCamState;
        public CinemachineCamera cam;
    }
}

public enum PlayerCamStates
{
    Free,
    LockedOn,
    FinisherCloseUp,
    OverworldFocus
}
