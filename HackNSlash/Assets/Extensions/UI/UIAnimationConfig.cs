using UnityEngine;

namespace Extensions.UI
{
    /// <summary>
    /// Centralized configuration for UI animation settings.
    /// Defines colors, durations, and animation parameters used across all menu UI.
    /// </summary>
    [CreateAssetMenu(fileName = "UIAnimationConfig", menuName = "UI/Animation Config")]
    public class UIAnimationConfig : ScriptableObject
    {
        [Header("Colors")]
        [Tooltip("Normal/unselected state color")]
        public Color normalColor = Color.white;
        
        [Tooltip("Selected/highlighted state color")]
        public Color selectedColor = Color.yellow;
        
        [Tooltip("Disabled state color")]
        public Color disabledColor = Color.gray;
        
        [Tooltip("Empty slot color")]
        public Color emptyColor = new Color(0.5f, 0.5f, 0.5f);
        
        [Tooltip("Locked state color")]
        public Color lockedColor = new Color(0.3f, 0.3f, 0.3f);
        
        [Tooltip("Equipped indicator color")]
        public Color equippedColor = Color.green;
        
        [Header("Rarity Colors")]
        [Tooltip("Common rarity color")]
        public Color commonColor = Color.white;
        
        [Tooltip("Uncommon rarity color")]
        public Color uncommonColor = Color.green;
        
        [Tooltip("Rare rarity color")]
        public Color rareColor = Color.blue;
        
        [Tooltip("Epic rarity color")]
        public Color epicColor = Color.magenta;
        
        [Tooltip("Legendary rarity color")]
        public Color legendaryColor = Color.yellow;
        
        [Header("Text Colors")]
        [Tooltip("Normal text color")]
        public Color normalTextColor = Color.white;
        
        [Tooltip("Selected text color")]
        public Color selectedTextColor = Color.yellow;
        
        [Tooltip("Empty text color")]
        public Color emptyTextColor = Color.gray;
        
        [Header("Animation Durations")]
        [Tooltip("Duration for color transitions")]
        public float colorTransitionDuration = 0.15f;
        
        [Tooltip("Duration for scale animations")]
        public float scaleAnimationDuration = 0.15f;
        
        [Tooltip("Duration for fade animations")]
        public float fadeAnimationDuration = 0.2f;
        
        [Tooltip("Duration for punch scale effect")]
        public float punchScaleDuration = 0.3f;
        
        [Header("Scale Values")]
        [Tooltip("Scale multiplier when selected")]
        public float selectedScale = 1.05f;
        
        [Tooltip("Punch scale strength (added to current scale)")]
        public Vector3 punchScaleStrength = new Vector3(0.2f, 0.2f, 0.2f);
        
        [Header("Easing")]
        [Tooltip("Ease type for selection animations")]
        public PrimeTween.Ease selectionEase = PrimeTween.Ease.OutBack;
        
        [Tooltip("Ease type for deselection animations")]
        public PrimeTween.Ease deselectionEase = PrimeTween.Ease.OutQuad;
        
        [Tooltip("Ease type for color transitions")]
        public PrimeTween.Ease colorEase = PrimeTween.Ease.Linear;
    }
}

