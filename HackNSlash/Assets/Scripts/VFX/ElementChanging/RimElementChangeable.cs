using System;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

[Serializable]
public class RimElementChangeable : IElementChangeable
{
    
    [Header("Components")]
    public MaterialInfo[] materialInfos; // Array of materials and their target renderers
    public ShieldComponent shieldComponent;
    [Header("Rim Material Properties")]
    public RimMaterialProperties shieldedProperties;
    public RimMaterialProperties brokenProperties;
    public float tweenDuration = 0.15f;
    
    private List<Material> newMaterialInstances = new List<Material>();
    private bool isShieldBroken = false;
    private ElementEffect currentElementEffect = ElementEffect.None;
    
    public override void Initialize()
    {
        foreach (var materialInfo in materialInfos)
        {
            if (materialInfo.material == null)
            {
                Debug.LogError("RimElementChangeable: Base material is not assigned.");
                return;
            }

            var newMaterialInstance = new Material(materialInfo.material);
            newMaterialInstances.Add(newMaterialInstance);

            foreach (var meshRenderer in materialInfo.targetRenderers)
            {
                if (meshRenderer == null)
                {
                    Debug.LogError("RimElementChangeable: One of the mesh renderers is not assigned.");
                    continue;
                }

                meshRenderer.material = newMaterialInstance;
            }
        }

        if (shieldComponent != null)
        {
            shieldComponent.OnShieldBreak += OnShieldBreak;
            shieldComponent.OnShieldRestored += OnShieldRestored;
            
            if (shieldComponent.IsShieldActive)
                OnShieldRestored();
            else
                OnShieldBreak();
        }
        else
        {
            Debug.LogWarning("RimElementChangeable: ShieldComponent reference is not assigned. Rim will not react to shield state changes.");
        }
    }
    
    public override void UpdateElement(ElementEffect newElement)
    {
        currentElementEffect = newElement;
        if (isShieldBroken) return;
        var elementData = Services.Get<ElementSystem>().GetElementData(newElement);

        foreach (var newMaterialInstance in newMaterialInstances)
        {
            Tween.MaterialProperty(newMaterialInstance, RimMaterialProperties.RimColor, elementData.elementColor, tweenDuration);
        }
    }

    private void OnShieldBreak()
    {
        isShieldBroken = true;

        foreach (var newMaterialInstance in newMaterialInstances)
        {
            Tween.MaterialProperty(newMaterialInstance, RimMaterialProperties.RimColor, brokenProperties.rimColor,
                tweenDuration);
            Tween.MaterialProperty(newMaterialInstance, RimMaterialProperties.RimAttenuation,
                brokenProperties.rimAttenuation, tweenDuration);
            Tween.MaterialProperty(newMaterialInstance, RimMaterialProperties.MinRim, brokenProperties.minRim,
                tweenDuration);
            Tween.MaterialProperty(newMaterialInstance, RimMaterialProperties.MaxRim, brokenProperties.maxRim,
                tweenDuration);
            Tween.MaterialProperty(newMaterialInstance, RimMaterialProperties.RimOffset, brokenProperties.rimOffset,
                tweenDuration);
        }
    }
    
    private void OnShieldRestored()
    {
        isShieldBroken = false;
        var elementData = Services.Get<ElementSystem>().GetElementData(currentElementEffect);

        foreach (var newMaterialInstance in newMaterialInstances)
        {
            Tween.MaterialProperty(newMaterialInstance, RimMaterialProperties.RimColor, elementData.elementColor,
                tweenDuration);
            Tween.MaterialProperty(newMaterialInstance, RimMaterialProperties.RimAttenuation,
                shieldedProperties.rimAttenuation, tweenDuration);
            Tween.MaterialProperty(newMaterialInstance, RimMaterialProperties.MinRim, shieldedProperties.minRim,
                tweenDuration);
            Tween.MaterialProperty(newMaterialInstance, RimMaterialProperties.MaxRim, shieldedProperties.maxRim,
                tweenDuration);
            Tween.MaterialProperty(newMaterialInstance, RimMaterialProperties.RimOffset, shieldedProperties.rimOffset,
                tweenDuration);
        }
    }

    [Serializable]
    public struct RimMaterialProperties
    {
        public static readonly int RimColor = Shader.PropertyToID("_RimColor"); // HDR Color
        public static readonly int RimAttenuation = Shader.PropertyToID("_RimAttenuation"); // Float [0,1]
        public static readonly int MinRim = Shader.PropertyToID("_MinRim"); // Float [0,1]
        public static readonly int MaxRim = Shader.PropertyToID("_MaxRim"); // Float [0,1]
        public static readonly int RimOffset = Shader.PropertyToID("_RimOffset"); // Vector3
        
        [ColorUsage(true, true)]
        public Color rimColor;
        public float rimAttenuation;
        public float minRim;
        public float maxRim;
        public Vector3 rimOffset;
    }
}