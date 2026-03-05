using System;
using Animancer;
using UnityEngine;
using UnityEngine.InputSystem;
using Extensions.Utils;
using Object = System.Object;

public enum MovingStates
{
	NonCombat,
	DualSword,
	Katana
}

public class PlayerMoving : PlayerState
{
	
    #region State Methods
    
    public override void OnEnter()
    {
	    doNotRemove = true;
	    
	    InputManager.Instance.onJump += OnJumpAction;
	    InputManager.Instance.onSwapMode += OnSwitchAction;
	    InputManager.Instance.onUltimateMode += OnUltimateAction;
	    
	    pc.pac.RootMotionEnabled(false);
	    pc.cam.isFollowingPlayer = true;
	    
	    pc.psm.canAttack = true;
	    pc.psm.currentPlayerAttack = null;
	    
	    pc.pac.walkingAnim = WalkingAnimStates.Idle;
	    pc.pac.SwitchAnimState(WalkingAnimStates.Idle);
    }

    public override void OnUpdate()
    {
	    if (!(pc.psm.IsWalking && !pc.cam.IsLockedOn && pc.rb.linearVelocity.ZeroVector3Axis().magnitude > 0.01f 
	        && pc.psm.moveInput.magnitude > 0.95f))
	    {
		    pc.psm.WalkingTimer.Reset();
		    
	    }
	    
	    pc.psm.CalculateGravity();
	    
	    pc.pac.UpdateAnimation();
	    
	    PauseCallbacks();
	    
	    if (pc.psm.pauseMovement) return;
	    
	    CheckJump();
    }

    public override void OnFixedUpdate()
    {
	    if (pc.psm.pauseMovement) return;
	    
        Run(1);
    }

    public override void OnExit()
    {
	    InputManager.Instance.onJump -= OnJumpAction;
	    InputManager.Instance.onSwapMode -= OnSwitchAction;
	    InputManager.Instance.onUltimateMode -= OnUltimateAction;
    }

    public override void OnInterrupt()
    {
	    
    }

    public override void OnResume()
    {
	    pc.pac.walkingAnim = WalkingAnimStates.Idle;
	    pc.pac.SwitchAnimState(WalkingAnimStates.Idle);
	    
	    pc.pac.RootMotionEnabled(false);
	    pc.cam.isFollowingPlayer = true;
	    
	    pc.psm.currentPlayerAttack = null;
	    
	    pc.psm.TurnToLook();
    }

    #endregion
    
    #region Input Callbacks
    
    private void PauseCallbacks()
	{
		if (GameManager.Instance.CurrentGameState == GameState.Menu)
		{
			
			
			
		}
	}
    
    private void OnJumpAction(InputAction.CallbackContext context)
    {
	    if (!pc.psm.canAttack || pc.psm.isElementAttacking) return;
	    
	    if (sc.GetCurrentState() is PlayerAttacking) sc.ResumePrevious();
	    
	    if (pc.psm.CanJump)
	    {
		    InputManager.Instance.ReleaseHoldAttacks();
		    pc.psm.LastPressedJumpTimer.Start();
	    }
    }
    
    private void OnSwitchAction(InputAction.CallbackContext context)
    {
	    if (context.phase != InputActionPhase.Performed) return;
	    
	    if (!pc.ps.CanSwapToNonCombat()) return;
	    
	    pc.psm.SwapToNonCombat();
    }
    
    private void OnUltimateAction(InputAction.CallbackContext context)
	{
		if (context.phase != InputActionPhase.Performed) return;
		
		if (!pc.ps.CanUseUltimate()) return;

		pc.psm.SwapToUltimate();
	}
    
    #endregion
    
    #region Movement Methods
    
    private void Run(float lerpAmount)
    {
	    Transform cam = pc.cam.transform;
	    pc.psm.moveDirection = pc.psm.moveInput.x * cam.right.ZeroVector3Axis().normalized + 
	                           pc.psm.moveInput.y * cam.forward.ZeroVector3Axis().normalized;

	    // Project the flat move direction along the ground surface so the player rides over small
	    // bumps and shallow slopes instead of stalling against them. Only applied when grounded.
	    Vector3 slopeDir = pc.psm.moveDirection;
	    if (!pc.psm.isJumping && !pc.psm.isJumpFalling)
	        slopeDir = ProjectOnGround(pc.psm.moveDirection, pc.transform.position, pc.psm);

        Vector3 targetSpeed = slopeDir * (pc.psm.IsSprinting ? pc.psm.playerData.sprintMaxSpeed : pc.psm.playerData.runMaxSpeed);
		targetSpeed = Vector3.Lerp(pc.rb.linearVelocity, targetSpeed, lerpAmount);

		float accelRate;
		if (!pc.psm.LastOnGroundTimer.IsFinished)
			accelRate = (Mathf.Abs(targetSpeed.magnitude) > 0.01f) ? pc.psm.playerData.runAccelAmount : pc.psm.playerData.runDecelAmount;
		else
			accelRate = (Mathf.Abs(targetSpeed.magnitude) > 0.01f) ? pc.psm.playerData.runAccelAmount * pc.psm.playerData.accelInAir : 
															pc.psm.playerData.runDecelAmount * pc.psm.playerData.decelInAir;
		
		if ((pc.psm.isJumping || pc.psm.isJumpFalling) && Mathf.Abs(pc.rb.linearVelocity.y) < pc.psm.playerData.jumpHangSpeedThreshold)
		{
			accelRate *= pc.psm.playerData.jumpHangAccelerationMult;
			targetSpeed *= pc.psm.playerData.jumpHangMaxSpeedMult;
		}
		
		Vector3 speedDiff = targetSpeed - pc.rb.linearVelocity.ZeroVector3Axis();
		Vector3 movementForce = speedDiff * accelRate;

		pc.rb.AddForce(movementForce, ForceMode.Acceleration);
		
		pc.psm.TurnToLook();
	}

    /** <summary>
     * Projects <paramref name="flatDir"/> onto the ground surface directly below the player.
     * Falls back to the original flat direction if no ground is found or the surface is too steep.
     * </summary>
     */
    private static Vector3 ProjectOnGround(Vector3 flatDir, Vector3 playerPos, PlayerStateMachine psm)
    {
	    if (flatDir.sqrMagnitude < 0.0001f) return flatDir;

	    Vector3 origin = playerPos + Vector3.up * psm.slopeProbeOriginOffset;
	    float totalDist = psm.slopeProbeOriginOffset + psm.slopeProbeDistance;

	    if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, totalDist, ~0, QueryTriggerInteraction.Ignore))
		    return flatDir;

	    float angle = Vector3.Angle(hit.normal, Vector3.up);
	    if (angle > psm.slopeProbeMaxAngle) return flatDir;

	    Vector3 projected = Vector3.ProjectOnPlane(flatDir, hit.normal).normalized;
	    return projected.sqrMagnitude > 0.01f ? projected : flatDir;
    }
    
    #endregion
    
    #region Jump Methods

    private void CheckJump()
    {
	    if (pc.psm.CanJump)
	    {
		    pc.psm.isJumpFalling = false;

		    if (pc.psm.IsJumpTriggered)
		    {
			    pc.psm.isJumping = true;
			    pc.psm.LastPressedJumpTimer.Stop();
			    
			    pc.psm.LastDoubleJumpTimer.Start();

			    pc.psm.Jump(pc.psm.playerData.jumpForce, true, WalkingAnimStates.Jumping);
		    }
	    }
    }
    
    #endregion
}
