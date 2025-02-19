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
    
    public static Dictionary<OnVFXActions, Action<PlayerController, TransformInfo, Attack>> OnVFXActionMap;
    
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
    }
    
    private VFXController InstantiateVFX(PlayerController pc, TransformInfo start, Attack a)
    {
        GameObject vfx = Instantiate(a.vfxAttack.vfxHitBox, start.Position, start.Rotation);
        
        if (!vfx.TryGetComponent(out VFXController vc))
        {
            Destroy(vfx);
            return null;
        }
        
        List<VisualEffect> vfxs = new List<VisualEffect>();

        foreach (VFXData vfxData in a.vfxAttack.vfxDatas)
        {
            GameObject effect = vfxData.effect == null ? 
                GameManager.CurrentElementData.GetVFX(vfxData.vfxType) : vfxData.effect;
            if (effect == null) continue;
            
            GameObject vfxInstance = Instantiate(effect, vfx.transform);
            if (!vfxInstance.TryGetComponent(out VisualEffect vfxInstanceVFX))
            {
                Destroy(vfxInstance);
                continue;
            }
            
            vfxInstanceVFX.SafeSetFloat("Lifetime", vfxData.duration <= 0 ? a.vfxAttack.vfxDuration : vfxData.duration);
            
            vfxInstance.transform.localPosition = vfxData.localTransform.Position;
            vfxInstance.transform.localRotation = vfxData.localTransform.Rotation;
            vfxInstance.transform.localScale = vfxData.localTransform.Scale;
            
            vfxs.Add(vfxInstanceVFX);
        }
        
        vc.InitializeVFX(pc, start, a, vfxs.ToArray(), a.vfxAttack.canCollide);
        return vc;
    }
    
    #region Linear Path
    
    private void LinearPath(PlayerController pc, TransformInfo start, Attack a)
    {
        Timing.RunCoroutine(BeginLinearPath(pc, start, a));
    }
    
    IEnumerator<float> BeginLinearPath(PlayerController pc, TransformInfo start, Attack a)
    {
        VFXAttack v = a.vfxAttack;
        
        VFXController vfx = InstantiateVFX(pc, start, a);
        if (vfx == null) yield break;
        
        Vector3 direction = vfx.transform.forward;
        vfx.transform.TweenDistance(direction, v.vfxDuration * v.vfxSpeed, v.vfxDuration, Ease.Linear);
        
        Destroy(vfx.gameObject, v.vfxDuration);
    }
    
    #endregion
    
    
    #region Follow Weapon
    
    private void FollowWeapon(PlayerController pc, TransformInfo start, Attack a)
    {
        Timing.RunCoroutine(BeginFollowWeapon(pc, start, a));
    }
    
    IEnumerator<float> BeginFollowWeapon(PlayerController pc, TransformInfo start, Attack a)
    {
        VFXAttack v = a.vfxAttack;
        
        VFXController vfx = InstantiateVFX(pc, start, a);
        if (vfx == null) yield break;
        
        vfx.transform.SetParent(pc.wc.WeaponBodies[0].transform);
        vfx.transform.localPosition = Vector3.zero;
        vfx.transform.localRotation = Quaternion.identity;
        
        Destroy(vfx.gameObject, v.vfxDuration);
    }
    
    
    #endregion
    
    
}
