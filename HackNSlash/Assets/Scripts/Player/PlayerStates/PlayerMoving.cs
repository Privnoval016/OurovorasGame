using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class PlayerMoving : State
{
	private PlayerController pc;
	
    #region State Methods
    
    public override void OnEnter()
    {
	    pc = (PlayerController) sc.parent;
	    InputManager.Instance.jump.performed += OnJumpAction;
    }

    public override void OnUpdate()
    {
	    #region Timers
	    pc.lastOnGroundTime -= Time.deltaTime;
	    pc.lastPressedJumpTime -= Time.deltaTime;
	    
	    if (pc.lastOnGroundTime > 0)
			pc.lastDoubleJumpTime += Time.deltaTime;
	    #endregion
	    
	    pc.moveInput = InputManager.Instance.movement.ReadValue<Vector2>();
	    
	    CheckJump();

	    CalculateGravity();
    }

    public override void OnFixedUpdate()
    {
        Run(1);
        ApplyGravity();
    }

    public override void OnExit()
    {
        
    }
    
    #endregion
    
    
    #region Input Callbacks
    
    private void OnJumpAction(InputAction.CallbackContext context)
    {
	    if (!pc.isDoubleJumpUsed)
	    {
		    pc.isDoubleJumpTriggered = true;
	    }
	    
	    else if (pc.lastOnGroundTime > 0)
		    pc.lastPressedJumpTime = pc.playerData.jumpInputBufferTime;
    }
    
    #endregion
    
    #region Gravity Methods

    private void CalculateGravity()
    {
	    if (pc.isJumping && Mathf.Abs(pc.rb.linearVelocity.y) < pc.playerData.jumpHangTimeThreshold)
	    {
		    SetGravityScale(pc.playerData.gravityScale * pc.playerData.jumpHangGravityMult);
	    }
	    else if (pc.rb.linearVelocity.y < 0)
	    {
		    //Higher gravity if falling
		    SetGravityScale(pc.playerData.gravityScale * pc.playerData.fallGravityMult);
		    //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
		    pc.rb.linearVelocity = new Vector2(pc.rb.linearVelocity.x, Mathf.Max(pc.rb.linearVelocity.y, -pc.playerData.maxFallSpeed));
	    }
	    else
	    {
		    //Default gravity if standing on a platform or moving upwards
		    SetGravityScale(pc.playerData.gravityScale);
	    }
    }
    
    private void SetGravityScale(float scale)
    {
	    pc.gravityScale = scale;
    }
    
    private void ApplyGravity()
	{
		Vector3 gravity = pc.globalGravity * pc.gravityScale * Vector3.up;
		pc.rb.AddForce(gravity, ForceMode.Acceleration);
	}
    
    #endregion
    
    #region Movement Methods
    
    private void Run(float lerpAmount)
    {
	    Transform cam = Camera.main.transform;
	    Vector3 moveDirection = pc.moveInput.x * MathUtil.ZeroVector3Axis(cam.right).normalized + 
	                            pc.moveInput.y * MathUtil.ZeroVector3Axis(cam.forward).normalized;
		
	    Vector3 targetSpeed = moveDirection * pc.playerData.runMaxSpeed;
		targetSpeed = Vector3.Lerp(pc.rb.linearVelocity, targetSpeed, lerpAmount);


		float accelRate;
		if (pc.lastOnGroundTime > 0)
			accelRate = (Mathf.Abs(targetSpeed.magnitude) > 0.01f) ? pc.playerData.runAccelAmount : pc.playerData.runDecelAmount;
		else
			accelRate = (Mathf.Abs(targetSpeed.magnitude) > 0.01f) ? pc.playerData.runAccelAmount * pc.playerData.accelInAir : 
															pc.playerData.runDecelAmount * pc.playerData.decelInAir;
		
		
		if ((pc.isJumping || pc.isJumpFalling) && Mathf.Abs(pc.rb.linearVelocity.y) < pc.playerData.jumpHangTimeThreshold)
		{
			accelRate *= pc.playerData.jumpHangAccelerationMult;
			targetSpeed *= pc.playerData.jumpHangMaxSpeedMult;
		}
		
		Vector3 speedDiff = targetSpeed - MathUtil.ZeroVector3Axis(pc.rb.linearVelocity);
		
		Vector3 movementForce = speedDiff * accelRate;
		
		pc.rb.AddForce(movementForce, ForceMode.Force);
	}
    
    #endregion
    
    #region Jump Methods

    private void CheckJump()
    {
	    if (pc.IsGrounded)
	    {
		    pc.lastOnGroundTime = pc.playerData.coyoteTime; //if so sets the lastGrounded to coyoteTime
		    pc.lastDoubleJumpTime = 0;
		    pc.isDoubleJumpUsed = false;
		    pc.isDoubleJumpTriggered = false;
	    }

	    if (!pc.isDoubleJumpTriggered && (pc.isJumping || pc.isJumpFalling))
	    {
		    pc.lastDoubleJumpTime += Time.deltaTime;
	    }

	    if (pc.isJumping && pc.rb.linearVelocity.y < 0)
	    {
		    pc.isJumping = false;
		    pc.isJumpFalling = true;
	    }

	    if (pc.CanJump)
	    {
		    pc.isJumpFalling = false;
		    
		    if (pc.lastPressedJumpTime > 0)
		    {
			    pc.isJumping = true;

			    Jump();
		    }
	    }

		if (pc.CanDoubleJump && pc.isDoubleJumpTriggered)
	    {
		    pc.isDoubleJumpUsed = true;
		    pc.isDoubleJumpTriggered = false;
		    
		    DoubleJump();
	    }
    }
    
	private void Jump()
	{
		Debug.Log("Jumping");
		//Ensures we can't call Jump multiple times from one press
		pc.lastPressedJumpTime = 0;
		pc.lastOnGroundTime = 0;

		#region Perform Jump
		//We increase the force applied if we are falling
		//This means we'll always feel like we jump the same amount 
		float force = pc.playerData.jumpForce; 
		if (pc.rb.linearVelocity.y < 0)
			force -= pc.rb.linearVelocity.y;
		
		pc.rb.AddForce(Vector3.up * force, ForceMode.Impulse);
		#endregion
	}

	private void DoubleJump()
	{
		Debug.Log("Double Jumping");
		pc.lastDoubleJumpTime = 0;
		
		#region Perform Double Jump
		
		float force = pc.playerData.doubleJumpForce;
		
		pc.rb.linearVelocity = MathUtil.ZeroVector3Axis(pc.rb.linearVelocity);
		
		pc.rb.AddForce(Vector3.up * force, ForceMode.Impulse);
		#endregion
	}
    
    #endregion
}
