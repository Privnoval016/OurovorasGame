using System;
using System.Collections;
using System.Collections.Generic;
using Extensions.Utils;
using UnityEngine;
using UnityEngine.VFX;
using MEC;
using NaughtyAttributes;
using UnityEngine.Serialization;

public class VFXActivator : MonoBehaviour
{
    public VFXDelayInfo[] vfxs;
    public float totalDuration;
    private bool lifetimesSet = false;

    private void Awake()
    {
        gameObject.SetActive(false);
        foreach (VFXDelayInfo vfx in vfxs)
        {
            vfx.effect.Stop();
            vfx.effect.gameObject.SetActive(false);
        }
    }
    
    public void SetEffectLifetimes(float effectTime)
    {
        totalDuration = effectTime;
        foreach (VFXDelayInfo vfx in vfxs)
        {
            vfx.effect.SafeSetFloat("Lifetime", totalDuration * vfx.durationScale);
        }
        lifetimesSet = true;
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
    
    [Button]
    public void PlayVFX()
    {
        if (!lifetimesSet) SetEffectLifetimes(totalDuration);
        
        gameObject.SetActive(true);
        
        foreach (VFXDelayInfo vfx in vfxs)
        {
            this.RunSegmentCoroutine(PlayWithDelay(vfx.effect, totalDuration * vfx.delayScale));
        }
    }
    
    IEnumerator<float> PlayWithDelay(VisualEffect vfx, float delay = 0)
    {
        yield return Timing.WaitForSeconds(delay);
        vfx.gameObject.SetActive(true);
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
