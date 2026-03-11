using System;
using UnityEngine;

public class EnemyHitbox : MonoBehaviour, IVFXSpawnLocation
{
    public EnemyController ts;
    public bool activeHitbox = false;
    [HideInInspector] public Collider hitboxCollider;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
    }

    public EnemyAttackAIAction GetCurrentAttackAIAction()
    {
        if (ts.esm.currentAction is EnemyAttackAIAction action)
            return action;
        
        return null;
    }

    public Transform GetSpawnTransform()
    {
        return transform;
    }
}
