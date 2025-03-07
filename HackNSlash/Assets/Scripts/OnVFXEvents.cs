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
    FollowGround
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
        OnVFXActionMap.Add(OnVFXActions.FollowGround, FollowGround);
    }
    
    public VFXController InvokeOnVFX(PlayerController pc, TransformInfo start, Attack a, int vfxIndex = 0, WeaponType weaponType = WeaponType.None)
    {
        if (a == null || a.vfxInfos.Length == 0) return null;
        VFXController vfx = InstantiateVFX(pc, start, a, vfxIndex);
        if (vfx == null) return null;
        this.RunSegmentCoroutine(SpawnWithDelay(vfx, a, vfxIndex, weaponType));
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

        TransformInfo realStart = new(start.Position,
            a.vfxInfos[vfxIndex].spawnTransform.Rotation.eulerAngles != Vector3.zero
                ? start.Rotation * a.vfxInfos[vfxIndex].spawnTransform.Rotation
                : start.Rotation,
            start.Scale.ScaledBy(a.vfxInfos[vfxIndex].spawnTransform.Scale));

        VFXSpawnInfo v = a.vfxInfos[vfxIndex];
        GameObject vfx = Instantiate(v.vfxAttack.vfxHitBox, realStart.Position, realStart.Rotation);
        vfx.transform.localScale = realStart.Scale;
        
        if (!vfx.TryGetComponent(out VFXController vc))
        {
            Destroy(vfx);
            return null;
        }

        List<VFXActivator> vfxs = new();

        VFXData[] vfxDatas = v.vfxAttack.vfxDatas;
        foreach (VFXData vfxData in vfxDatas)
        {
            GameObject effect = vfxData.effect == null ? 
                pc.CurrentElementData.GetVFX(vfxData.vfxType) : vfxData.effect;
            if (effect == null) continue;
            
            GameObject vfxInstance = Instantiate(effect, vfx.transform);

            if (!vfxInstance.TryGetComponent(out VFXActivator va))
            {
                Destroy(vfxInstance);
                continue;
            }
            
            vfxs.Add(va);
            va.SetEffectLifetimes(v.duration * vfxData.durationScale);
            
            vfxInstance.transform.localPosition = vfxData.localTransform.Position;
            vfxInstance.transform.localRotation = vfxData.localTransform.Rotation;
            vfxInstance.transform.localScale = vfxData.localTransform.Scale;
        }
        
        vc.InitializeVFX(pc, start, a, v, vfxs.ToArray(), vfxIndex, v.vfxAttack.canCollide);
        return vc;
    }
    
    #region Linear Path
    
    private void LinearPath(VFXController vfx , WeaponType weaponType)
    {
        this.RunSegmentCoroutine(BeginLinearPath(vfx, weaponType));
    }
    
    IEnumerator<float> BeginLinearPath(VFXController vfx, WeaponType weaponType)
    {
        VFXAttack v = vfx.vfxSpawnInfo.vfxAttack;
        
        Vector3 direction = vfx.transform.forward;
        vfx.transform.TweenDistance(direction, vfx.timeAlive * v.vfxSpeed, vfx.timeAlive, Ease.Linear);
        
        vfx.EnableVFX();
        
        yield return Timing.WaitForSeconds(vfx.timeAlive);
    }
    
    #endregion
    
    
    #region Follow Weapon

    [SerializeField] private float slowDownTime = 0.3f;
    
    private void FollowWeapon(VFXController vfx, WeaponType weaponType)
    {
        this.RunSegmentCoroutine(BeginFollowWeapon(vfx, weaponType));
    }
    
    IEnumerator<float> BeginFollowWeapon(VFXController vfx, WeaponType weaponType)
    {
        VFXSpawnInfo v = vfx.vfxSpawnInfo;

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
        this.RunSegmentCoroutine(BeginStationaryPath(vfx, weaponType));
    }
    
    IEnumerator<float> BeginStationaryPath(VFXController vfx, WeaponType weaponType)
    {
        VFXSpawnInfo v = vfx.vfxSpawnInfo;
        
        vfx.EnableVFX();
        
        yield return Timing.WaitForSeconds(v.duration);
    }
    
    #endregion
    
    #region Follow Ground
    
    private void FollowGround(VFXController vfx, WeaponType weaponType)
    {
        this.RunSegmentCoroutine(BeginFollowGround(vfx, weaponType));
    }
    
    IEnumerator<float> BeginFollowGround(VFXController vfx, WeaponType weaponType)
    {
        Vector3 position = vfx.transform.GetGroundedPosition(vfx.player.psm.groundLayer) + Vector3.up * 0.1f;
        vfx.transform.position = position;
        
        float startTime = Time.time;
        float duration = vfx.timeAlive - Time.deltaTime * 2;
        float speed = vfx.vfxSpawnInfo.vfxAttack.vfxSpeed;
        
        vfx.UpdateVFXFloat("Slow", Math.Max((duration - slowDownTime) / duration, 0));
        
        vfx.EnableVFX();
        
        while (Time.time - startTime < duration)
        {
            Vector3 nextPos = vfx.transform.position + speed * vfx.transform.forward * Time.deltaTime;
            Vector3 direction = vfx.transform.forward;
            
            RaycastHit hit;
            if (!Physics.SphereCast(nextPos, 0.1f, Vector3.down, out hit, 100, vfx.player.psm.groundLayer))
            {
                direction = (vfx.transform.forward + Vector3.up).normalized;
            }
            
            vfx.rb.linearVelocity = speed * direction;
            yield return Timing.WaitForOneFrame;
        }
    }
    
    #endregion
    
}
