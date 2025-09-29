using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

[System.Serializable]
public class MarkedHitAction : IHitAction
{
    public enum MarkedType // replace this with strategy pattern when adding hitheal
    {
        Explosion,
        HitHeal
    }
    
    [Header("Marked Parameters")]
    public MarkedType markedType = MarkedType.Explosion;
    
    public override void Execute(OnHitEvents onHitEvents, PhysicsEnemy enemy, Attack attack, Transform attackerTransform)
    {
        base.Execute(onHitEvents, enemy, attack, attackerTransform);
        
        Marked();
    }
    
    private void Marked()
    {
        var vfx = OnVFXEvents.Instance.SpawnPlayerVFX(pc, a, 1, 
            new TransformInfo(ec.TargetedPosition(), Quaternion.identity, Vector3.one));
        
        
        vfx.transform.SetParent(ec.transform);

        vfx.activeHitbox = false;

        switch (markedType)
        {
            case MarkedType.Explosion:
                vfx.RunSegmentCoroutine(ActivateDelayedHit(vfx).CancelWith(vfx), vfx.GetInstanceID().ToString());
                break;
            case MarkedType.HitHeal:
                break;
        }
        
    }
    
    IEnumerator<float> ActivateDelayedHit(VFXController vfx)
    {
        yield return Timing.WaitForSeconds(vfx.timeActive);
        
        vfx.activeHitbox = true;
        
        yield return Timing.WaitForSeconds(0.3f);
        
        if (vfx != null)
            vfx.activeHitbox = false;
    }
}
