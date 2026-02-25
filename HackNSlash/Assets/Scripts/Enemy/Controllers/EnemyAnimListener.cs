using UnityEngine;

public class EnemyAnimListener : MonoBehaviour
{
    [HideInInspector] public EnemyController ts;
    
    public void ActivateHitbox(int index = 0)
    {
        ts.esm.HitboxActivate(index);
    }
    
    public void DeactivateHitbox(int index = 0)
    {
        ts.esm.HitboxDeactivate(index);
    }
    
    public void ActivateAllHitboxes()
    {
        ts.esm.AllHitboxesActivate();
    }
    
    public void DeactivateAllHitboxes()
    {
        ts.esm.AllHitboxesDeactivate();
    }
    
    public void ActivateParryWindow()
    {
        ts.esm.ParryWindowActivate();
    }
    
    public void DeactivateParryWindow()
    {
        ts.esm.ParryWindowDeactivate();
    }
}
