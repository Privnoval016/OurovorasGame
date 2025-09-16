using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Extensions.Utils;
using MEC;
using PrimeTween;
using Unity.Mathematics;
using UnityEngine.VFX;

public enum OnVFXActions
{
    LinearPath,
    HomingPath,
    RotatingPath,
    StationaryPath,
    FollowWeapon,
    FollowGround,
}


public class OnVFXEvents : Singleton<OnVFXEvents>
{
    public static Dictionary<OnVFXActions, Action<VFXController>> OnVFXActionMap;
    
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

    public VFXController SpawnHitStopVFX(ElementEffect e, PlayerController pc, Attack a, 
        int hitStopProfileIndex, TransformInfo overrideTransform)
    {
        if (a == null || a.hitStopProfiles.Length == 0 || hitStopProfileIndex >= a.hitStopProfiles.Length)
        {
            Debug.LogWarning("Invalid attack or hit stop profile index.");
            return null;
        }
        
        HitStopProfile hitStopProfile = a.hitStopProfiles[hitStopProfileIndex];
        TransformInfo start = overrideTransform;
        start.Position += hitStopProfile.hitStopVFX.spawnTransform.Position;
        start.Rotation = hitStopProfile.hitStopVFX.spawnTransform.Rotation.eulerAngles != Vector3.zero
            ? start.Rotation * hitStopProfile.hitStopVFX.spawnTransform.Rotation
            : start.Rotation;
        start.Scale = hitStopProfile.hitStopVFX.spawnTransform.Scale == Vector3.zero 
            ? start.Scale 
            : start.Scale.ScaledBy(hitStopProfile.hitStopVFX.spawnTransform.Scale);
        
        GameObject vfxInstance = Instantiate(hitStopProfile.hitStopVFX.vfxAttack.vfxHitBox, 
            start.Position, start.Rotation);
        vfxInstance.transform.localScale = start.Scale;
        if (!vfxInstance.TryGetComponent(out VFXController vfxController))
        {
            Destroy(vfxInstance);
            return null;
        }
        
        var vfxActivators = GetVFXActivators(hitStopProfile.hitStopVFX, vfxController, 
            GameManager.GetElementData(ElementData.GetElementFromAttack(e, pc)));
        
        vfxController.InitializeVFX(e, start, hitStopProfile.hitStopVFX, vfxActivators, 0, 
            hitStopProfile.hitStopVFX.vfxAttack.canCollide);
        
        if (vfxController == null)
        {
            Debug.LogWarning("Failed to initialize VFXController.");
            return null;
        }
        
        vfxController.EnableVFX();
        
        return vfxController;
    }
    
    public VFXController SpawnPlayerVFX(PlayerController pc, Attack a, int vfxIndex = 0, 
        TransformInfo overrideTransform = default, WeaponType weaponType = WeaponType.None)
    {
        if (a == null || a.vfxInfos.Length == 0) return null;
        VFXController vfx = InstantiatePlayerVFX(pc, a, vfxIndex, overrideTransform);
        if (vfx == null) return null;
        
        if (vfx.HitDetector is PlayerVFXHitDetector) 
            ((PlayerVFXHitDetector) vfx.HitDetector).followedWeaponType = weaponType;
        
        
        this.RunSegmentCoroutine(SpawnWithDelay(vfx, a.vfxInfos[vfxIndex].delay));
        return vfx;
    }
    
    public VFXController SpawnEnemyVFX(EnemyController ts, EnemyAttackInfo a, int vfxIndex = 0, 
        TransformInfo overrideTransform = default)
    {
        if (a == null || a.attack.vfxInfos.Length == 0) return null;
        VFXController vfx = InstantiateEnemyVFX(ts, a, vfxIndex, overrideTransform);
        if (vfx == null) return null;
        this.RunSegmentCoroutine(SpawnWithDelay(vfx, a.attack.vfxInfos[vfxIndex].delay));
        return vfx;
    }
    
    IEnumerator<float> SpawnWithDelay(VFXController vfx, float t)
    {
        yield return Timing.WaitForSeconds(t);
        OnVFXActionMap[vfx.vfxSpawnInfo.vfxAttack.vfxAction].Invoke(vfx);
    }

    private VFXController InstantiatePlayerVFX(PlayerController pc, Attack a, int vfxIndex = 0,
        TransformInfo overrideTransform = default)
    {
        if (a.vfxInfos.Length <= vfxIndex) return null;

        VFXSpawnInfo v = a.vfxInfos[vfxIndex];

        TransformInfo start = overrideTransform;

        Transform parent = null;

        bool targetFound = false;

        switch (v.spawnTarget)
        {
            case PlayerTarget.Player:
                start.Position = pc.transform.position;
                start.Rotation = Quaternion.LookRotation(pc.transform.forward);
                parent = pc.transform;
                break;
            case PlayerTarget.TargetedEnemy:
                if (!pc.cam.IsLockedOn && v.spawnTransform.Position != Vector3.zero)
                {
                    start.Position = pc.transform.position;
                    start.Rotation = Quaternion.LookRotation(pc.transform.forward);
                    parent = pc.transform;
                    break;
                }
                else if (!pc.cam.IsLockedOn) return null;

                start.Position = pc.cam.TargetPosition;
                start.Rotation = Quaternion.LookRotation(pc.transform.position - start.Position);
                parent = pc.cam.TargetedEnemy.transform;
                targetFound = true;
                break;
            case PlayerTarget.KatanaSpirit:
                start.Position = pc.spirit.transform.position;
                start.Rotation = Quaternion.LookRotation(pc.spirit.transform.forward);
                parent = pc.spirit.transform;
                break;
        }

        TransformInfo offset = v.spawnTransform;

        start.Scale = start.Scale == Vector3.zero ? Vector3.one : start.Scale;
        start.Scale = start.Scale.ScaledBy(offset.Scale);


        GameObject vfx = Instantiate(v.vfxAttack.vfxHitBox, start.Position, start.Rotation);
        vfx.transform.localScale = start.Scale;

        if (!vfx.TryGetComponent(out VFXController vc))
        {
            Destroy(vfx);
            return null;
        }

        vc.transform.SetParent(parent, true);

        if (v.spawnTarget != PlayerTarget.None)
        {
            if (v.spawnTarget != PlayerTarget.TargetedEnemy || !targetFound)
            {
                if (v.applyParentPoseToPosition && parent != null)
                {
                    vc.transform.localRotation = offset.Rotation;
                    vc.transform.localPosition = offset.Position;
                }
                else
                {
                    vc.transform.position = start.Position + offset.Position;
                    vc.transform.rotation = offset.Rotation.eulerAngles != Vector3.zero
                        ? start.Rotation * offset.Rotation
                        : start.Rotation;
                }
            }

            if (!v.parentToTarget)
            {
                vc.transform.SetParent(null, true);
            }
        }
        else
        {
            vc.transform.position = start.Position + offset.Position;
            vc.transform.Rotate(offset.Rotation.eulerAngles);
        }
        
        ElementEffect element = ElementData.GetElementFromAttack(a.element, pc);
        
        ElementData e = GameManager.GetElementData(element);
        
        var vfxs = GetVFXActivators(v, vc, e);

        vc.InitializeVFX(element, new TransformInfo(vfx.transform), v, vfxs, vfxIndex, v.vfxAttack.canCollide);
        vc.AddHitDetector(new PlayerVFXHitDetector(vc, pc, a));
        
        return vc;
    }
    
    private VFXController InstantiateEnemyVFX(EnemyController ts, EnemyAttackInfo a, int vfxIndex = 0,
        TransformInfo overrideTransform = default)
    {
        if (a.attack.vfxInfos.Length <= vfxIndex) return null;

        VFXSpawnInfo v = a.attack.vfxInfos[vfxIndex];
        
        TransformInfo start = overrideTransform;
        start.Position += v.spawnTransform.Position;
        start.Rotation = v.spawnTransform.Rotation.eulerAngles != Vector3.zero
            ? start.Rotation * v.spawnTransform.Rotation
            : start.Rotation;
        start.Scale = v.spawnTransform.Scale == Vector3.zero 
            ? start.Scale 
            : start.Scale.ScaledBy(v.spawnTransform.Scale);
        
        GameObject vfxInstance = Instantiate(v.vfxAttack.vfxHitBox, 
            start.Position, start.Rotation);
        vfxInstance.transform.localScale = start.Scale;
        if (!vfxInstance.TryGetComponent(out VFXController vfxController))
        {
            Destroy(vfxInstance);
            return null;
        }
        
        ElementEffect element = ts.GetElementFromAttack(a.attack.element);
        
        var vfxActivators = GetVFXActivators(v, vfxController, GameManager.GetElementData(element));
        
        vfxController.InitializeVFX(element, start, v, vfxActivators, vfxIndex, 
            v.vfxAttack.canCollide);
        vfxController.AddHitDetector(new EnemyVFXHitDetector(vfxController, ts, a));
        
        if (vfxController == null)
        {
            Debug.LogWarning("Failed to initialize VFXController.");
            return null;
        }
        
        vfxController.EnableVFX();
        
        return vfxController;
    }
    
    private VFXActivator[] GetVFXActivators(VFXSpawnInfo v, VFXController vfx, ElementData e)
    {
        List<VFXActivator> vfxs = new();

        VFXData[] vfxDatas = v.vfxAttack.vfxDatas;
        foreach (VFXData vfxData in vfxDatas)
        {
            GameObject effect = vfxData.effect == null ? e.GetVFX(vfxData.vfxType) 
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
        
        if (vfxs.Count == 0) return Array.Empty<VFXActivator>();
        return vfxs.ToArray();
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
                if (oldest != null)
                {
                    OnVFXDisabled(oldest);
                    oldest.DestroyVFX();
                }
            }
        }
    }

    public void OnVFXDisabled(VFXController vfx)
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
    
    private void LinearPath(VFXController vfx)
    {
        this.RunSegmentCoroutine(BeginLinearPath(vfx).CancelWith(vfx));
    }
    
    IEnumerator<float> BeginLinearPath(VFXController vfx)
    {
        VFXAttack v = vfx.vfxSpawnInfo.vfxAttack;
        
        Vector3 direction = vfx.transform.forward;
        vfx.transform.TweenDistance(direction, vfx.timeActive * v.vfxSpeed, vfx.timeActive, Ease.Linear);
        
        vfx.EnableVFX();
        
        yield return Timing.WaitForSeconds(vfx.timeActive);
    }
    
    #endregion
    
    
    #region Follow Weapon
    
    private void FollowWeapon(VFXController vfx)
    {
        this.RunSegmentCoroutine(BeginFollowWeapon(vfx).CancelWith(vfx));
    }
    
    IEnumerator<float> BeginFollowWeapon(VFXController vfx)
    {
        if (vfx.HitDetector is not PlayerVFXHitDetector)
        {
            Debug.LogWarning($"VFX {vfx.name} does not have a PlayerVFXHitDetector. " +
                             $"Cannot follow weapon. VFX not spawned.");
            yield break;
        }
        
        PlayerVFXHitDetector hitDetector = (PlayerVFXHitDetector) vfx.HitDetector;
        
        WeaponBody weaponBody = hitDetector.player.wc.GetWeapon(hitDetector.followedWeaponType);
        
        if (weaponBody == null)
        {
            Debug.LogWarning($"No weapon body found for {hitDetector.followedWeaponType} " +
                             $"on player {hitDetector.player.name}. VFX not spawned.");
            yield break;
        }
        
        vfx.ChangeParent(weaponBody.transform);
        vfx.transform.localPosition = Vector3.zero;
        vfx.transform.localRotation = Quaternion.identity;
        
        vfx.EnableVFX();
        
        yield return Timing.WaitForSeconds(vfx.timeActive);
    }
    
    
    #endregion
    
    
    #region Stationary Path
    
    private void StationaryPath(VFXController vfx)
    {
        this.RunSegmentCoroutine(BeginStationaryPath(vfx).CancelWith(vfx));
    }
    
    IEnumerator<float> BeginStationaryPath(VFXController vfx)
    {
        VFXSpawnInfo v = vfx.vfxSpawnInfo;
        
        vfx.EnableVFX();
        
        yield return Timing.WaitForSeconds(v.duration);
    }
    
    #endregion
    
    #region Follow Ground
    
    private void FollowGround(VFXController vfx)
    {
        this.RunSegmentCoroutine(BeginFollowGround(vfx).CancelWith(vfx));
    }
    
    IEnumerator<float> BeginFollowGround(VFXController vfx)
    {
        Vector3 position = vfx.transform.GetGroundedPosition(GameManager.Instance.groundLayer);
        vfx.transform.position = position;
        
        float startTime = Time.time;
        float duration = vfx.timeActive - Time.deltaTime * 2;
        float speed = vfx.vfxSpawnInfo.vfxAttack.vfxSpeed;
        
        vfx.EnableVFX();
        
        while (Time.time - startTime < duration)
        {
            Vector3 direction = vfx.transform.forward;
            
            if (Physics.Raycast(vfx.transform.position + Vector3.up * 1f, Vector3.down, 
                    out RaycastHit down, 2f, GameManager.Instance.groundLayer))
                direction = Vector3.ProjectOnPlane(vfx.transform.forward, down.normal).normalized;
            
            
            vfx.rb.linearVelocity = speed * direction;
            yield return Timing.WaitForOneFrame;
        }
        
        vfx.rb.linearVelocity = Vector3.zero;
    }
    
    #endregion
    
    #region Rotating Path
    
    private void RotatingPath(VFXController vfx)
    {
        this.RunSegmentCoroutine(BeginRotatingPath(vfx).CancelWith(vfx));
    }
    
    IEnumerator<float> BeginRotatingPath(VFXController vfx)
    {
        if (vfx.HitDetector.creatorTransform == null)
        {
            Debug.LogWarning($"VFX {vfx.name} does not have a creator transform. " +
                             $"Cannot follow weapon. VFX not spawned.");
            yield break;
        }
        
        float startTime = Time.time;
        float duration = vfx.timeActive - Time.deltaTime * 2;
        float speed = vfx.vfxSpawnInfo.vfxAttack.vfxSpeed;
        
        vfx.EnableVFX();
        
        while (Time.time - startTime < duration)
        {
            vfx.transform.position = vfx.HitDetector.creatorTransform.position + vfx.vfxSpawnInfo.spawnTransform.Position;
            vfx.transform.Rotate(Vector3.up, speed * Time.deltaTime);
            yield return Timing.WaitForOneFrame;
        }
    }
    
    #endregion
    
}
