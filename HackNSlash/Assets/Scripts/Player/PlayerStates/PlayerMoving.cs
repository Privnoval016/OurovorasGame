using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoving : State
{
	private PlayerController pc;

	private enum WalkingAnimStates
	{
		Idle,
		Walking,
		Targeting,
		Sprinting,
		Falling,
		Jumping,
		DoubleJumping,
		Standby
	}
	
	private WalkingAnimStates animState;
	
    #region State Methods
    
    public override void OnEnter()
    {
	    pc = (PlayerController) sc.parent;
	    //doNotRemove = true;
	    
	    InputManager.Instance.jump.performed += OnJumpAction;
	    
	    pc.canAttack = true;
	    pc.comboIndex = -1;
	    
	    SwitchAnimState(WalkingAnimStates.Idle);
    }

    public override void OnUpdate()
    {
	    #region Timers
	    pc.lastOnGroundTime -= Time.deltaTime;
	    pc.lastPressedJumpTime -= Time.deltaTime;
	    
	    if (pc.lastOnGroundTime > 0)
			pc.lastDoubleJumpTime += Time.deltaTime;

	    if (pc.IsWalking && !pc.cam.isLockedOn && MathUtil.ZeroVector3Axis(pc.rb.linearVelocity).magnitude > 0.01f && pc.moveInput.magnitude > 0.95f)
	    {
		    pc.walkingTime += Time.deltaTime;
	    }
	    else
	    {
		    pc.walkingTime = 0;
	    }

	    
	    #endregion
	    
	    CheckJump();

	    CalculateGravity();
	    
	    UpdateAnimation();
    }

    public override void OnFixedUpdate()
    {
        Run(1);
    }

    public override void OnExit()
    {
	    InputManager.Instance.jump.performed -= OnJumpAction;
    }

    public override void OnInterrupt()
    {
	    pc.isDoubleJumpUsed = false;
	    animState = WalkingAnimStates.Standby;
    }

    public override void OnResume()
    {
	    animState = WalkingAnimStates.Standby;
	    if (pc.IsMidair) SwitchAnimState(WalkingAnimStates.Falling, false);
    }

    #endregion
    
    
    #region Input Callbacks
    
    private void OnJumpAction(InputAction.CallbackContext context)
    {
	    if (!pc.canAttack) return;
	    
	    if (sc.GetCurrentState() is PlayerAttacking) sc.ResumePrevious();
	    
	    if (pc.lastOnGroundTime > 0)
	    {
		    pc.lastPressedJumpTime = pc.playerData.jumpInputBufferTime;
	    }
	    
	    else if (pc.attackData.doubleJumpEnabled && !pc.isDoubleJumpUsed)
	    {
		    pc.isDoubleJumpTriggered = true;
	    }
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
    
    #endregion
    
    #region Movement Methods
    
    private void Run(float lerpAmount)
    {
	    
	    Transform cam = pc.cam.transform;
	    pc.moveDirection = pc.moveInput.x * MathUtil.ZeroVector3Axis(cam.right).normalized + 
	                            pc.moveInput.y * MathUtil.ZeroVector3Axis(cam.forward).normalized;
		
	    Vector3 targetSpeed = pc.moveDirection * (pc.IsSprinting ? pc.playerData.sprintMaxSpeed : pc.playerData.runMaxSpeed);
		targetSpeed = Vector3.Lerp(pc.rb.linearVelocity, targetSpeed, lerpAmount);
		
		if (animState is WalkingAnimStates.Walking or WalkingAnimStates.Targeting)
		{
			pc.animancer.speed = targetSpeed.magnitude / pc.playerData.runMaxSpeed;
		}
		else
		{
			pc.animancer.speed = 1;
		}


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
		
		TurnToLook();
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
	    

	    if (pc.CanJump)
	    {
		    pc.isJumpFalling = false;
		    
		    if (pc.IsJumpTriggered)
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
		
	    if (pc.rb.linearVelocity.y < 0 && pc.isJumping)
	    {
		    pc.isJumping = false;
		    pc.isJumpFalling = true;
	    }
    }
    
	private void Jump()
	{
		Debug.Log("Jumping");
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
    
    #region Look Methods

    private void TurnToLook()
    {
	    if (!pc.IsWalking) return;
	    
	    if (!pc.cam.isLockedOn)
	    {
		    pc.transform.rotation =
			    EaseUtil.DampQuaternion(pc.transform.rotation, Quaternion.LookRotation(pc.moveDirection), 5f, 0.1f);
	    }
	    else
	    {
		    Vector3 lookDir = MathUtil.ZeroVector3Axis(pc.cam.targetedEnemy.transform.position - pc.transform.position);

		    pc.transform.rotation =
			    EaseUtil.DampQuaternion(pc.transform.rotation, Quaternion.LookRotation(lookDir), 5f, 0.1f);
	    }
    }
    
    #endregion
    
    #region Animation Methods

    private void UpdateAnimation()
    {
	    
	    WalkingAnimStates newState = WalkingAnimStates.Idle;
	    
	    //if (pc.IsGrounded && !pc.IsWalking) newState = WalkingAnimStates.Idle;
	    if (pc.isDoubleJumpUsed) newState = WalkingAnimStates.DoubleJumping;
	    else if (pc.IsPerformingJump) newState = WalkingAnimStates.Jumping;
	    else if (pc.IsMidair) newState = WalkingAnimStates.Falling;
	    else if (pc.cam.isLockedOn && pc.IsWalking) newState = WalkingAnimStates.Targeting;
	    else if (pc.IsSprinting) newState = WalkingAnimStates.Sprinting;
	    else if (pc.IsWalking) newState = WalkingAnimStates.Walking;
	    
	    SwitchAnimState(newState);
	    
	    Debug.Log(animState);
    }
    
    private void SwitchAnimState(WalkingAnimStates newState, bool uniqueUpdateOnly = true)
	{
		if (uniqueUpdateOnly && animState == newState) return;
		
	    switch (newState)
	    {
		    case WalkingAnimStates.Idle:
			    
			    pc.animancer.SetTrigger(Animator.StringToHash("Exit"));
			    
			    break;
		    
		    case WalkingAnimStates.Walking:
			    
			    SafeCrossFade(Animator.StringToHash("WalkLoop"), 0.01f);
			    
			    break;
		    
		    case WalkingAnimStates.Targeting:
			    
				SafeCrossFade(Animator.StringToHash("TargetedWalkLoop"), 0.01f);
			    
			    break;
		    case WalkingAnimStates.Sprinting:
			    
			    SafeCrossFade(Animator.StringToHash("SprintLoop"), 0.25f);
			    
			    break;
		    case WalkingAnimStates.Falling:
			    
			    //if (animState != WalkingAnimStates.Jumping && animState != WalkingAnimStates.DoubleJumping)
				    SafeCrossFade(Animator.StringToHash("FallingLoop"), 0.01f);
			    break;
		    case WalkingAnimStates.Jumping:
			    SafeCrossFade(Animator.StringToHash("JumpAction"), 0.01f);
			    break;
		    case WalkingAnimStates.DoubleJumping:
			    if (animState == WalkingAnimStates.Targeting)
			    {
				    SafeCrossFade(Animator.StringToHash("DoubleJumpTarget"), 0.01f);
			    }
			    else
			    {
				    SafeCrossFade(Animator.StringToHash("DoubleJump"), 0.01f);
			    }

			    break;
	    }
	    
	    animState = newState;
	}
    
    private void SafeCrossFade(int stateNameHash, float fadeDuration = -1F, int layer = -1, float normalizedTime = Single.NegativeInfinity)
	{
	    if (pc.animancer.IsPlaying(stateNameHash)) return;
	    
	    pc.animancer.CrossFade(stateNameHash, fadeDuration, layer, normalizedTime);
	}
    
    public void EnterStandby()
	{
	    SwitchAnimState(WalkingAnimStates.Standby);
	}
    
    #endregion
}
