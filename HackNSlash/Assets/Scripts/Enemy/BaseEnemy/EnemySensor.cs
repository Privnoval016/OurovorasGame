using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Detects players, other enemies, and self within a radius.
 * Maps each <see cref="ContextKey{Transform}"/> from <see cref="EnemyContextKeys"/>
 * to a concrete component check — no strings involved.
 * </summary>
 */
public class EnemySensor : Sensor
{
    protected override bool IsValidTarget(Collider other)
        => other.TryGetComponent(out PlayerController _)
        || other.TryGetComponent(out EnemyController _);

    protected override bool HasDetectionTag(ContextKey<Transform> key, Collider other)
    {
        if (key == EnemyContextKeys.Player)   return other.TryGetComponent(out PlayerController _);
        if (key == EnemyContextKeys.OtherEnemy) return other.TryGetComponent(out EnemyController _) && other.gameObject != gameObject;
        if (key == EnemyContextKeys.Self)       return other.gameObject == gameObject;
        return false;
    }
}