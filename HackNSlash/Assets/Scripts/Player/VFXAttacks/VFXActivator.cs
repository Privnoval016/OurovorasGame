using System;
using System.Collections;
using System.Collections.Generic;
using ExtensionUtils;
using UnityEngine;
using UnityEngine.VFX;
using MEC;

public class VFXActivator : MonoBehaviour
{
    public VFXDelayInfo[] vfxs;
    [HideInInspector] public float duration;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void SetEffectLifetimes(float effectTime)
    {
        duration = effectTime;
        foreach (VFXDelayInfo vfx in vfxs)
        {
            vfx.effect.SafeSetFloat("Lifetime", duration * vfx.durationScale);
        }
    }
    
    public void SetVFXFloat(string name, float value)
    {
        foreach (VFXDelayInfo vfx in vfxs)
        {
            vfx.effect.SafeSetFloat(name, value);
        }
    }
    
    public void SetVFXVector4(string name, Vector4 value)
    {
        foreach (VFXDelayInfo vfx in vfxs)
        {
            vfx.effect.SafeSetVector4(name, value);
        }
    }
    
    public void PlayVFX()
    {
        gameObject.SetActive(true);
        
        foreach (VFXDelayInfo vfx in vfxs)
        {
            Timing.RunCoroutine(PlayWithDelay(vfx.effect, duration * vfx.delayScale).CancelWith(gameObject));
        }
    }
    
    IEnumerator<float> PlayWithDelay(VisualEffect vfx, float delay = 0)
    {
        yield return Timing.WaitForSeconds(delay);
        vfx.Play();
    }
}

[Serializable]
public class VFXDelayInfo
{
    public VisualEffect effect;
    [Range(0, 1)] public float durationScale = 1;
    [Range(0, 1)] public float delayScale;
}
