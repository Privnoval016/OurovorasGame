using UnityEngine;

[CreateAssetMenu(fileName = "StatData", menuName = "Player/StatData", order = 1)]
public class StatData : ScriptableObject
{
    [Header("Charge Settings")]
    public float chargeRestoreRate = 10f;

    public float chargeRestoreTime = 3f;
}
