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


