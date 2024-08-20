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
	    #endregion
	    
	    pc.moveInput = InputManager.Instance.movement.ReadValue<Vector2>();

	    Debug.Log(InputManager.Instance.movement.ReadValue<Vector2>());

	    if (Physics2D.OverlapBox(pc.groundCheckPoint.position, pc.groundCheckSize, 0, pc.groundLayer))
	    {
		    pc.lastOnGroundTime = pc.playerData.coyoteTime; //if so sets the lastGrounded to coyoteTime
	    }
	    
	    if (pc.IsJumping && pc.rb.velocity.y < 0)
	    {
		    pc.IsJumping = false;

		    pc.isJumpFalling = true;
	    }

	    if (pc.lastOnGroundTime > 0 && !pc.IsJumping)
	    {
		    pc.isJumpCut = false;

		    pc.isJumpFalling = false;
	    }
	    
	    if (pc.CanJump && pc.lastPressedJumpTime > 0)
	    {
		    pc.IsJumping = true;
		    pc.isJumpCut = false;
		    pc.isJumpFalling = false;
		    Jump();
	    }

    }

    public override void OnFixedUpdate()
    {
        Run(1);
    }

    public override void OnExit()
    {
        
    }
    
    #endregion
    
    
    #region Input Callbacks
    
    private void OnJumpAction(InputAction.CallbackContext context)
	{
		Debug.Log("Jump Pressed");
	    pc.lastPressedJumpTime = pc.playerData.jumpInputBufferTime;
	}
    
    #endregion
    
    #region Movement Methods
    
    private void Run(float lerpAmount)
	{
		Vector2 targetSpeed = pc.moveInput * pc.playerData.runMaxSpeed;
		targetSpeed = Vector2.Lerp(pc.rb.velocity, targetSpeed, lerpAmount);


		float accelRate;
		if (pc.lastOnGroundTime > 0)
			accelRate = (Mathf.Abs(targetSpeed.magnitude) > 0.01f) ? pc.playerData.runAccelAmount : pc.playerData.runDeccelAmount;
		else
			accelRate = (Mathf.Abs(targetSpeed.magnitude) > 0.01f) ? pc.playerData.runAccelAmount * pc.playerData.accelInAir : 
															pc.playerData.runDeccelAmount * pc.playerData.deccelInAir;
		
		
		if ((pc.IsJumping || pc.isJumpFalling) && Mathf.Abs(pc.rb.velocity.y) < pc.playerData.jumpHangTimeThreshold)
		{
			accelRate *= pc.playerData.jumpHangAccelerationMult;
			targetSpeed *= pc.playerData.jumpHangMaxSpeedMult;
		}
		
		Vector2 speedDiff = targetSpeed - MathUtil.ToVector2(pc.rb.velocity);
		
		Vector2 movementForce = speedDiff * accelRate;
		
		pc.rb.AddForce(movementForce, ForceMode.Force);
	}
    
	private void Jump()
	{
		//Ensures we can't call Jump multiple times from one press
		pc.lastPressedJumpTime = 0;
		pc.lastOnGroundTime = 0;

		#region Perform Jump
		//We increase the force applied if we are falling
		//This means we'll always feel like we jump the same amount 
		float force = pc.playerData.jumpForce;
		if (pc.rb.velocity.y < 0)
			force -= pc.rb.velocity.y;

		pc.rb.AddForce(Vector2.up * force, ForceMode.Impulse);
		#endregion
	}
    
    #endregion
}
