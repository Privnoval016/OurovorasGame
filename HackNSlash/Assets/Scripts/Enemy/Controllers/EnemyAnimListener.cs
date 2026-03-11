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
    
    
    public void PlayVFX(int hitboxIndex = 0, float vfxIndex = 0)
    {
        int vfxIndexInt = Mathf.RoundToInt(vfxIndex); // using float so we can set it alongside hitbox index in the animation event
        
        // get vfxinfos from the current attack's vfx profile, using hitbox index and vfx index
        EnemyAIActionBase currentAction = ts.esm.currentAction;
        if (currentAction is not EnemyAttackAIAction attackAction)
        {
            Debug.LogWarning($"Trying to play VFX for non-attack action {currentAction?.name}");
            return;
        }
        
        var vfxInfos = attackAction?.attack?.vfxInfos;
        if (vfxInfos == null || vfxIndexInt >= vfxInfos.Length)
        {
            Debug.LogWarning($"Trying to play VFX with invalid hitbox index {hitboxIndex} or vfx index {vfxIndexInt}");
            return;
        }
        
        
        Services.Get<VFXSystem>().PlayAnimationEventVFX(ts, vfxInfos, hitboxIndex, vfxIndexInt);
    }
}
