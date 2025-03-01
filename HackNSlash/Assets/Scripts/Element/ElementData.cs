using System;
using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementData", menuName = "Element Data", order = 0)]
public class ElementData : ScriptableObject
{
    public ElementEffect element;

    [Header("VFX Colors")] [ColorUsage(true, true)]
    public Color vfxPureColor;
    
    [ColorUsage(true, true)]
    public Color vfxBrightColor;
    
    [ColorUsage(true, true)]
    public Color vfxDarkColor;
    
    
    [Header("VFX Attributes")] 
    [SerializedDictionary("VFXType", "Effect GameObject")]
    public SerializedDictionary<VFXType, GameObject> elementVFXs = new();
    
    public GameObject GetVFX(VFXType type)
    {
        return elementVFXs[type];
    }
    
}
