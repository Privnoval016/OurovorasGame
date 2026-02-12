using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Extensions.UI
{
    /// <summary>
    /// UI component for displaying an attack button slot (X, Y, A on Xbox controller).
    /// Shows button icon and assigned attack name with visual feedback.
    /// Uses UIAnimationManager for consistent animations.
    /// </summary>
    public class AttackButtonSlotUI : Selectable, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image buttonIconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI attackNameText;
        
        [Header("Events")]
        [SerializeField] private UnityEvent<int> onButtonSelected; // Passes button index
        [SerializeField] private UnityEvent<int> onButtonHovered; // For showing description
        
        private int buttonIndex;
        private AttackDisplayData currentAttack;
        private bool isHovered = false;
        private UIAnimationManager animationManager;
        
        protected override void Awake()
        {
            base.Awake();
            animationManager = UIAnimationManager.Instance;
        }
        
        /// <summary>
        /// Initializes the button slot with index and icon.
        /// </summary>
        public void Initialize(int index, Sprite buttonIcon)
        {
            buttonIndex = index;
            
            if (animationManager == null)
                animationManager = UIAnimationManager.Instance;
            
            if (buttonIconImage != null && buttonIcon != null)
            {
                buttonIconImage.sprite = buttonIcon;
            }
            
            SetEmpty();
        }
        
        // ...existing SetAttack, SetEmpty, GetButtonIndex, GetCurrentAttack methods...
        
        /// <summary>
        /// Sets the assigned attack data.
        /// </summary>
        public void SetAttack(AttackDisplayData attack)
        {
            currentAttack = attack;
            
            if (attackNameText != null)
            {
                if (attack == null || string.IsNullOrEmpty(attack.attackName))
                {
                    attackNameText.text = "Unassigned";
                    attackNameText.color = animationManager.GetEmptyColor();
                }
                else
                {
                    attackNameText.text = attack.attackName;
                    attackNameText.color = animationManager.GetNormalColor();
                }
            }
        }
        
        /// <summary>
        /// Sets the slot to empty/unassigned state.
        /// </summary>
        public void SetEmpty()
        {
            currentAttack = null;
            
            if (attackNameText != null)
            {
                attackNameText.text = "Unassigned";
                attackNameText.color = animationManager.GetEmptyColor();
            }
        }
        
        /// <summary>
        /// Called when hovering over button (controller navigation).
        /// </summary>
        public override void OnSelect(BaseEventData eventData)
        {
            isHovered = true;
            UpdateVisuals();
            
            // Animate selection using animation manager
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform, borderImage, backgroundImage)
                    .AnimateSelection();
            }
            
            // Notify for description display
            onButtonHovered?.Invoke(buttonIndex);
            
            UIAudio.PlayHover();
        }
        
        /// <summary>
        /// Called when deselecting button.
        /// </summary>
        public override void OnDeselect(BaseEventData eventData)
        {
            isHovered = false;
            UpdateVisuals();
            
            // Animate deselection using animation manager
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform, borderImage, backgroundImage)
                    .AnimateDeselection();
            }
        }
        
        /// <summary>
        /// Called when user presses A to reassign attack.
        /// </summary>
        public void OnSubmit(BaseEventData eventData)
        {
            // Punch animation on submit
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform)
                    .AnimatePunch();
            }
            
            onButtonSelected?.Invoke(buttonIndex);
            UIAudio.PlaySelect();
        }
        
        private void UpdateVisuals()
        {
            if (animationManager == null)
            {
                animationManager = UIAnimationManager.Instance;
                if (animationManager == null) return;
            }
            
            Color targetColor = isHovered ? animationManager.GetSelectedColor() : animationManager.GetNormalColor();
            Color textColor = currentAttack == null ? animationManager.GetSelectedTextColor() : animationManager.GetNormalTextColor();
            
            if (borderImage != null)
                borderImage.color = targetColor;
            
            if (attackNameText != null)
                attackNameText.color = textColor;
        }
        
        /// <summary>
        /// Gets the button index.
        /// </summary>
        public int GetButtonIndex() => buttonIndex;
        
        /// <summary>
        /// Gets the currently assigned attack.
        /// </summary>
        public AttackDisplayData GetCurrentAttack() => currentAttack;
    }
}

