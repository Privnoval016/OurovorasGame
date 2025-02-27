using System;
using System.Collections.Generic;
using System.Linq;
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
    
    public void EndObjectCoroutines()
    {
        Timing.KillCoroutines(gameObject);
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
        
        Dictionary<VisualEffect, int> vfxs = new();

        VFXData[] vfxDatas = v.vfxAttack.vfxDatas;
        for (int i = 0; i < vfxDatas.Length; i++)
        {
            GameObject effect = vfxDatas[i].effect == null ? 
                pc.CurrentElementData.GetVFX(vfxDatas[i].vfxType) : vfxDatas[i].effect;
            if (effect == null) continue;
            
            GameObject vfxInstance = Instantiate(effect, vfx.transform);
            VisualEffect[] vfxInstanceChildren = vfxInstance.GetComponentsInChildren<VisualEffect>();
            if (vfxInstanceChildren.Length == 0)
            {
                Destroy(vfxInstance);
                continue;
            }

            foreach (VisualEffect vfxInstanceVFX in vfxInstanceChildren)
            {
                vfxInstanceVFX.SafeSetFloat("Lifetime", v.duration * vfxDatas[i].durationScale);
                vfxInstanceVFX.gameObject.SetActive(false);
                vfxs.Add(vfxInstanceVFX, i);
            }
            
            vfxInstance.transform.localPosition = vfxDatas[i].localTransform.Position;
            vfxInstance.transform.localRotation = vfxDatas[i].localTransform.Rotation;
            vc.SetChildScale(vfxInstance, vfxDatas[i].localTransform.Scale);
        }
        
        vc.InitializeVFX(pc, start, a, v, vfxs, vfxIndex, v.vfxAttack.canCollide);
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
