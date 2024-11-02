using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "ScriptableObjects/PlayerData")]
public class PlayerData : ScriptableObject
{
	[Header("Gravity")]
	[HideInInspector] public float gravityStrength; //Downwards force (gravity) needed for the desired jumpHeight and jumpTimeToApex.
	[HideInInspector] public float gravityScale; //Strength of the player's gravity as a multiplier of gravity
	[Space(5)]
	[Tooltip("Multiplier to the player's gravityScale when falling.")] public float fallGravityMult;
	[Tooltip("Maximum fall speed (terminal velocity) of the player when falling.")] public float maxFallSpeed;
	
	[Space(20)]

	[Header("Run")]
	[Tooltip("Target speed we want the player to reach.")] public float runMaxSpeed;
	
	[Tooltip("Target sprinting speed we want the player to reach.")] public float sprintMaxSpeed;
	[Tooltip("Time until beginning to sprint.")] public float sprintBuildupLength;
	
	[Tooltip("The speed at which our player accelerates to max speed, can be set to runMaxSpeed for instant acceleration down to 0 for none at all")]
	public float runAcceleration;
	
	[HideInInspector] public float runAccelAmount; //The actual force (multiplied with speedDiff) applied to the player.
	
	[FormerlySerializedAs("runDecceleration")] [Tooltip("The speed at which our player decelerates from their current speed, can be set to runMaxSpeed for instant deceleration down to 0 for none at all")] 
	public float runDeceleration;
	
	[FormerlySerializedAs("runDeccelAmount")] [HideInInspector] public float runDecelAmount; //Actual force (multiplied with speedDiff) applied to the player .
	[Space(5)]
	
	[Range(0f, 1)] public float accelInAir; //Multipliers applied to acceleration rate when airborne.
	[FormerlySerializedAs("deccelInAir")] [Range(0f, 1)] public float decelInAir;
	[Space(5)]
	public bool doConserveMomentum = true;

	[Space(20)]

	[Header("Jump")]
	public float jumpHeight; //Height of the player's jump
	public float jumpTimeToApex; //Time between applying the jump force and reaching the desired jump height. These values also control the player's gravity and jump force.
	[HideInInspector] public float jumpForce; //The actual force applied (upwards) to the player when they jump.

	[Header("Double Jump")]
	public float doubleJumpTimeToApex; //Time between applying the double jump force and reaching the desired jump height. These values also control the player's gravity and jump force.
	[HideInInspector] public float doubleJumpForce; //The actual force applied (upwards) to the player when they double jump.
	[FormerlySerializedAs("doubleJumpCooldown")] public float doubleJumpWaitDuration; //Time between double jumps
	
	[Header("Both Jumps")]
	[Range(0f, 1)] public float jumpHangGravityMult; //Reduces gravity while close to the apex (desired max height) of the jump
	public float jumpHangTimeThreshold; //Speeds (close to 0) where the player will experience extra "jump hang". The player's velocity.y is closest to 0 at the jump's apex (think of the gradient of a parabola or quadratic function)
	[Space(0.5f)]
	public float jumpHangAccelerationMult; 
	public float jumpHangMaxSpeedMult; 			
	
	[Header("Dash")]
	
	public float dashSpeed;
	

    [Header("Assists")]
	[Range(0.01f, 0.5f)] public float coyoteTime; //Grace period after falling off a platform, where you can still jump
	[Range(0.01f, 0.5f)] public float jumpInputBufferTime; //Grace period after pressing jump where a jump will be automatically performed once the requirements (eg. being grounded) are met.
	
	[Header("Lock On")]
	public float lockOnRange;

	[Header("Attacks")] 
	[Tooltip("Time before you break combo")] public float midairAttackGravityMult;

	//Unity Callback, called when the inspector updates
    private void OnValidate()
    {
		//Calculate gravity strength using the formula (gravity = 2 * jumpHeight / timeToJumpApex^2) 
		gravityStrength = -(2 * jumpHeight) / (jumpTimeToApex * jumpTimeToApex);
		
		//Calculate the rigidbody's gravity scale (ie: gravity strength relative to unity's gravity value, see project settings/Physics2D)
		gravityScale = gravityStrength / Physics2D.gravity.y;

		//Calculate are run acceleration & deceleration forces using formula: amount = ((1 / Time.fixedDeltaTime) * acceleration) / runMaxSpeed
		runAccelAmount = (50 * runAcceleration) / runMaxSpeed;
		runDecelAmount = (50 * runDeceleration) / runMaxSpeed;

		//Calculate jumpForce using the formula (initialJumpVelocity = gravity * timeToJumpApex)
		jumpForce = Mathf.Abs(gravityStrength) * jumpTimeToApex;
		
		doubleJumpForce = Mathf.Abs(gravityStrength) * doubleJumpTimeToApex;

		#region Variable Ranges
		runAcceleration = Mathf.Clamp(runAcceleration, 0.01f, runMaxSpeed);
		runDeceleration = Mathf.Clamp(runDeceleration, 0.01f, runMaxSpeed);
		#endregion
	}
}