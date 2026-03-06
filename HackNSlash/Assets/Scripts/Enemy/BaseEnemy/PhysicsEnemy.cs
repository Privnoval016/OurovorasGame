using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;
using PrimeTween;

// All enemies that are able to take knockback should inherit from this class
public class PhysicsEnemy : LockOnTarget
{
    /**
     * <summary>
     * Defines how this enemy takes knockback from attacks.  Knockback is only applied if <see cref="TakeKnockback"/> is true.
     * </summary>
     */
    public enum KnockbackMode
    {
        [Tooltip("This enemy is completely immune to knockback.")]
        KnockbackImmune,
        [Tooltip("This enemy is immune to knockback while its shield is active.")]
        ImmuneWhenShielded,
        [Tooltip("This enemy always takes knockback from attacks, regardless of shield state.")]
        AlwaysKnockback
    }
    
    public event Action<ElementEffect, PlayerController, Attack, Transform, int> onHit = delegate { };
    public event Action<ElementEffect, PlayerController, Attack, Transform, int> onStagger = delegate { };
    
    [Header("Physics Parameters")]
    
    public EnemyGravity gravityData;

    /** <summary>Unified physics body for separation and slope constraints.</summary> */
    public CharacterPhysicsBody physicsBody;

    /** <summary>
     * Optional shared config asset.  When assigned, knockback velocities are clamped to
     * <see cref="PhysicsConfig.maxEnemyKnockbackSpeedH"/> / <see cref="PhysicsConfig.maxEnemyKnockbackSpeedV"/>
     * so dash-into-enemy events cannot send enemies flying.
     * </summary>
     */
    [Tooltip("Optional. When assigned, knockback velocity is clamped via config values.")]
    public PhysicsConfig physicsConfig;

    private float gravityScale;
    private bool pauseGravity;

    public bool IsGrounded =>
        Physics.CheckBox(groundCheckPoint.position, groundCheckSize, Quaternion.identity, GameManager.Instance.groundLayer);

    [Header("Knockback Parameters")]
    [Tooltip("Determines how this enemy takes knockback from attacks.  Knockback is only applied if TakeKnockback is true.")]
    public KnockbackMode knockbackMode = KnockbackMode.AlwaysKnockback;
    
    public bool knockbackImmuneOverride = false;

    public bool TakeKnockback
    {
        get
        {
            bool takeKnockback = physicsInteract && !knockbackImmuneOverride;
        
            bool isShielded = damageable.DamageableComponents.TryGetComponent(out ShieldComponent sc) && sc.CurrentShieldPercentage > 0;
        
            switch (knockbackMode)
            {
                case KnockbackMode.KnockbackImmune:
                    takeKnockback = false;
                    break;
                case KnockbackMode.ImmuneWhenShielded:
                    takeKnockback = takeKnockback && !isShielded;
                    break;
            }
            return takeKnockback;
        }
    }

    public bool physicsInteract = true;
    private float physicsLockTime;

    #region CHECK PARAMETERS

    [Header("Checks")] 
    [SerializeField] private Transform lockOnAimPoint;
    [SerializeField] private Vector3 lockOnAimOffset = new Vector3(0, 0, 0);
    [SerializeField] public Transform groundCheckPoint;
    [SerializeField] public Vector3 groundCheckSize = new Vector3(0.49f, 0.3f, 0.49f);

    #endregion

    public override Vector3 TargetedPosition(float deltaTime = 0)
    {
        return base.TargetedPosition(deltaTime) + DeltaPosition(deltaTime);
    }
    
    public override Vector3 LockOnAimPosition(float deltaTime = 0)
    {
        if (lockOnAimPoint != null)
        {
            return lockOnAimPoint.position + lockOnAimOffset;
        }

        return base.LockOnAimPosition(deltaTime);
    }

    public override void OnStart()
    {
        col = GetComponent<Collider>();
        rb.useGravity = false;
        knockbackImmuneOverride = false;

        if (physicsBody == null)
            physicsBody = GetComponent<CharacterPhysicsBody>();
    }

    public override void OnUpdate()
    {
        CalculateGravity();
        CheckPhysicsLock();
    }

    public override void OnFixedUpdate()
    {
        ApplyGravity();
    }

    public override void OnLateUpdate()
    {
        AvoidColliderClipping();
    }

    private void AvoidColliderClipping()
    {
        // Body separation is handled globally by CharacterSeparationSystem.
    }

    #region Gravity Methods

    public void CalculateGravity()
    {
        if (pauseGravity || !physicsInteract)
        {
            SetGravityScale(0);
            return;
        }

        float vy = rb.linearVelocity.y;

        // Jump hang: only reduce gravity near the apex when the multiplier is
        // meaningful (> 0). A zero jumpHangGravityMult (default/crash-reset value)
        // would completely kill gravity and must be treated as disabled.
        bool hangActive = gravityData.jumpHangGravityMult > 0.01f
                          && !IsGrounded
                          && Mathf.Abs(vy) < gravityData.jumpHangSpeedThreshold;

        if (hangActive)
        {
            SetGravityScale(gravityData.gravityScale * gravityData.jumpHangGravityMult);
        }
        else if (vy < 0f)
        {
            // Higher gravity while falling; fallGravityMult must be >= 1 so we never
            // accidentally reduce gravity below normal while descending.
            SetGravityScale(gravityData.gravityScale * Mathf.Max(1f, gravityData.fallGravityMult));
            rb.linearVelocity = new Vector3(rb.linearVelocity.x,
                Mathf.Max(vy, -gravityData.maxFallSpeed), rb.linearVelocity.z);
        }
        else
        {
            SetGravityScale(gravityData.gravityScale);
        }
    }

    public void PauseGravity(bool pause, float overrideTime = -1f)
    {
        if (pause)
        {
            pauseGravity = true;
        }
        else
        {
            this.KillObjectCoroutines("UnpauseGravity");
            this.RunSegmentCoroutine(UnpauseGravity(overrideTime >= 0 ? overrideTime : gravityData.onHitHangTime), "UnpauseGravity");
        }
    }

    IEnumerator<float> UnpauseGravity(float delay)
    {
        yield return Timing.WaitForSeconds(delay);
        
        pauseGravity = false;
    }

    public void SetGravityScale(float scale)
    {
        gravityScale = scale;
    }
    
    private void ApplyGravity()
    {
        Vector3 gravity = GameManager.Instance.globalGravity * gravityScale * Vector3.up;
        rb.AddForce(gravity, ForceMode.Acceleration);
    }
    
    #endregion

    

    #region Knockback Methods
    
    public bool ResetMovement()
    {
        rb.linearVelocity = Vector3.zero;
        return true;
    }
    
    
    public bool ForceKnockback(Vector3 force, ForceMode mode = ForceMode.VelocityChange)
    {
        if (!TakeKnockback) return false;

        ResetMovement();
        rb.AddForce(force, mode);
        ClampKnockbackVelocity();
        return true;
    }

    public bool TraverseDistKnockback(Vector3 direction, float distance, float time, Func<bool> condition = null)
    {
        if (!TakeKnockback) return false;

        if (time <= 0) return false;

        ResetMovement();
        this.RunSegmentCoroutine(rb.TraverseDistanceInTime(direction, distance, time, condition));
        return true;
    }

    public bool SetVelocityKnockback(Vector3 velocity)
    {
        if (!TakeKnockback) return false;

        ResetMovement();
        rb.linearVelocity = velocity;
        ClampKnockbackVelocity();
        return true;
    }
    
    public bool TweenKnockback(Vector3 direction, float distance, float time, Ease ease = Ease.Default)
    {
        if (!TakeKnockback) return false;

        ResetMovement();
        transform.TweenDistance(direction, distance, time, ease);
        return true;
    }

    public void LockPhysics(float duration)
    {
        if (!TakeKnockback) return;
        
        ResetMovement();
        
        physicsInteract = false;
        physicsLockTime = duration;
    }

    /** <summary>
     * Clamps the rigidbody's velocity after a knockback is applied so that no
     * single event can send the enemy flying across the level.
     * Only active when a <see cref="PhysicsConfig"/> asset is assigned.
     * </summary>
     */
    private void ClampKnockbackVelocity()
    {
        if (physicsConfig == null) return;

        Vector3 v = rb.linearVelocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        float maxH = physicsConfig.maxEnemyKnockbackSpeedH;
        float maxV = physicsConfig.maxEnemyKnockbackSpeedV;

        if (horizontal.magnitude > maxH)
            horizontal = horizontal.normalized * maxH;

        float vy = Mathf.Clamp(v.y, -maxV, maxV);
        rb.linearVelocity = new Vector3(horizontal.x, vy, horizontal.z);
    }
    
    private void CheckPhysicsLock()
    {
        if (physicsLockTime > 0)
        {
            physicsLockTime -= Time.deltaTime;
            if (physicsLockTime <= 0)
            {
                physicsInteract = true;
            }
        }
    }

    #endregion
    
    #region LockOnTarget Methods

    public override void OnHit(ElementEffect element, PlayerController pc, Attack a, Transform attackerTransform, int actionIndex = 0)
    {
        onHit?.Invoke(element, pc, a, attackerTransform, actionIndex);
        
        base.OnHit(element, pc, a, attackerTransform, actionIndex);


        pc.ohe.KillObjectCoroutines(GetInstanceID().ToString());
        physicsLockTime = 0;
        physicsInteract = true;
        
        pc.ohe.ActivateHitAction(actionIndex, this, a, attackerTransform);
        
        
    }
    
    #endregion
    
    public override void OnStagger(ElementEffect element, PlayerController pc, Attack a, Transform attackerTransform, int actionIndex = 0)
    {
        onStagger?.Invoke(element, pc, a, attackerTransform, actionIndex);
        
        base.OnStagger(element, pc, a, attackerTransform, actionIndex);
    }
}
