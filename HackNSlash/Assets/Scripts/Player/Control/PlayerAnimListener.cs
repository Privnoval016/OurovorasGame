using Extensions.Utils;
using UnityEngine;

public class PlayerAnimListener : MonoBehaviour
{
    [HideInInspector] public PlayerController pc;

    [Header("Slash")] public TransformInfo slashLocalTransform;
    
    public void PlaySwordLeftVFX(int vfxIndex = 0)
    {
        Debug.Log("Sword Left Attack");
        OnVFXEvents.Instance.InvokeOnVFX(pc, slashLocalTransform, pc.psm.currentPlayerAttack, vfxIndex, WeaponType.SwordLeft);
    }
    
    public void PlaySwordRightVFX(int vfxIndex = 0)
    {
        Debug.Log("Sword Right Attack");
        OnVFXEvents.Instance.InvokeOnVFX(pc, slashLocalTransform, pc.psm.currentPlayerAttack, vfxIndex, WeaponType.SwordRight);
    }
    
    public void PlayBothSwordsVFX(int vfxIndex = 0)
    {
        Debug.Log("Both Swords Attack");
        OnVFXEvents.Instance.InvokeOnVFX(pc, slashLocalTransform, pc.psm.currentPlayerAttack, vfxIndex, WeaponType.SwordLeft);
        OnVFXEvents.Instance.InvokeOnVFX(pc, slashLocalTransform, pc.psm.currentPlayerAttack, vfxIndex, WeaponType.SwordRight);
    }
    
    public void PlayKatanaVFX(int vfxIndex = 0, int temp = 0)
    {
        Debug.Log("Katana Attack");
    }
}
