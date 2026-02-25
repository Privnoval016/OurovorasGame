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
    public CollisionListener collisionListener;

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
        if (!TakeKnockback) return;
        if (collisionListener == null || !collisionListener.activeMover) return;

        Vector3 direction = collisionListener.GetCombinedDirection();
        if (direction != Vector3.zero)
        {
            rb.AddForce(direction * gravityData.colliderBuffer, ForceMode.VelocityChange);
        }
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
