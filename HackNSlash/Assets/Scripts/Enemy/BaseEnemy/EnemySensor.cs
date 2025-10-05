
using Extensions.UtilityAI;
using UnityEngine;

public class EnemySensor : Sensor<EnemyAIContextKey>
{
    protected override bool HasDetectionTag(EnemyAIContextKey aiContextKey, Collider other)
    {
        return aiContextKey switch
        {
            EnemyAIContextKey.Player => other.TryGetComponent(out PlayerController _),
            EnemyAIContextKey.OtherEnemy => other.TryGetComponent(out EnemyController _),
            EnemyAIContextKey.Self => other.gameObject == gameObject,
            _ => false
        };
    }
}