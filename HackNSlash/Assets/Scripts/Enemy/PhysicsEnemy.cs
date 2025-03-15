using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;
using PrimeTween;
using UnityEngine.Serialization;

// All enemies that are able to take knockback should inherit from this class
public class PhysicsEnemy : LockOnTarget
{
    public EnemyGravity gravityData;
    
    private float gravityScale;
    [HideInInspector] public bool pauseGravity;
    
    [HideInInspector]
    public Rigidbody rb;
    
    public bool IsGrounded =>
        Physics.CheckBox(groundCheckPoint.position, groundCheckSize, Quaternion.identity, groundLayer);

    public bool knockbackImmune;
    public bool TakeKnockback => !knockbackImmune && physicsInteract;

    public bool physicsInteract = true;
    
    #region CHECK PARAMETERS
   
    [Header("Checks")] 
    [SerializeField] public Transform groundCheckPoint;
    [SerializeField] public Vector3 groundCheckSize = new Vector3(0.49f, 0.3f, 0.49f);
    
    [SerializeField] private LayerMask groundLayer;
    
    #endregion

    public override Vector3 TargetedPosition(float deltaTime = 0)
    {
        return base.TargetedPosition(deltaTime) + DeltaPosition(deltaTime);
    }

    public override void OnStart()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.useGravity = false;
        knockbackImmune = false;
    }

    public override void OnUpdate()
    {
        CalculateGravity();
    }

    public override void OnFixedUpdate()
    {
        ApplyGravity();
    }

    public override void OnLateUpdate()
    {
        AvoidPlayerClipping();
    }
    
    private void AvoidPlayerClipping()
    {
        // do at some point (stop enemy from staying clipped into player after attack)
    }

    #region Gravity Methods

    public void CalculateGravity()
    {
        if (pauseGravity || !physicsInteract) SetGravityScale(0);
        else if (!IsGrounded && Mathf.Abs(rb.linearVelocity.y) < gravityData.jumpHangTimeThreshold)
        {
            SetGravityScale(gravityData.gravityScale * gravityData.jumpHangGravityMult);
        }
        else if (rb.linearVelocity.y < 0)
        {
            //Higher gravity if falling
            SetGravityScale(gravityData.gravityScale * gravityData.fallGravityMult);
            //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -gravityData.maxFallSpeed), rb.linearVelocity.z);
        }
        else
        {
            //Default gravity if standing on a platform or moving upwards
            SetGravityScale(gravityData.gravityScale);
        }
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
    
    
    public bool ForceKnockback(Vector3 force, ForceMode mode = ForceMode.VelocityChange)
    {
        if (!TakeKnockback) return false;
        
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force, mode);
        return true;
    }

    public bool TraverseDistKnockback(Vector3 direction, float distance, float time, Func<bool> condition = null)
    {
        Debug.Log("Traversing");
        if (!TakeKnockback) return false;

        rb.linearVelocity = Vector3.zero;
        this.RunSegmentCoroutine(rb.TraverseDistanceInTime(direction, distance, time, condition));
        return true;
    }

    public bool SetVelocityKnockback(Vector3 velocity)
    {
        if (!TakeKnockback) return false;
        
        rb.linearVelocity = velocity;
        return true;
    }
    
    public bool TweenKnockback(Vector3 direction, float distance, float time, Ease ease = Ease.Default)
    {
        if (!TakeKnockback) return false;
        
        rb.linearVelocity = Vector3.zero;
        transform.TweenDistance(direction, distance, time, ease);
        return true;
    }

    #endregion

    public override void OnHit(PlayerController pc, Attack a, int actionIndex = 0)
    {
        base.OnHit(pc, a, actionIndex);
        
        OnHitEvents.OnHitActionMap[a.hitInfo.onHitActions[actionIndex]](pc, this, a);
    }
}
