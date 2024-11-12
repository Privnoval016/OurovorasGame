using UnityEngine;

public class EnemyController : IDamageable
{
    public EnemyGravity gravityData;
    
    private float gravityScale;
    
    private Rigidbody rb;
    
    public bool IsGrounded =>
        Physics.CheckBox(groundCheckPoint.position, groundCheckSize, Quaternion.identity, groundLayer);
    
    #region CHECK PARAMETERS
   
    [Header("Checks")] 
    [SerializeField] public Transform groundCheckPoint;
    [SerializeField] public Vector3 groundCheckSize = new Vector3(0.49f, 0.3f, 0.49f);
    
    [SerializeField] private LayerMask groundLayer;
    
    #endregion
    
    public override void OnStart()
    {
        base.OnStart();
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        CalculateGravity();
    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        ApplyGravity();
    }
    
    #region Gravity Methods

    public void CalculateGravity()
    {
        if (!IsGrounded && Mathf.Abs(rb.linearVelocity.y) < gravityData.jumpHangTimeThreshold)
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
}
