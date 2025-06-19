using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public string[] pureColorOverrides;
    public string[] brightColorOverrides;
    public string[] darkColorOverrides;
    public string[] gradientColorOverrides;
    public ParamDelayScales[] lifetimeOverrides;
    
    public float MaximumLifetimeScale => lifetimeOverrides.Length > 0 ? lifetimeOverrides.Max(p => p.lifetimeScale) : 1;

    private void Awake()
    {
        InitializeOverrides();
        
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
            if (vfx.effect == null) continue;

            foreach (var p in lifetimeOverrides)
            {
                vfx.effect.SafeSetFloat(p.paramName, totalDuration * p.lifetimeScale * vfx.durationScale);
            }
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
    
    public void SetVFXGradient(string name, Gradient gradient)
    {
        foreach (VFXDelayInfo vfx in vfxs)
        {
            vfx.effect.SafeSetGradient(name, gradient);
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

    private void OnValidate()
    {
        InitializeOverrides();
    }

    private void InitializeOverrides()
    {
        if (pureColorOverrides == null || pureColorOverrides.Length == 0)
        {
            pureColorOverrides = new string[] { "PureColor" };
        }
        
        if (brightColorOverrides == null || brightColorOverrides.Length == 0)
        {
            brightColorOverrides = new string[] { "BrightColor" };
        }
        
        if (darkColorOverrides == null || darkColorOverrides.Length == 0)
        {
            darkColorOverrides = new string[] { "DarkColor" };
        }
        
        if (gradientColorOverrides == null || gradientColorOverrides.Length == 0)
        {
            gradientColorOverrides = new string[] { "GradientColor1" };
        }
        
        if (lifetimeOverrides == null || lifetimeOverrides.Length == 0)
        {
            lifetimeOverrides = new ParamDelayScales[]
            {
                new ParamDelayScales { paramName = "Lifetime", lifetimeScale = 1 },
            };
        }
    }

    [Serializable]
    public class ParamDelayScales
    {
        public string paramName;
        public float lifetimeScale = 1;
    }
}

[Serializable]
public class VFXDelayInfo
{
    public VisualEffect effect;
    [Range(0, 1)] public float durationScale = 1;
    [Range(0, 1)] public float delayScale;
}
