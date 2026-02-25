using System;
using Extensions.Modifiers;
using UnityEngine;

[Serializable]
public class StatusEffectChange : IModifierProvider
{
    public StatusEffect StatusEffect;
    public int stacks;
    [Tooltip("Duration in seconds. 0 means permanent.")]
    public float duration;
}