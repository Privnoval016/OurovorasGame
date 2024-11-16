using System;
using Animancer;
using UnityEngine;
using UnityEngine.InputSystem;
using ExtensionUtils;
using Object = System.Object;

public class PlayerMoving : State
{
	private PlayerController pc;
	
	private WalkingAnimStates animState;
	
    #region State Methods
    
    public override void OnEnter()
    {
	    pc = (PlayerController) sc.parent;
	    //doNotRemove = true;
	    
	    InputManager.Instance.jump.performed += OnJumpAction;
	    
	    
	    pc.canAttack = true;
	    pc.comboIndex = -1;
	    
	    animState = WalkingAnimStates.Idle;
	    SwitchAnimState(WalkingAnimStates.Idle);
    }

    public override void OnUpdate()
    {
	    #region Timers
	    pc.lastOnGroundTime -= Time.deltaTime;
	    pc.lastPressedJumpTime -= Time.deltaTime;
	    
	    if (pc.lastOnGroundTime > 0)
			pc.lastDoubleJumpTime += Time.deltaTime;

	    if (pc.IsWalking && !pc.cam.isLockedOn && pc.rb.linearVelocity.ZeroVector3Axis().magnitude > 0.01f && pc.moveInput.magnitude > 0.95f)
	    {
		    pc.walkingTime += Time.deltaTime;
	    }
	    else
	    {
		    pc.walkingTime = 0;
	    }

	    
	    #endregion
	    
	    CheckJump();
	    
	    pc.CalculateGravity();
	    
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
    }

    public override void OnResume()
    {
	    pc.comboIndex = -1;
	    
	    animState = WalkingAnimStates.Idle;
	    SwitchAnimState(WalkingAnimStates.Idle);
	    
	    
	    pc.TurnToLook();
    }

    #endregion
    
    #region Input Callbacks
    
    private void OnJumpAction(InputAction.CallbackContext context)
    {
	    if (!pc.canAttack) return;
	    
	    if (sc.GetCurrentState() is PlayerAttacking) sc.ResumePrevious();
	    
	    if (pc.lastOnGroundTime > 0)
	    {
		    InputManager.Instance.ReleaseHoldAttacks();
		    pc.lastPressedJumpTime = pc.playerData.jumpInputBufferTime;
	    }
	    
	    else if (pc.attackData.doubleJumpEnabled && !pc.isDoubleJumpUsed)
	    {
		    InputManager.Instance.ReleaseHoldAttacks();
		    pc.isDoubleJumpTriggered = true;
	    }
    }
    
    #endregion
    
    #region Movement Methods
    
    private void Run(float lerpAmount)
    {
	    
	    Transform cam = pc.cam.transform;
	    pc.moveDirection = pc.moveInput.x * cam.right.ZeroVector3Axis().normalized + 
	                            pc.moveInput.y * cam.forward.ZeroVector3Axis().normalized;
		
	    Vector3 targetSpeed = pc.moveDirection * (pc.IsSprinting ? pc.playerData.sprintMaxSpeed : pc.playerData.runMaxSpeed);
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
		
		Vector3 speedDiff = targetSpeed - pc.rb.linearVelocity.ZeroVector3Axis();
		
		Vector3 movementForce = speedDiff * accelRate;
		
		pc.rb.AddForce(movementForce, ForceMode.Force);
		
		pc.TurnToLook();
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

		SwitchAnimState(WalkingAnimStates.Jumping);
		
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
		
		SwitchAnimState(WalkingAnimStates.DoubleJumping, () => SwitchAnimState(WalkingAnimStates.Falling));
		
		pc.lastDoubleJumpTime = 0;
		
		#region Perform Double Jump
		
		float force = pc.playerData.doubleJumpForce;
		
		pc.rb.linearVelocity = pc.rb.linearVelocity.ZeroVector3Axis();
		
		pc.rb.AddForce(Vector3.up * force, ForceMode.Impulse);
		#endregion
	}
    
    #endregion
    
    
    #region Animation Methods

    private void UpdateAnimation()
    {
	    switch (animState)
	    {
		    case WalkingAnimStates.Idle:
			    if (pc.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);
			    else if (pc.cam.isLockedOn && pc.IsWalking) SwitchAnimState(WalkingAnimStates.Targeting);
			    else if (pc.IsSprinting) SwitchAnimState(WalkingAnimStates.Sprinting);
			    else if (pc.IsWalking) SwitchAnimState(WalkingAnimStates.Walking);

			    break;
		    
		    case WalkingAnimStates.Walking:
			    if (pc.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);
			    else if (pc.cam.isLockedOn) SwitchAnimState(WalkingAnimStates.Targeting);
			    else if (pc.IsSprinting) SwitchAnimState(WalkingAnimStates.Sprinting);
			    else if (!pc.IsWalking) SwitchAnimState(WalkingAnimStates.Idle, null, true);

			    break;
		    
		    case WalkingAnimStates.Sprinting:
			    if (pc.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);
			    else if (pc.cam.isLockedOn) SwitchAnimState(WalkingAnimStates.Targeting);
			    else if (!pc.IsWalking) SwitchAnimState(WalkingAnimStates.Idle, null, true);
			    else if (!pc.IsSprinting) SwitchAnimState(WalkingAnimStates.Walking);

			    break;
		    
		    case WalkingAnimStates.Targeting:
			    if (pc.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);
			    else if (!pc.IsWalking) SwitchAnimState(WalkingAnimStates.Idle, null, true);
			    else if (!pc.cam.isLockedOn && !pc.IsSprinting) SwitchAnimState(WalkingAnimStates.Walking);
			    else if (pc.IsSprinting) SwitchAnimState(WalkingAnimStates.Sprinting);
			    break;
		    
		    case WalkingAnimStates.Falling:
			    if (pc.IsGrounded) SwitchAnimState(WalkingAnimStates.Idle);
			    
			    break;
		    
		    case WalkingAnimStates.Jumping:
			    if (!pc.isJumpFalling || pc.rb.linearVelocity.y > 0) break;
			    
			    if (pc.IsGrounded) SwitchAnimState(WalkingAnimStates.Idle);
			    else if (pc.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);

			    break;
		    
		    case WalkingAnimStates.DoubleJumping:
			    // if (pc.rb.linearVelocity.y > 0) break;
			    //
			    // if (pc.IsGrounded) SwitchAnimState(WalkingAnimStates.Idle);
			    // else if (pc.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);

			    break;
	    }
	    
	    //Debug.Log(animState);
    }
    
    private void SwitchAnimState(WalkingAnimStates newState, Action onExit = null, bool playExit = false)
    {
	    Object currentAnim = pc.moveAnimData._animStates[animState];
	    Object nextAnim = pc.moveAnimData._animStates[newState];
	    
	    playExit = playExit && currentAnim is Loop;

	    if (playExit)
	    {
		    Loop currentLoop = (Loop) currentAnim;
		    
		    if (nextAnim is Loop nextLoop)
		    {
			    ExitTimeAnimation(currentLoop.EndClip, nextLoop.LoopClip, onExit);
		    }
		    else
		    {
			    ExitTimeAnimation(currentLoop.EndClip, (ITransition) nextAnim, onExit);
		    }
	    }
	    else if (nextAnim is Loop nextLoop)
	    {
		    pc.PlayAnimation(nextLoop.LoopClip).Events(this).OnEnd ??= () => OnAnimExit(null, onExit);
	    }
	    else
	    {
		    pc.PlayAnimation((ITransition) nextAnim).Events(this).OnEnd ??= () => OnAnimExit(null, onExit);
	    }
	    
	    InputManager.Instance.ReleaseHoldAttacks();
	    animState = newState;
    }
    
    private void ExitTimeAnimation(ITransition currentAnim, ITransition nextAnim, Action onExit = null)
	{
		AnimancerState state = pc.PlayAnimation(currentAnim);
		state.Events(this).OnEnd ??= () => OnAnimExit(nextAnim, onExit);
	}
	
	void OnAnimExit(ITransition nextAnim, Action onExit = null)
	{
		if (nextAnim != null) pc.PlayAnimation(nextAnim);
		onExit?.Invoke();
	}
	
    
    #endregion
}
