using UnityEngine;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Central manager for all UI animations in the menu system.
    /// Provides builders for creating animations with consistent settings.
    /// Place this on the root menu GameObject.
    /// </summary>
    public class UIAnimationManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private UIAnimationConfig animationConfig;
        
        [Header("Default Settings")]
        [SerializeField] private bool useUnscaledTimeByDefault = true;
        
        private static UIAnimationManager instance;
        
        /// <summary>
        /// Gets the active animation manager instance.
        /// </summary>
        public static UIAnimationManager Instance => instance;
        
        /// <summary>
        /// Gets the animation configuration.
        /// </summary>
        public UIAnimationConfig Config => animationConfig;
        
        private void Awake()
        {
            // Set instance (local to this menu)
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Debug.LogWarning("Multiple UIAnimationManagers found. This may cause issues.");
            }
            
            // Validate config
            if (animationConfig == null)
            {
                Debug.LogError("UIAnimationManager: No animation config assigned!");
            }
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
        
        /// <summary>
        /// Creates a new animation builder for the given target.
        /// </summary>
        /// <param name="target">The transform to animate.</param>
        /// <param name="graphics">Optional graphics to animate colors on.</param>
        /// <returns>A new animation builder.</returns>
        public UIAnimationBuilder CreateBuilder(Transform target, params Graphic[] graphics)
        {
            if (animationConfig == null)
            {
                Debug.LogError("UIAnimationManager: Cannot create builder without animation config!");
                return null;
            }
            
            return new UIAnimationBuilder(animationConfig, target, graphics)
                .WithUnscaledTime(useUnscaledTimeByDefault);
        }
        
        /// <summary>
        /// Gets the normal color from config.
        /// </summary>
        public Color GetNormalColor() => animationConfig?.normalColor ?? Color.white;
        
        /// <summary>
        /// Gets the selected color from config.
        /// </summary>
        public Color GetSelectedColor() => animationConfig?.selectedColor ?? Color.yellow;
        
        /// <summary>
        /// Gets the disabled color from config.
        /// </summary>
        public Color GetDisabledColor() => animationConfig?.disabledColor ?? Color.gray;
        
        /// <summary>
        /// Gets the empty color from config.
        /// </summary>
        public Color GetEmptyColor() => animationConfig?.emptyColor ?? Color.gray;
        
        /// <summary>
        /// Gets the equipped color from config.
        /// </summary>
        public Color GetEquippedColor() => animationConfig?.equippedColor ?? Color.green;
        
        /// <summary>
        /// Gets the normal text color from config.
        /// </summary>
        public Color GetNormalTextColor() => animationConfig?.normalTextColor ?? Color.white;
        
        /// <summary>
        /// Gets the selected text color from config.
        /// </summary>
        public Color GetSelectedTextColor() => animationConfig?.selectedTextColor ?? Color.yellow;
        
        /// <summary>
        /// Gets the empty text color from config.
        /// </summary>
        public Color GetEmptyTextColor() => animationConfig?.emptyTextColor ?? Color.gray;
        
        /// <summary>
        /// Gets the locked color from config.
        /// </summary>
        public Color GetLockedColor() => animationConfig?.lockedColor ?? Color.gray;
        
        /// <summary>
        /// Gets the rarity color from config.
        /// </summary>
        public Color GetRarityColor(Rarity rarity)
        {
            if (animationConfig == null) return Color.white;
            
            return rarity switch
            {
                Rarity.Common => animationConfig.commonColor,
                Rarity.Uncommon => animationConfig.uncommonColor,
                Rarity.Rare => animationConfig.rareColor,
                Rarity.Epic => animationConfig.epicColor,
                Rarity.Legendary => animationConfig.legendaryColor,
                _ => animationConfig.commonColor
            };
        }
    }
}

