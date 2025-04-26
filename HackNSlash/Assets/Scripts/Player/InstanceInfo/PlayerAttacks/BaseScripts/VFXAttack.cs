using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using UnityEngine;
using UnityEngine.VFX;

public enum VFXType
{
    Slash,
    Trail,
    Burst,
}

[CreateAssetMenu(menuName = "Player/VFXAttack")]
public class VFXAttack : ScriptableObject
{
    [Header("VFX")]
    [Tooltip("The container for all of the VFX prefabs, which determines collision (if any)")]
    public GameObject vfxHitBox;
    [Tooltip("Individual VFX prefabs that will spawn as children of the hitbox (must be VFX Graphs)")]
    public VFXData[] vfxDatas;
        
    [Header("Movement Settings")]
    public OnVFXActions vfxAction = OnVFXActions.FollowWeapon;
    public float vfxSpeed;
    public bool canCollide = true;
}

[Serializable]
public class VFXData
{
    public VFXType vfxType;
    [Tooltip("Overrides the VFX associated with the current element")]
    public GameObject effect;

    public TransformInfo localTransform;
    [Tooltip("Duration of the VFX relative to the hitbox's duration")]
    [Range(0, 1)] public float durationScale = 1;

    [Tooltip("When the VFX will be spawned relative to the hitbox's duration")]
    [Range(0, 1)] public float delayScale = 0;
}
