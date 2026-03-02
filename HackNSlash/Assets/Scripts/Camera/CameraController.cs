using System;
using System.Collections.Generic;
using Extensions.EventBus;
using Extensions.Utils;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/** <summary>
 * Manages all in-game cameras via a priority-based state machine backed by Cinemachine.
 *
 * Responsibilities:
 * <list type="bullet">
 *   <item>Lock-on target tracking and validation.</item>
 *   <item>Finisher camera with player invincibility guard.</item>
 *   <item>Overworld / cutscene focus mode.</item>
 *   <item>Camera shake via CinemachineImpulseSource.</item>
 * </list>
 *
 * States are prioritised: a lower-priority request will not override a higher-priority
 * active state unless <paramref name="force"/> is true.
 * </summary>
 */
public class CameraController : MonoBehaviour
{
    #region Inspector Settings

    [Header("Target Settings")]
    [SerializeField] private GameObject player;
    [SerializeField] private Transform playerTargetTransform;
    [SerializeField] private Transform enemyTargetTransform;
    [SerializeField] private Transform finisherTargetTransform;

    [SerializeField] private GameObject cameraContainer;
    [SerializeField] private CineCamInfo[] cameraList;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Lock-On")]
    [Tooltip("Damping factor for the enemy target transform interpolation.")]
    [SerializeField] private float lockOnTargetDamp = 5f;
    [Tooltip("Damping delta-time parameter for lock-on interpolation.")]
    [SerializeField] private float lockOnTargetDampDt = 0.1f;

    #endregion

    #region Public Properties

    public bool IsLockedOn => TargetedEnemy != null;
    [HideInInspector] public bool lockOnTriggered;
    [HideInInspector] public LockOnTarget TargetedEnemy;
    [HideInInspector] public LockOnTarget FinisherTarget;
    public bool isFollowingPlayer = true;

    public Vector3 LockOnDirection =>
        (TargetedEnemy.TargetedPosition() - pc.transform.position).ZeroVector3Axis().normalized;

    public Vector3 TargetPosition =>
        IsLockedOn ? TargetedEnemy.TargetedPosition() : pc.transform.position;

    public PlayerCamStates CurrentState => _currentState;
    public CinemachineCamera CurrentCamera => _cineCams[_currentState];
    [HideInInspector] public CinemachineBrain camBrain;

    #endregion

    #region Private State

    private Dictionary<PlayerCamStates, CinemachineCamera> _cineCams;
    private List<CinemachineInputAxisController> _inputAxisControllers;
    private PlayerController pc;
    private Vector3 _lastPlayerPosition;
    private PlayerCamStates _currentState;

    // Tracks whether the player was made invincible by the finisher so we
    // can safely restore it even if the finisher is cancelled.
    private bool _finisherInvincibilityActive;

    #endregion

    #region State Priority

    /** <summary>
     * Returns the priority of a camera state.
     * Higher priority states cannot be overridden by lower ones unless forced.
     * </summary>
     */
    private static int GetPriority(PlayerCamStates state) => state switch
    {
        PlayerCamStates.Free           => 0,
        PlayerCamStates.LockedOn       => 1,
        PlayerCamStates.OverworldFocus => 2,
        PlayerCamStates.FinisherCloseUp => 3,
        _ => 0
    };

    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        player.TryGetComponent(out pc);
        TryGetComponent(out camBrain);

        InputManager.Instance.onLockOn += OnLockOnAction;
        InputManager.Instance.onRetarget += OnRetargetAction;

        InitializeCamData();

        _lastPlayerPosition = pc.cameraFollowTarget.position;
        playerTargetTransform.position = _lastPlayerPosition;

        SetLockOnTarget(null);
    }

    private void Update()
    {
        PerformStateActions();
    }

    #endregion

    #region State Machine

    private void InitializeCamData()
    {
        _cineCams = new Dictionary<PlayerCamStates, CinemachineCamera>();
        foreach (var info in cameraList)
            _cineCams.Add(info.playerCamState, info.cam);

        if (_cineCams.Count == 0)
            Debug.LogError("CameraController: no cameras assigned in cameraList!");

        cameraContainer.SetActive(true);

        _inputAxisControllers = new List<CinemachineInputAxisController>();
        foreach (var cam in _cineCams.Values)
        {
            if (cam.TryGetComponent(out CinemachineInputAxisController c))
                _inputAxisControllers.Add(c);
        }

        // Activate initial state without going through priority guard.
        _currentState = PlayerCamStates.Free;
        ActivateCameraObject(PlayerCamStates.Free);
    }

    private void PerformStateActions()
    {
        switch (_currentState)
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

    /** <summary>
     * Requests a camera state transition.
     * Will not override a higher-priority state unless <paramref name="force"/> is true.
     * </summary>
     * <returns>True if the transition occurred.</returns>
     */
    public bool SwitchState(PlayerCamStates state, bool force = false)
    {
        if (_currentState == state) return false;
        if (!force && GetPriority(state) < GetPriority(_currentState)) return false;
        if (!_cineCams.ContainsKey(state)) return false;

        OnStateExit(_currentState);
        _currentState = state;
        ActivateCameraObject(state);
        OnStateEnter(state);
        return true;
    }

    /** <summary>
     * Forces an exit from the current state back to combat-appropriate defaults,
     * restoring any side effects (e.g. player invincibility from the finisher state).
     * </summary>
     */
    public void ForceExitToDefault()
    {
        OnStateExit(_currentState);
        PlayerCamStates target = IsLockedOn ? PlayerCamStates.LockedOn : PlayerCamStates.Free;
        _currentState = target;
        ActivateCameraObject(target);
    }

    private void ActivateCameraObject(PlayerCamStates state)
    {
        foreach (var kvp in _cineCams)
            kvp.Value.gameObject.SetActive(kvp.Key == state);
    }

    // Called when leaving a state — restore any side effects that state applied.
    private void OnStateExit(PlayerCamStates state)
    {
        if (state == PlayerCamStates.FinisherCloseUp)
            ClearFinisherInvincibility();
    }

    // Called when entering a state — apply state-specific setup.
    private void OnStateEnter(PlayerCamStates state)
    {
        if (state == PlayerCamStates.FinisherCloseUp)
            ApplyFinisherInvincibility();
    }

    public CinemachineCamera GetCamera(PlayerCamStates state)
    {
        if (_cineCams.TryGetValue(state, out var cam)) return cam;
        Debug.LogError($"CameraController: no camera for state {state}");
        return null;
    }

    #endregion

    #region Finisher Invincibility

    /** <summary>
     * Grants the player invincibility for the duration of the finisher camera.
     * Called automatically when the <see cref="PlayerCamStates.FinisherCloseUp"/> state is entered.
     * </summary>
     */
    private void ApplyFinisherInvincibility()
    {
        if (_finisherInvincibilityActive) return;
        _finisherInvincibilityActive = true;
        // Prevent any incoming hits from interrupting the finisher animation.
        if (pc != null) pc.IgnoreAllCollisionsWithLayer(GameManager.Instance.enemyLayer, true);
    }

    /** <summary>
     * Restores normal collision after the finisher is complete or cancelled.
     * </summary>
     */
    private void ClearFinisherInvincibility()
    {
        if (!_finisherInvincibilityActive) return;
        _finisherInvincibilityActive = false;
        if (pc != null) pc.IgnoreAllCollisionsWithLayer(GameManager.Instance.enemyLayer, false);
        FinisherTarget = null;
    }

    #endregion

    #region Input Callbacks

    private void OnLockOnAction(InputAction.CallbackContext context)
    {
        if (!context.canceled)
            lockOnTriggered = true;
        else
        {
            SetLockOnTarget(null);
            lockOnTriggered = false;
        }
    }

    private void OnRetargetAction(InputAction.CallbackContext context)
    {
        if (IsLockedOn) CheckForLockOnTarget();
    }

    #endregion

    #region Camera Target Updates

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
        if (isFollowingPlayer)
            _lastPlayerPosition = pc.cameraFollowTarget.position;

        Vector3 enemyTarget = IsLockedOn
            ? TargetedEnemy.TargetedPosition()
            : playerTargetTransform.position;

        playerTargetTransform.position = _lastPlayerPosition;
        enemyTargetTransform.position = EaseUtil.DampVector3(
            enemyTargetTransform.position, enemyTarget, lockOnTargetDamp, lockOnTargetDampDt);
    }

    private void UpdateFinisherTargets()
    {
        if (isFollowingPlayer)
            _lastPlayerPosition = pc.cameraFollowTarget.position;

        playerTargetTransform.position = _lastPlayerPosition;
        finisherTargetTransform.position = FinisherTarget != null
            ? FinisherTarget.TargetedPosition()
            : playerTargetTransform.position;
    }

    private void CheckForLockOnTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            playerTargetTransform.position, pc.psm.playerData.lockOnRange);

        if (hits.Length == 0) return;

        LockOnTarget best = null;
        foreach (var c in hits)
        {
            if (!c.TryGetComponent(out LockOnTarget t)) continue;
            if (t == TargetedEnemy) continue;
            if (!IsOnScreen(t.TargetedPosition(), 0.1f)) continue;

            if (best == null ||
                Vector3.Distance(pc.transform.position, t.TargetedPosition()) <
                Vector3.Distance(pc.transform.position, best.TargetedPosition()))
                best = t;
        }

        if (best != null) SetLockOnTarget(best);
    }

    private void ValidateLockedOnTarget()
    {
        if (lockOnTriggered && !IsLockedOn)
            CheckForLockOnTarget();

        if (!lockOnTriggered)
            SetLockOnTarget(null);

        if (TargetedEnemy != null &&
            Vector3.Distance(playerTargetTransform.position, TargetPosition) >
            pc.psm.playerData.lockOnRange)
            SetLockOnTarget(null);
    }

    #endregion

    #region Screen Utilities

    private bool IsOnScreen(Vector3 position, float edgeOffset)
    {
        float min = edgeOffset;
        float max = 1f - edgeOffset;
        Vector3 vp = camBrain.OutputCamera.WorldToViewportPoint(position);
        return vp.z > min && vp.x > min && vp.x < max && vp.y > min && vp.y < max;
    }

    private Vector2 GetViewportOutOfBounds(Vector3 position, float edgeOffset)
    {
        float min = edgeOffset;
        float max = 1f - edgeOffset;
        Vector3 vp = camBrain.OutputCamera.WorldToViewportPoint(position);
        float x = Mathf.Max(0f, Mathf.Max(min - vp.x, vp.x - max));
        float y = Mathf.Max(0f, Mathf.Max(min - vp.y, vp.y - max));
        return new Vector2(x, y);
    }

    #endregion

    #region Effects

    /** <summary>Triggers a camera impulse shake. Magnitude is forwarded directly to CinemachineImpulseSource.</summary> */
    public void ShakeCamera(float duration, float magnitude)
    {
        impulseSource?.GenerateImpulseWithForce(magnitude);
    }

    /** <summary>Enables or disables all CinemachineInputAxisControllers (used to pause camera rotation during menus).</summary> */
    public void EnableCameraInputDetection(bool on)
    {
        foreach (var c in _inputAxisControllers)
            c.enabled = on;
    }

    #endregion

    #region Serializable Types

    [Serializable]
    public class CineCamInfo
    {
        [FormerlySerializedAs("cameraState")] public PlayerCamStates playerCamState;
        public CinemachineCamera cam;
    }

    #endregion
}

public enum PlayerCamStates
{
    Free,
    LockedOn,
    FinisherCloseUp,
    OverworldFocus
}
