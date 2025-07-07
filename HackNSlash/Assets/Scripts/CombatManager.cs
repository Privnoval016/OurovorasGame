using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;
using UnityEngine.Serialization;

public class CombatManager : Singleton<CombatManager>
{
    
    #region Time Parameters
    
    [Header("Time Parameters")]
    
    public float unPausedTimescale = 1f;
    public float pausedTimescale = 0f;
    public bool hitStopped = false;
    
    private float preHitStopTimescale = 1f;

    [FormerlySerializedAs("playerStopped")] [FormerlySerializedAs("isPaused")] public bool entitiesStopped;
    
    #endregion

    #region Monobehaviour Callbacks

    

    #endregion
    
    #region Damage Methods
    
    public float CalculateDamage()
    {
        // Placeholder for damage calculation logic
        return 10f; // Example fixed damage value
    }
    
    #endregion
    
    #region Time Effects
    
    private void SetTimeScale(float timescale)
    {
        Time.timeScale = timescale;
    }
    
    public void ApplyPausedTimescale(PlayerController pc, bool pause)
    {
        SetTimeScale(pause ? pausedTimescale : unPausedTimescale);
        pc.cam.EnableCameraInputDetection(!pause);
        entitiesStopped = pause;
    }
    
    public void ApplySlowedTimeScale(PlayerController pc, bool slowed, float timescale = 0.1f)
    {
        SetTimeScale(slowed ? timescale : unPausedTimescale);
        pc.cam.EnableCameraInputDetection(!slowed);
        entitiesStopped = slowed;
    }
    
    public void HitStop(PlayerController pc, Attack a, bool playImmediately, int index = 0)
    {
        if (a == null || a.hitStopProfiles == null || index >= a.hitStopProfiles.Length) return;
        if (hitStopped) return; // Prevent hit stop if already paused
        
        HitStopProfile hitStopProfile = a.hitStopProfiles[index];
        
        pc.cam.ShakeCamera(hitStopProfile.hitStopDuration, hitStopProfile.screenShakeMagnitude);

        this.KillObjectCoroutines(nameof(ApplyHitStopTimescale));
        this.RunSegmentCoroutine(ApplyHitStopTimescale(hitStopProfile, playImmediately), nameof(ApplyHitStopTimescale));
    }
    
    IEnumerator<float> ApplyHitStopTimescale(HitStopProfile h, bool playImmediately)
    {
        //if (!playImmediately) yield return Timing.WaitForSeconds(h.minAnimTime);
        
        hitStopped = true;
        preHitStopTimescale = Time.timeScale;
        SetTimeScale(h.hitStopTimescale);
        yield return Timing.WaitForSeconds(h.hitStopDuration);
        hitStopped = false;
        SetTimeScale(preHitStopTimescale);
    }
    
    #endregion
    
    #region VFX Methods

    public void PlayHitEffects(PlayerController pc, Attack a, IContactDetector contact, bool playImmediately, int index = 0)
    {
        if (contact == null) return;
        
        List<Vector3> spawnPositions = new List<Vector3>();
        
        foreach (var target in pc.psm.EnemiesInHit)
        {
            Vector3 point = contact.GetClosestPointOnCollider(target.col);
            if (point != Vector3.zero)
            {
                spawnPositions.Add(point);
            }
        }
        
        foreach (Vector3 spawnPosition in spawnPositions)
        {
            OnVFXEvents.Instance.SpawnHitStopVFX(pc, a, index,
                new TransformInfo(spawnPosition, Quaternion.identity, Vector3.one));
        }
        
        HitStop(pc, a, playImmediately, index);
    }
    
    #endregion
}
