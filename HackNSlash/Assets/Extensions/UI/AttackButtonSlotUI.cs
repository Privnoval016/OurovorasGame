using TMPro;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Extensions.UI
{
    /// <summary>
    /// UI component for displaying an attack button slot (X, Y, A on Xbox controller).
    /// Shows button icon and assigned attack name with visual feedback.
    /// Completely modular and reusable.
    /// </summary>
    public class AttackButtonSlotUI : Selectable, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image buttonIconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI attackNameText;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private float selectedScale = 1.15f;
        [SerializeField] private float animationDuration = 0.15f;
        
        [Header("Events")]
        [SerializeField] private UnityEvent<int> onButtonSelected; // Passes button index
        [SerializeField] private UnityEvent<int> onButtonHovered; // For showing description
        
        private int buttonIndex;
        private AttackDisplayData currentAttack; // west = X (1), north = Y (0), south = A (2)
        private bool isHovered = false;
        private Vector3 originalScale;
        
        protected override void Awake()
        {
            base.Awake();
            originalScale = transform.localScale;
        }
        
        /// <summary>
        /// Initializes the button slot with index and icon.
        /// </summary>
        public void Initialize(int index, Sprite buttonIcon)
        {
            buttonIndex = index;
            
            if (buttonIconImage != null && buttonIcon != null)
            {
                buttonIconImage.sprite = buttonIcon;
            }
            
            SetEmpty();
        }
        
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
                    attackNameText.color = Color.gray;
                }
                else
                {
                    attackNameText.text = attack.attackName;
                    attackNameText.color = normalColor;
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
                attackNameText.color = Color.gray;
            }
        }
        
        /// <summary>
        /// Called when hovering over button (controller navigation).
        /// </summary>
        public override void OnSelect(BaseEventData eventData)
        {
            isHovered = true;
            UpdateVisuals();
            
            // Scale up animation
            Tween.Scale(transform, originalScale * selectedScale, animationDuration, Ease.OutBack, useUnscaledTime: true);
            
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
            
            // Scale down animation
            Tween.Scale(transform, originalScale, animationDuration, Ease.OutQuad, useUnscaledTime: true);
        }
        
        /// <summary>
        /// Called when user presses A to reassign attack.
        /// </summary>
        public void OnSubmit(BaseEventData eventData)
        {
            onButtonSelected?.Invoke(buttonIndex);
            UIAudio.PlaySelect();
        }
        
        private void UpdateVisuals()
        {
            Color targetColor = isHovered ? selectedColor : normalColor;
            
            if (borderImage != null)
                Tween.Color(borderImage, targetColor, animationDuration, useUnscaledTime: true);
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

