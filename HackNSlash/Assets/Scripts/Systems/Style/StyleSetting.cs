using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StyleSetting", menuName = "ScriptableObjects/Style/StyleSetting", order = 1)]
public class StyleSetting : ScriptableObject
{
    [Header("Numerical Settings")]
    public StyleLevel level;
    public float threshold;
    public float decayRate;
    public float penaltyRate;
    
    [Header("Graphical Settings")]
    public Sprite meterBackground;
    public Sprite meterOutline;
    public Sprite meterFill;
}