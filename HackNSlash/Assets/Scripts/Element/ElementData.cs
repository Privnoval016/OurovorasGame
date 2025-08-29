using System;
using System.Collections.Generic;
using Extensions.Utils;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementData", menuName = "Element Data", order = 0)]
public class ElementData : ScriptableObject
{
    public ElementEffect element;
    
    [Header("Element Colors")]
    [ColorUsage(true, false)]
    public Color elementColor;
    [ColorUsage(true, false)]
    public Color elementInactiveColor;

    [Header("VFX Colors")] 
    [ColorUsage(true, true)]
    public Color vfxPureColor;
    
    [ColorUsage(true, true)]
    public Color vfxBrightColor;
    
    [ColorUsage(true, true)]
    public Color vfxDarkColor;
    
    [GradientUsage(true)]
    public Gradient vfxGradient;


    [Header("VFX Attributes")] 
    public VFXObjectInfo[] elementVFXs;
    
    private Dictionary<VFXType, GameObject> vfxDict = new();
    
    public Material weaponTrailMaterial;
    
    
    public GameObject GetVFX(VFXType type)
    {
        return vfxDict.GetValueOrDefault(type, null);
    }

    public static ElementEffect GetElementFromAttack(ElementEffect attackElement, PlayerController pc)
    {
        ElementEffect element = attackElement;

        if (element == ElementEffect.MatchCurrent)
        {
            element = pc.pi.currentElementEffect;
        }
        else if (element == ElementEffect.None && pc.pi.imbuedElementEffect != ElementEffect.None)
        {
            element = pc.pi.imbuedElementEffect;
        }
        else if (element == ElementEffect.None && pc.psm.movingState == MovingStates.Katana)
        {
            element = ElementEffect.Aether;
        }

        return element;
    }

    private void OnValidate()
    {
        vfxDict = new Dictionary<VFXType, GameObject>();
        
        foreach (VFXObjectInfo vfxInfo in elementVFXs)
        {
            if (vfxInfo.vfxPrefab != null && !vfxDict.ContainsKey(vfxInfo.vfxType))
            {
                vfxDict.Add(vfxInfo.vfxType, vfxInfo.vfxPrefab);
            }
        }
    }
    
    [Serializable]
    public class VFXObjectInfo
    {
        public VFXType vfxType;
        public GameObject vfxPrefab;
    }
}


