using Extensions.EventBus;
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
        Services.Get<VFXSystem>().SpawnPlayerVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.SwordLeft);
        PlayAttackSFXEffect(WeaponType.SwordLeft);
    }
    
    public void PlaySwordRightVFX(int vfxIndex = 0)
    {
        lastWeaponType = WeaponType.SwordRight;
        Services.Get<VFXSystem>().SpawnPlayerVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.SwordRight);
        PlayAttackSFXEffect(WeaponType.SwordRight);
    }
    
    public void PlayBothSwordsVFX(int vfxIndex = 0)
    {
        lastWeaponType = WeaponType.SwordLeft;
        Services.Get<VFXSystem>().SpawnPlayerVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.SwordLeft);
        Services.Get<VFXSystem>().SpawnPlayerVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.SwordRight);
        
        PlayAttackSFXEffect(WeaponType.SwordLeft);
        PlayAttackSFXEffect(WeaponType.SwordRight);
    }
    
    public void PlayKatanaVFX(int vfxIndex = 0)
    {
        lastWeaponType = WeaponType.Katana;
        Services.Get<VFXSystem>().SpawnPlayerVFX(pc, pc.psm.currentPlayerAttack, vfxIndex, new TransformInfo(), WeaponType.Katana);
    }
    
    public void PlayHitStop(int index = 0)
    {
        Debug.Log($"number of enemies in hit: {pc.psm.EnemiesInHit.Count}; {pc.psm.PlayHitStopThisAction}");
        if (!pc.psm.PlayHitStopThisAction) return;
        WeaponBody lastWeapon = pc.wc.GetWeapon(lastWeaponType);
        ElementEffect element = pc.psm.currentPlayerAttack != null ? 
            ElementData.GetElementFromAttack(pc.psm.currentPlayerAttack.element, pc) : ElementEffect.None;
        Services.Get<CombatSystem>().PlayHitEffects(element, pc, pc.psm.currentPlayerAttack, lastWeapon, true, index);
    }
    
    private void PlayAttackSFXEffect(WeaponType followedWeaponType)
    {
        Transform attachTo = pc.wc.GetWeapon(followedWeaponType).transform;
        
        if (pc.psm.currentPlayerAttack == null || pc.psm.currentPlayerAttack.audioProfiles == null) return;
        
        foreach (var profile in pc.psm.currentPlayerAttack.audioProfiles)
        {
            if (profile.playTime != AudioProfile.PlayTime.Instant) continue;
            
            EventBus<PlaySFXEvent>.Raise(new PlaySFXEvent(profile.audioEvent, attachTo, profile.parameters));
        }
    }
    
    public void PlayFootstepSound()
    {
        var effect = AudioLookupAtlas.Instance?.playerFootstepSound;
        
        if (effect == null)
        {
            return;
        }
        
        EventBus<PlaySFXEvent>.Raise(new PlaySFXEvent(effect, pc.psm.groundCheckPoint, null, true));
    }
    
    
    public void PlayVFX(int hitboxIndex = 0, float vfxIndex = 0)
    {
        int vfxIndexInt = Mathf.RoundToInt(vfxIndex); // using float so we can set it alongside hitbox index in the animation event
        
        // get vfxinfos from the current attack's vfx profile, using hitbox index and vfx index
        PlayerAttack currentAttack = pc.psm.currentPlayerAttack;
        
        var vfxInfos = currentAttack?.vfxInfos;
        if (vfxInfos == null || vfxIndexInt >= vfxInfos.Length)
        {
            Debug.LogWarning($"Trying to play VFX with invalid hitbox index {hitboxIndex} or vfx index {vfxIndexInt}");
            return;
        }
        
        
        Services.Get<VFXSystem>().PlayAnimationEventVFX(pc, vfxInfos, hitboxIndex, vfxIndexInt);
    }
    
}
