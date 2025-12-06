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

public enum StatusEffectTargets
{
    None,
    Speed,          // affects move speed
    DamageDealt,    // affects damage dealt to others
    DamageTaken,    // affects damage taken from others
    DynamicDamage   // affects damage taken during updates/not necessarily just when hit (shared damage, DOT, etc)
}