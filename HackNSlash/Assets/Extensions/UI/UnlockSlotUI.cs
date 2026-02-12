using TMPro;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Extensions.UI
{
    /// <summary>
    /// UI component for displaying an unlock slot in the progression grid.
    /// Shows unlock icon, level requirement, and locked/unlocked state.
    /// Completely modular and reusable.
    /// </summary>
    public class UnlockSlotUI : Selectable, ISubmitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private Image lockOverlay;
        [SerializeField] private TextMeshProUGUI levelText;
        
        [Header("Events")]
        public UnityEvent<int> onUnlockHovered; // For showing description
        
        private int unlockIndex;
        private string unlockName;
        private string description;
        private int requiredLevel;
        private bool isUnlocked;
        private bool isHovered;
        private UIAnimationManager animationManager;
        
        protected override void Awake()
        {
            base.Awake();
            animationManager = UIAnimationManager.Instance;
        }
        
        /// <summary>
        /// Initializes the unlock slot with data.
        /// </summary>
        public void Initialize(int index, string name, string desc, int level, bool unlocked, Sprite icon)
        {
            unlockIndex = index;
            unlockName = name;
            description = desc;
            requiredLevel = level;
            isUnlocked = unlocked;
            
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
            }
            
            if (levelText != null)
            {
                levelText.text = $"Lv.{level}";
            }
            
            UpdateLockedState();
        }
        
        /// <summary>
        /// Updates visual state based on locked/unlocked.
        /// </summary>
        private void UpdateLockedState()
        {
            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(!isUnlocked);
            }
            
            if (iconImage != null)
            {
                iconImage.color = isUnlocked ? Color.white : animationManager.GetLockedColor();
            }
            
            // Can only interact if unlocked
            interactable = isUnlocked;
        }
        
        /// <summary>
        /// Called when hovering over unlock slot.
        /// </summary>
        public override void OnSelect(BaseEventData eventData)
        {
            if (!isUnlocked) return;
            
            isHovered = true;
            UpdateVisuals();
            
            // Animate selection
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform, borderImage)
                    .AnimateSelection();
            }
            
            // Notify for description display
            onUnlockHovered?.Invoke(unlockIndex);
            
            UIAudio.PlayHover();
        }
        
        /// <summary>
        /// Called when deselecting unlock slot.
        /// </summary>
        public override void OnDeselect(BaseEventData eventData)
        {
            isHovered = false;
            UpdateVisuals();
            
            // Animate deselection
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform, borderImage)
                    .AnimateDeselection();
            }
        }
        
        /// <summary>
        /// Called when user presses A (currently just for feedback).
        /// </summary>
        public void OnSubmit(BaseEventData eventData)
        {
            if (!isUnlocked) return;
            
            // Could trigger additional info or video playback
            UIAudio.PlaySelect();
        }
        
        private void UpdateVisuals()
        {
            if (!isUnlocked) return;
            
            if (animationManager == null)
            {
                animationManager = UIAnimationManager.Instance;
                if (animationManager == null) return;
            }
            
            Color targetColor = isHovered ? animationManager.GetSelectedColor() : animationManager.GetNormalColor();
            
            if (borderImage != null)
                borderImage.color = targetColor;
        }
        
        /// <summary>
        /// Gets the unlock name.
        /// </summary>
        public string GetUnlockName() => unlockName;
        
        /// <summary>
        /// Gets the unlock description.
        /// </summary>
        public string GetDescription() => description;
        
        /// <summary>
        /// Gets whether this unlock is available.
        /// </summary>
        public bool IsUnlocked() => isUnlocked;
    }
}

