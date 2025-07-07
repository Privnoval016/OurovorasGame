using UnityEngine;

[CreateAssetMenu(fileName = "StatData", menuName = "Player/StatData", order = 1)]
public class StatData : ScriptableObject
{
    [Header("Charge Settings")]
    public float chargeRestoreRate = 10f;

    public float chargeRestoreTime = 3f;
    
    [Header("Ultimate Settings")]
    public float maxUltimateCharge = 500f;
    public float minActivationCharge = 250f;
    
    public float ultimateDrainRate = 20f;
    
    [Space(10)]
    public float maxFinisherCharge = 250f;
    public float finisherDistance = 5f;
}
