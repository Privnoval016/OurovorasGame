using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Extensions.Utils;
using MEC;
using PrimeTween;
using UnityEngine.VFX;

public enum OnVFXActions
{
    LinearPath,
    HomingPath,
    RotatingPath,
    StationaryPath,
    FollowWeapon,
    FollowGround
}


public class OnVFXEvents : Singleton<OnVFXEvents>
{
    public static Dictionary<OnVFXActions, Action<VFXController, WeaponType>> OnVFXActionMap;
    
    public OnVFXParameters parameters;
    
    private List<VFXController> activeVFX = new();
    
    public Dictionary<VFXAttack, List<VFXController>> ActiveVFXCount = new();
    
    protected override void Awake()
    {
        base.Awake();
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
        OnVFXActionMap.Add(OnVFXActions.RotatingPath, RotatingPath);
    }
    
    #region VFX Invocation
    
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
        
        
        Quaternion rotation = a.vfxInfos[vfxIndex].spawnTransform.Rotation.eulerAngles != Vector3.zero
            ? start.Rotation * a.vfxInfos[vfxIndex].spawnTransform.Rotation
            : start.Rotation;

        TransformInfo realStart = new(start.Position, rotation,
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
                GameManager.GetElementData(ElementData.GetElementFromAttack(a, pc)).GetVFX(vfxData.vfxType) 
                : vfxData.effect;
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

        vc.InitializeVFX(pc, realStart, a, v, vfxs.ToArray(), vfxIndex, v.vfxAttack.canCollide);
        
        return vc;
    }

    public void OnVFXInitialize(VFXController vc)
    {
        activeVFX.Add(vc);
        
        if (ActiveVFXCount.ContainsKey(vc.vfxSpawnInfo.vfxAttack))
        {
            ActiveVFXCount[vc.vfxSpawnInfo.vfxAttack].Add(vc);
        }
        else
        {
            ActiveVFXCount.Add(vc.vfxSpawnInfo.vfxAttack, new List<VFXController> { vc });
        }
        
        if (vc.vfxSpawnInfo.vfxAttack.maxVFXAlive != 0)
        {
            int currentCount = ActiveVFXCount.ContainsKey(vc.vfxSpawnInfo.vfxAttack) 
                ? ActiveVFXCount[vc.vfxSpawnInfo.vfxAttack].Count 
                : 0;

            if (currentCount > vc.vfxSpawnInfo.vfxAttack.maxVFXAlive)
            {
                var oldest = ActiveVFXCount[vc.vfxSpawnInfo.vfxAttack][0];
                if (oldest != null) oldest.DestroyVFX();
            }
        }
    }

    public void OnVFXDestroyed(VFXController vfx)
    {
        if (ActiveVFXCount.ContainsKey(vfx.vfxSpawnInfo.vfxAttack))
        {
            ActiveVFXCount[vfx.vfxSpawnInfo.vfxAttack].Remove(vfx);
        }
        
        activeVFX.Remove(vfx);
    }

    public void OnVFXPaused(bool paused)
    {
        foreach (VFXController vfx in activeVFX)
        {
            if (paused)
            {
                // Pause the VFX
            }
            else
            {
                // Resume the VFX
            }
        }
    }
    
    #endregion
    
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
    
    private void FollowWeapon(VFXController vfx, WeaponType weaponType)
    {
        this.RunSegmentCoroutine(BeginFollowWeapon(vfx, weaponType));
    }
    
    IEnumerator<float> BeginFollowWeapon(VFXController vfx, WeaponType weaponType)
    {
        VFXSpawnInfo v = vfx.vfxSpawnInfo;

        vfx.UpdateVFXFloat("Slow", Math.Max((v.duration - parameters.slowDownTime) / v.duration, 0));
        
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
        this.RunSegmentCoroutine(BeginFollowGround(vfx, weaponType).CancelWith(vfx));
    }
    
    IEnumerator<float> BeginFollowGround(VFXController vfx, WeaponType weaponType)
    {
        Vector3 position = vfx.transform.GetGroundedPosition(vfx.player.psm.groundLayer);
        vfx.transform.position = position;
        
        float startTime = Time.time;
        float duration = vfx.timeAlive - Time.deltaTime * 2;
        float speed = vfx.vfxSpawnInfo.vfxAttack.vfxSpeed;
        
        vfx.UpdateVFXFloat("Slow", Math.Max((duration - parameters.slowDownTime) / duration, 0));
        
        vfx.EnableVFX();
        
        while (Time.time - startTime < duration)
        {
            Vector3 direction = vfx.transform.forward;
            
            if (Physics.Raycast(vfx.transform.position + Vector3.up * 1f, Vector3.down, out RaycastHit down, 2f, vfx.player.psm.groundLayer))
                direction = Vector3.ProjectOnPlane(vfx.transform.forward, down.normal).normalized;
            
            
            vfx.rb.linearVelocity = speed * direction;
            yield return Timing.WaitForOneFrame;
        }
    }
    
    #endregion
    
    #region Rotating Path
    
    private void RotatingPath(VFXController vfx, WeaponType weaponType)
    {
        this.RunSegmentCoroutine(BeginRotatingPath(vfx, weaponType).CancelWith(vfx));
    }
    
    IEnumerator<float> BeginRotatingPath(VFXController vfx, WeaponType weaponType)
    {
        float startTime = Time.time;
        float duration = vfx.timeAlive - Time.deltaTime * 2;
        float speed = vfx.vfxSpawnInfo.vfxAttack.vfxSpeed;
        
        vfx.EnableVFX();
        
        while (Time.time - startTime < duration)
        {
            vfx.transform.position = vfx.player.transform.position + vfx.vfxSpawnInfo.spawnTransform.Position;
            vfx.transform.Rotate(Vector3.up, speed * Time.deltaTime);
            yield return Timing.WaitForOneFrame;
        }
    }
    
    #endregion
    
}
