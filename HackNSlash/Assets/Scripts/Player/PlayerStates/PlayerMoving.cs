using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class PlayerMoving : PlayerState
{
    #region Variables
    
    #endregion
    
    
    #region State Methods
    
    public override void OnEnter()
    {
	    
    }

    public override void OnFixedUpdate()
    {
        
    }

    public override void OnExit()
    {
        
    }
    
    #endregion
    
    #region Movement Methods
    
    private void Run(float lerpAmount)
	{
		/* Vector2 targetSpeed = InputManager.Instance.movement.ReadValue<Vector2>() * sc.playerData.runMaxSpeed;
		targetSpeed = Vector2.Lerp(sc.rb.velocity, targetSpeed, lerpAmount);


		float accelRate;

		//Gets an acceleration value based on if we are accelerating (includes turning) 
		//or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
		if (LastOnGroundTime > 0)
			accelRate = (Mathf.Abs(targetSpeed.magnitude) > 0.01f) ? sc.playerData.runAccelAmount : sc.playerData.runDeccelAmount;
		else
			accelRate = (Mathf.Abs(targetSpeed.magnitude) > 0.01f) ? sc.playerData.runAccelAmount * sc.playerData.accelInAir : 
															sc.playerData.runDeccelAmount * sc.playerData.deccelInAir;
		
		
		if ((IsJumping || isJumpFalling) && Mathf.Abs(sc.rb.velocity.y) < sc.playerData.jumpHangTimeThreshold)
		{
			accelRate *= sc.playerData.jumpHangAccelerationMult;
			targetSpeed *= sc.playerData.jumpHangMaxSpeedMult;
		}
		
		Vector2 speedDiff = targetSpeed - new Vector2(sc.rb.velocity.x, sc.rb.velocity.y); */
	}
    
    #endregion
}
