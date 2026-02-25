using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Custom selectable button for settings menu navigation.
    /// Uses UIAnimationManager for consistent visual feedback.
    /// </summary>
    public class SettingsButton : Selectable, ISubmitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image iconImage;
        
        [Header("Event")]
        public UnityEngine.Events.UnityEvent onPressed;
        
        private UIAnimationManager animationManager;
        
        protected override void Awake()
        {
            base.Awake();
            animationManager = UIAnimationManager.Instance;
        }
        
        /// <summary>
        /// Initializes the button with label and optional icon.
        /// </summary>
        public void Initialize(string label, Sprite icon = null)
        {
            if (labelText != null)
                labelText.text = label;
            
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
            
            if (animationManager == null)
                animationManager = UIAnimationManager.Instance;
            
            UpdateVisuals();
        }
        
        #region Selectable Overrides
        
        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform, borderImage, backgroundImage)
                    .AnimateSelection();
            }
            
            UIAudio.PlayHover();
        }
        
        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform, borderImage, backgroundImage)
                    .AnimateDeselection();
            }
            
            UpdateVisuals();
        }
        
        public void OnSubmit(BaseEventData eventData)
        {
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform)
                    .AnimatePunch();
            }
            
            onPressed?.Invoke();
            UIAudio.PlaySelect();
        }
        
        #endregion
        
        private void UpdateVisuals()
        {
            if (animationManager == null)
            {
                animationManager = UIAnimationManager.Instance;
                if (animationManager == null) return;
            }
            
            Color normalColor = animationManager.GetNormalColor();
            
            if (backgroundImage != null)
                backgroundImage.color = normalColor;
            
            if (borderImage != null)
                borderImage.color = normalColor;
            
            if (labelText != null)
                labelText.color = animationManager.GetNormalTextColor();
        }
    }
}

