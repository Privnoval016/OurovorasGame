using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


[CreateAssetMenu(fileName = "ElementData", menuName = "Element Data", order = 0)]
public class ElementData : SerializedScriptableObject
{
    public ElementEffect element;

    [Header("Element Status Effects")]
    [SerializeField] private StatusEffect statusEffect;

    [Header("Element Colors")]
    [ColorUsage(true, false)]
    [Tooltip("The main non-emissive color used for the element, applied to most element-dependent visuals")]
    public Color elementColor;
    [ColorUsage(true, false)]
    [Tooltip("The non-emissive color used for the element when inactive, applied to element-dependent visuals when the element is not active")]
    public Color elementInactiveColor;

    [Header("VFX Colors")] 
    [ColorUsage(true, true)]
    [Tooltip("Base emissive color used for VFX")]
    public Color vfxPureColor;

    [ColorUsage(true, true)]
    [Tooltip("Bright emissive color used for VFX, typically for highlights and intense effects")]
    public Color vfxBrightColor;

    [ColorUsage(true, true)]
    [Tooltip("Dark emissive color used for VFX, typically for shadows and less intense effects")]
    public Color vfxDarkColor;

    [GradientUsage(true)]
    [Tooltip("Emissive gradient used for VFX, allowing for smooth color transitions in effects")]
    public Gradient vfxGradient;

    [Header("Crystal Colors")]
    [ColorUsage(true, false)]
    public Color crystalBaseColor = new Color(0.25f, 0.55f, 1f, 0.35f);
    [ColorUsage(true, false)]
    public Color crystalBaseColorDark = new Color(0.04f, 0.08f, 0.3f, 0.35f);
    [ColorUsage(true, true)]
    public Color crystalRimColor = new Color(0.5f, 0.85f, 1f, 1f);
    [ColorUsage(true, true)]
    public Color crystalSubsurfaceColor = new Color(0.15f, 0.45f, 1f, 1f);
    [ColorUsage(true, true)]
    public Color crystalSparkleColor = new Color(1f, 1f, 1f, 1f);
    [ColorUsage(true, true)]
    public Color crystalEmissionColor = new Color(0.3f, 0.65f, 1f, 1f);


    [Header("VFX Attributes")] 

    public Dictionary<VFXType, GameObject> ElementVFXs = new();

    public Material weaponTrailMaterial;
    
    [Header("Audio")]
    public AudioParamValueSO elementAudioParam;


    public GameObject GetVFX(VFXType type)
    {
        return ElementVFXs.GetValueOrDefault(type, null);
    }

    public StatusEffect GetStatusEffect()
    {
        return Services.Get<EffectSystem>()?.GetStatusEffect(statusEffect);
    }

    public static ElementEffect GetElementFromAttack(ElementEffect attackElement, PlayerController pc)
    {
        ElementEffect element = attackElement;

        if (element == ElementEffect.MatchCurrent)
        {
            element = pc.pcc.currentElementEffect;
        }
        else if (element == ElementEffect.None && pc.pcc.imbuedElementEffect != ElementEffect.None)
        {
            element = pc.pcc.imbuedElementEffect;
        }
        else if (element == ElementEffect.None && pc.psm.movingState == MovingStates.Katana)
        {
            element = ElementEffect.Aether;
        }

        return element;
    }
}


