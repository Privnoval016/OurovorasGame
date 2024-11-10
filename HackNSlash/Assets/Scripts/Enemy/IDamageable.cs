using UnityEngine;

public interface IDamageable
{
    bool TookDamageThisAction { get; set; }

    public void OnHit(Attack a);
}
