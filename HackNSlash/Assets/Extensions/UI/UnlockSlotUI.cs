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
    public class UnlockSlotUI : Selectable, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private Image lockOverlay;
        [SerializeField] private TextMeshProUGUI levelText;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private float selectedScale = 1.1f;
        [SerializeField] private float animationDuration = 0.15f;
        
        [Header("Events")]
        public UnityEvent<int> onUnlockHovered; // For showing description
        
        private int unlockIndex;
        private string unlockName;
        private string description;
        private int requiredLevel;
        private bool isUnlocked;
        private bool isHovered = false;
        private Vector3 originalScale;
        
        protected override void Awake()
        {
            base.Awake();
            originalScale = transform.localScale;
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
                iconImage.color = isUnlocked ? Color.white : lockedColor;
            }
            
            // Can only interact if unlocked
            interactable = isUnlocked;
        }
        
        /// <summary>
        /// Called when hovering over unlock slot.
        /// </summary>
        public void OnSelect(BaseEventData eventData)
        {
            if (!isUnlocked) return;
            
            isHovered = true;
            UpdateVisuals();
            
            // Scale up animation
            Tween.Scale(transform, originalScale * selectedScale, animationDuration, Ease.OutBack, useUnscaledTime: true);
            
            // Notify for description display
            onUnlockHovered?.Invoke(unlockIndex);
            
            UIAudio.PlayHover();
        }
        
        /// <summary>
        /// Called when deselecting unlock slot.
        /// </summary>
        public void OnDeselect(BaseEventData eventData)
        {
            isHovered = false;
            UpdateVisuals();
            
            // Scale down animation
            Tween.Scale(transform, originalScale, animationDuration, Ease.OutQuad, useUnscaledTime: true);
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
            
            Color targetColor = isHovered ? selectedColor : normalColor;
            
            if (borderImage != null)
                Tween.Color(borderImage, targetColor, animationDuration, useUnscaledTime: true);
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

