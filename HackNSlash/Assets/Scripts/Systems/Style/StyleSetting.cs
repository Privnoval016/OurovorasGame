using System;
using System.Collections.Generic;
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
    public Vector3 meterScale = Vector3.one;
    [Tooltip("Indices to identify which meters to activate in the UI (e.g., different styles might have different meter designs).")]
    public List<int> meterUsageIndex;

    private void OnValidate()
    {
        if (meterUsageIndex == null || meterUsageIndex.Count == 0)
        {
            meterUsageIndex = new List<int> { 0 };
        }
    }
}