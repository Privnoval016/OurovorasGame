using System;
using UnityEngine;
using UnityEngine.Serialization;

public interface IStatChangeProvider
{
    
}

public enum ChangeType
{
    Flat,
    AdditivePercent,
    MultiplicativePercent,
}

[Serializable]
public struct InnateStatChange : IStatChangeProvider
{
    
    [FormerlySerializedAs("stat")] public InnateStat innateStat;
    public ChangeType changeType;
    public float value;
    [Tooltip("Duration in seconds. 0 means permanent.")]
    public float duration;
}

[Serializable]
public class StatusEffectChange : IStatChangeProvider
{
    public StatusEffect StatusEffect;
    public int stacks;
    [Tooltip("Duration in seconds. 0 means permanent.")]
    public float duration;
}