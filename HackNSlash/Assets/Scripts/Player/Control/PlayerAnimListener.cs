using Extensions.Utils;
using UnityEngine;

public class PlayerAnimListener : MonoBehaviour
{
    [HideInInspector] public PlayerController pc;

    [Header("Slash")] public TransformInfo slashLocalTransform;

    private WeaponType lastWeaponType = WeaponType.SwordLeft;
    
    public void PlaySwordLeftVFX(int vfxIndex = 0)
    {
        lastWeaponType = WeaponType.SwordLeft;
        OnVFXEvents.Instance.SpawnPlayerVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.SwordLeft);
    }
    
    public void PlaySwordRightVFX(int vfxIndex = 0)
    {
        lastWeaponType = WeaponType.SwordRight;
        OnVFXEvents.Instance.SpawnPlayerVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.SwordRight);
    }
    
    public void PlayBothSwordsVFX(int vfxIndex = 0)
    {
        lastWeaponType = WeaponType.SwordLeft;
        OnVFXEvents.Instance.SpawnPlayerVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.SwordLeft);
        OnVFXEvents.Instance.SpawnPlayerVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.SwordRight);
    }
    
    public void PlayKatanaVFX(int vfxIndex = 0)
    {
        lastWeaponType = WeaponType.Katana;
        OnVFXEvents.Instance.SpawnPlayerVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.Katana);
    }
    
    public void PlayHitStop(int index = 0)
    {
        Debug.Log($"number of enemies in hit: {pc.psm.EnemiesInHit.Count}; {pc.psm.PlayHitStopThisAction}");
        if (!pc.psm.PlayHitStopThisAction) return;
        WeaponBody lastWeapon = pc.wc.GetWeapon(lastWeaponType);
        ElementEffect element = pc.psm.currentPlayerAttack != null ? 
            ElementData.GetElementFromAttack(pc.psm.currentPlayerAttack.element, pc) : ElementEffect.None;
        CombatManager.Instance.PlayHitEffects(element, pc, pc.psm.currentPlayerAttack, lastWeapon, true, index);
    }
}
