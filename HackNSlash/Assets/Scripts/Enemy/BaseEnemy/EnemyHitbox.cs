using System;
using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    public EnemyController ts;
    public bool activeHitbox = false;
    [HideInInspector] public Collider hitboxCollider;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
    }

    public EnemyAttackInfo GetCurrentAttackInfo()
    {
        return ts.currentAttackInfo;
    }
}
