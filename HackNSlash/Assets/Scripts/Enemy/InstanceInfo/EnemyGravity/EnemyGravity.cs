using UnityEngine;
using UnityEngine.Serialization;

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
    [FormerlySerializedAs("jumpHangTimeThreshold")] public float jumpHangSpeedThreshold; //Speeds (close to 0) where the player will experience extra "jump hang". The player's velocity.y is closest to 0 at the jump's apex (think of the gradient of a parabola or quadratic function)

    public float onHitHangTime = 0.2f;

    [Header("Collision")] 
    public float colliderBuffer = 0.3f;
    
    private void RecalculateScale()
    {
        // gravityStrength is a positive designer-set downward force magnitude.
        // globalGravity is negative (-9.81), so we divide by its absolute value to
        // produce a positive gravityScale — matching PlayerData's convention.
        // ApplyGravity does: globalGravity * gravityScale * Vector3.up, which resolves
        // to a downward force when globalGravity < 0 and gravityScale > 0.
        // Guard: if gravityStrength was reset to 0 (e.g. after a crash), fall back to
        // 1.0 so the enemy always has gravity rather than floating indefinitely.
        float absGravity = Mathf.Abs(Physics.gravity.y);
        if (absGravity < 0.001f) absGravity = 9.81f;
        gravityScale = gravityStrength > 0f ? gravityStrength / absGravity : 1f;
    }

    private void OnEnable() => RecalculateScale();

    //Unity Callback, called when the inspector updates
    private void OnValidate() => RecalculateScale();
}
