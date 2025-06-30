using UnityEngine;

[CreateAssetMenu(menuName = "Events/OnHitParameters")]
public class OnHitParameters : ScriptableObject
{
    [Header("Midair Knockback Attack")]
    public float midairKnockbackTime = 0.1f;

    [Header("Launch Up Attack")]
    public float launchUpTime = 0.3f;

    public float launchUpHeight = 15f;

    [Header("Follow Player Velocity Attack")]
    public float followVelocityTime = 0.75f;
    public float followVelocityMult = 1.2f;

    [Header("Launch Down Attack")]
    public float launchDownVelocityMult = 1.2f;

    public float bounceCheckTime = 0.5f;
    public float bounceTime = 0.3f;
    public float bounceHeight = 5f;

    [Header("Cross Slash")] 
    public float crossSlashHitboxDelay = 0.4f;
    public float crossSlashHitStrength = 10f;
    
    [Header("Grapple Attack")] 
    public float grappleSpeed = 150f;
    
}
