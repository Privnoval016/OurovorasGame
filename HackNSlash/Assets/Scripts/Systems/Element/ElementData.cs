using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Systems.Element
{
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


        [Header("VFX Attributes")] 
    
        public Dictionary<VFXType, GameObject> ElementVFXs = new();
    
        public Material weaponTrailMaterial;
    
    
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
}


