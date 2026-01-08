using System;
using Extensions.Modifiers;
using UnityEngine;

[Serializable]
public struct InnateStatChange : IModifierProvider
{
    
    public InnateStat innateStat;
    public ChangeType changeType;
    public float value;
    [Tooltip("Duration in seconds. 0 means permanent.")]
    public float duration;
}

public enum InnateStat : int // value based stats
{
    MaxHealth,
    MaxCharge,
    Strength,
    Defense,
}