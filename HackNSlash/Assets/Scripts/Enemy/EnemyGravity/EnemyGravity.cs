using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/EnemyGravity")]
public class EnemyGravity : ScriptableObject
{
    [Header("Gravity")]
    public float gravityStrength; //Downwards force (gravity) needed for the desired jumpHeight and jumpTimeToApex.
    [HideInInspector] public float gravityScale; //Strength of the player's gravity as a multiplier of gravity
    [Space(5)]
    [Tooltip("Multiplier to the player's gravityScale when falling.")] public float fallGravityMult;
    [Tooltip("Maximum fall speed (terminal velocity) of the player when falling.")] public float maxFallSpeed;
    
    [Header("Jump Apex Hang Time")]
    
    [Range(0f, 1)] public float jumpHangGravityMult; //Reduces gravity while close to the apex (desired max height) of the jump
    public float jumpHangTimeThreshold; //Speeds (close to 0) where the player will experience extra "jump hang". The player's velocity.y is closest to 0 at the jump's apex (think of the gradient of a parabola or quadratic function)
    [Space(0.5f)]
    public float jumpHangAccelerationMult; 
    public float jumpHangMaxSpeedMult; 	
    
    //Unity Callback, called when the inspector updates
    private void OnValidate()
    {
        //Calculate the rigidbody's gravity scale (ie: gravity strength relative to unity's gravity value, see project settings/Physics)
        gravityScale = gravityStrength / Physics.gravity.y;
    }
}
