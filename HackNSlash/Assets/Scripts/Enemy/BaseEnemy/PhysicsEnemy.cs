using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;
using PrimeTween;

// All enemies that are able to take knockback should inherit from this class
public class PhysicsEnemy : LockOnTarget
{
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

    public bool knockbackImmune;
    public bool TakeKnockback => !knockbackImmune && physicsInteract;

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
        knockbackImmune = false;

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
        if (pauseGravity || !physicsInteract) SetGravityScale(0);
        else if (!IsGrounded && Mathf.Abs(rb.linearVelocity.y) < gravityData.jumpHangSpeedThreshold)
        {
            SetGravityScale(gravityData.gravityScale * gravityData.jumpHangGravityMult);
        }
        else if (rb.linearVelocity.y < 0)
        {
            //Higher gravity if falling
            SetGravityScale(gravityData.gravityScale * gravityData.fallGravityMult);
            //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
            rb.linearVelocity = new Vector3(rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, -gravityData.maxFallSpeed), rb.linearVelocity.z);
        }
        else
        {
            //Default gravity if standing on a platform or moving upwards
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
