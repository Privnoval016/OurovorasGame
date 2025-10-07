using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "BaseStats", menuName = "ScriptableObjects/BaseStats", order = 1)]
public class BaseStats : SerializedScriptableObject
{
    public readonly Dictionary<InnateStat, int> Stats = new();

    [HideInInspector] public readonly Dictionary<StatusEffect, int> StatusEffects = new();
}
