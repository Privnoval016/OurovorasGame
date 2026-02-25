using System;
using Extensions.Utils;
using UnityEngine;

[CreateAssetMenu(fileName = "HitStopProfile", menuName = "GameFeel/HitStopProfile", order = 1)]
public class HitStopProfile : ScriptableObject
{
    [Header("Time Parameters")]
    public float hitStopDuration = 0.1f;
    public float hitStopTimescale = 0;
    public float hitStopDelay = 0;
    
    [Header("Screen Shake Parameters")]
    public float screenShakeMagnitude = 0.1f;

    [Header("VFXParameters")] 
    public VFXSpawnInfo hitStopVFX;


    public static HitStopProfile[] ShallowCopy(HitStopProfile[] profiles)
    {
        if (profiles == null) return null;

        HitStopProfile[] copy = new HitStopProfile[profiles.Length];
        for (int i = 0; i < profiles.Length; i++)
        {
            copy[i] = profiles[i];
        }

        return copy;
    }

    private void OnValidate()
    {
        if (hitStopVFX == null) hitStopVFX = new VFXSpawnInfo();
        if (hitStopVFX.spawnTransform.Scale == Vector3.zero) 
        {
            hitStopVFX.spawnTransform.Scale = Vector3.one;
        }
    }
}
