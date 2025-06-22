using Extensions.Utils;
using UnityEngine;

public class PlayerAnimListener : MonoBehaviour
{
    [HideInInspector] public PlayerController pc;

    [Header("Slash")] public TransformInfo slashLocalTransform;
    
    public void PlaySwordLeftVFX(int vfxIndex = 0)
    {
        OnVFXEvents.Instance.InvokeOnVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.SwordLeft);
    }
    
    public void PlaySwordRightVFX(int vfxIndex = 0)
    {
        OnVFXEvents.Instance.InvokeOnVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.SwordRight);
    }
    
    public void PlayBothSwordsVFX(int vfxIndex = 0)
    {
        OnVFXEvents.Instance.InvokeOnVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.SwordLeft);
        OnVFXEvents.Instance.InvokeOnVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.SwordRight);
    }
    
    public void PlayKatanaVFX(int vfxIndex = 0)
    {
        OnVFXEvents.Instance.InvokeOnVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.Katana);
    }
}
