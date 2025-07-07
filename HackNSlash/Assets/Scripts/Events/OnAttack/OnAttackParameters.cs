using UnityEngine;

[CreateAssetMenu(menuName = "Events/OnAttackParameters")]
public class OnAttackParameters : ScriptableObject
{
    public MovingStates movingState = MovingStates.NonCombat;
    
    [Header("Blade Beam")]
    public float bladeBeamHoldTime = 0.2f;
    public float bladeBeamInterval = 0.2f;
    public float crossSlashDuration = 0.3f;
    
    [Header("Grapple")]
    public float grappleMaxTime = 0.1f;
    
    [Header("Air Dash")]
    public float airDashSpeed = 80f;
    public float maxAirDashDistance = 20f;
    public bool finalAirSlash = false;
    
    [Header("Dash Attack")]
    public float groundDashDistance = 25f;
    public float groundDashTime = 0.2f;
    public float groundDashTimeThreshold = 0.4f;
    
    [Header("Launch Up Attack")] 
    public float launchUpHoldTime = 0.25f;
    public float launchUpTime = 0.3f;
    public float launchUpHeight = 15f;
    
    [Header("Plunge Attack")]
    public float plungeSpeed = 75;

    public float minPlungeTime = 0.02f;
    
    [Header("Enemy Step")] 
    public float enemyStepPushBack = 1;
    public float enemyStepTime = 0.3f;
    public float enemyStepHeight = 5f;
    
    [Header("Dodge")]
    public float dodgeDistance = 8f;
    public float dodgeTime = 0.2f;
    
    [Header("Mash Attack")] 
    public float mashInterval = 0.4f;
    public float mashDuration = 1.5f;
    
    [Header("Imbue Element")]
    public float imbueElementDuration = 5f;

    [Header("Finisher")] 
    public float finisherTimeScale = 0.7f;
}
