using System;
using System.Collections.Generic;
using UnityEngine;
using ExtensionUtils;
using MEC;
using PrimeTween;
using UnityEngine.VFX;

public enum OnVFXActions
{
    LinearPath,
    HomingPath,
    CircularPath,
    StationaryPath,
    FollowWeapon,
}


public class OnVFXEvents : MonoBehaviour
{
    public static OnVFXEvents Instance { get; private set; }
    
    public static Dictionary<OnVFXActions, Action<VFXController, WeaponType>> OnVFXActionMap;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        AddOnVFXMethods();

    }
    
    private void AddOnVFXMethods()
    {
        if (OnVFXActionMap != null) return;
        
        OnVFXActionMap = new();
        
        OnVFXActionMap.Add(OnVFXActions.LinearPath, LinearPath);
        OnVFXActionMap.Add(OnVFXActions.FollowWeapon, FollowWeapon);
        OnVFXActionMap.Add(OnVFXActions.StationaryPath, StationaryPath);
    }
    
    public VFXController InvokeOnVFX(PlayerController pc, TransformInfo start, Attack a, int vfxIndex = 0, WeaponType weaponType = WeaponType.None)
    {
        if (a == null || a.vfxInfos.Length == 0) return null;
        VFXController vfx = InstantiateVFX(pc, start, a, vfxIndex);
        if (vfx == null) return null;
        Timing.RunCoroutine(SpawnWithDelay(vfx, a, vfxIndex, weaponType));
        return vfx;
    }
    
    IEnumerator<float> SpawnWithDelay(VFXController vfx, Attack a, int vfxIndex = 0, WeaponType weaponType = WeaponType.None)
    {
        yield return Timing.WaitForSeconds(a.vfxInfos[vfxIndex].delay);
        OnVFXActionMap[a.vfxInfos[vfxIndex].vfxAttack.vfxAction].Invoke(vfx, weaponType);
    }
    
    private VFXController InstantiateVFX(PlayerController pc, TransformInfo start, Attack a, int vfxIndex = 0)
    {
        if (a.vfxInfos.Length <= vfxIndex) return null;

        VFXInfo v = a.vfxInfos[vfxIndex];
        GameObject vfx = Instantiate(v.vfxAttack.vfxHitBox, start.Position, start.Rotation);
        
        if (!vfx.TryGetComponent(out VFXController vc))
        {
            Destroy(vfx);
            return null;
        }
        
        List<VisualEffect> vfxs = new List<VisualEffect>();

        foreach (VFXData vfxData in v.vfxAttack.vfxDatas)
        {
            GameObject effect = vfxData.effect == null ? 
                pc.CurrentElementData.GetVFX(vfxData.vfxType) : vfxData.effect;
            if (effect == null) continue;
            
            GameObject vfxInstance = Instantiate(effect, vfx.transform);
            if (!vfxInstance.TryGetComponentInChildren(out VisualEffect vfxInstanceVFX))
            {
                Destroy(vfxInstance);
                continue;
            }
            
            vfxInstanceVFX.SafeSetFloat("Lifetime", v.duration * vfxData.durationScale);
            
            vfxInstance.transform.localPosition = vfxData.localTransform.Position;
            vfxInstance.transform.localRotation = vfxData.localTransform.Rotation;
            vc.SetChildScale(vfxInstance, vfxData.localTransform.Scale);
            
            vfxs.Add(vfxInstanceVFX);
            
            vfxInstanceVFX.gameObject.SetActive(false);
        }
        
        vc.InitializeVFX(pc, start, a, v, vfxs.ToArray(), vfxIndex, v.vfxAttack.canCollide);
        return vc;
    }
    
    #region Linear Path
    
    private void LinearPath(VFXController vfx , WeaponType weaponType)
    {
        Timing.RunCoroutine(BeginLinearPath(vfx, weaponType));
    }
    
    IEnumerator<float> BeginLinearPath(VFXController vfx, WeaponType weaponType)
    {
        VFXAttack v = vfx.vfxInfo.vfxAttack;
        
        Vector3 direction = vfx.transform.forward;
        print(direction + " " + vfx.transform.rotation.eulerAngles);
        vfx.transform.TweenDistance(direction, vfx.timeAlive * v.vfxSpeed, vfx.timeAlive, Ease.Linear);
        
        vfx.EnableVFX();
        
        yield return Timing.WaitForSeconds(vfx.timeAlive);
    }
    
    #endregion
    
    
    #region Follow Weapon

    [SerializeField] private float slowDownTime = 0.3f;
    
    private void FollowWeapon(VFXController vfx, WeaponType weaponType)
    {
        Timing.RunCoroutine(BeginFollowWeapon(vfx, weaponType));
    }
    
    IEnumerator<float> BeginFollowWeapon(VFXController vfx, WeaponType weaponType)
    {
        VFXInfo v = vfx.vfxInfo;

        vfx.UpdateVFXFloat("Slow", Math.Max((v.duration - slowDownTime) / v.duration, 0));
        
        vfx.ChangeParent(vfx.player.wc.GetWeapon(weaponType).transform);
        vfx.transform.localPosition = Vector3.zero;
        vfx.transform.localRotation = Quaternion.identity;
        
        vfx.EnableVFX();
        
        yield return Timing.WaitForSeconds(vfx.timeAlive);
    }
    
    
    #endregion
    
    
    #region Stationary Path
    
    private void StationaryPath(VFXController vfx, WeaponType weaponType)
    {
        Timing.RunCoroutine(BeginStationaryPath(vfx, weaponType));
    }
    
    IEnumerator<float> BeginStationaryPath(VFXController vfx, WeaponType weaponType)
    {
        VFXInfo v = vfx.vfxInfo;
        
        vfx.EnableVFX();
        
        yield return Timing.WaitForSeconds(v.duration);
    }
    
    #endregion
    
}
