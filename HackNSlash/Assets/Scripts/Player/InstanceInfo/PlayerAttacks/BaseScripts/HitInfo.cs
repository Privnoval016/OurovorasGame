using System;
using UnityEngine;

[Serializable]
public class HitInfo
{
    public HitActionInfo[] hitActionInfos;
    
    [Header ("Hit Detection")]
    public HitDetections hitDetection;
    public float lateralRadius = 3;
    public float verticalRadius = 3;
    public float hitRegisterAngle = 120;
    public int numTargets = 1;
    
    [Header("Stats")]
    public float attackCoolDown;

    public float hitCoolDown;
}

[Serializable]
public struct HitActionInfo
{
    [SerializeReference] public IHitAction hitAction;
}