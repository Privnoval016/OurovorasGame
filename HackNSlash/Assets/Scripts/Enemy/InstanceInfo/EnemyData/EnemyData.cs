using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/EnemyData", order = 1)]
public class EnemyData : ScriptableObject
{
    [Header("Stats")] 
    public float maxHealth;
    
    [Header("Movement")]
    
    public float speed; // Speed of the enemy
    public float runAccelAmount = 7; // Acceleration amount when running
    public float runDecelAmount = 7; // Deceleration amount when stopping

    [Header("Navigation")] 
    public int wanderSearchLength = 5;
    [MinMaxSlider(0, 5)] public Vector2 wanderMoveChance;
    public float targetClosenessDistance = 1.5f; // Distance at which the enemy considers the target close enough to stop moving
    
    [Header("Player Detection")]
    public float playerDetectionRadius = 10f; // Radius within which the enemy can detect the player
    public float playerDetectionAngle = 45f; // Angle within which the enemy can detect the player
}
