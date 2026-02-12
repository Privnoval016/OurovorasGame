using TMPro;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Extensions.UI
{
    /// <summary>
    /// UI component for displaying an element selection slot.
    /// Shows element icon/name and supports controller navigation with visual feedback.
    /// Completely modular and reusable.
    /// </summary>
    public class ElementSlotUI : Selectable, ISubmitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI nameText;
        
        [Header("Events")]
        [SerializeField] private UnityEvent<int> onElementSelected;
        [SerializeField] private UnityEvent<int> onElementHovered; // NEW: For updating UI without transitioning
        
        private UIAnimationManager animationManager;
        
        private ElementEffect element;
        private int elementIndex;
        private bool isSelected = false;
        private Vector3 originalScale;
        
        protected override void Awake()
        {
            base.Awake();
            originalScale = transform.localScale;
            
            animationManager = UIAnimationManager.Instance;
        }
        
        /// <summary>
        /// Initializes the slot with element data.
        /// </summary>
        public void Initialize(ElementEffect elementData, int index, Sprite icon)
        {
            element = elementData;
            elementIndex = index;
            
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }
            
            if (nameText != null)
                nameText.text = elementData.ToString();
            
            UpdateVisuals();
        }
        
        /// <summary>
        /// Called when element slot is selected with controller.
        /// </summary>
        public override void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            UpdateVisuals();
            
            animationManager.CreateBuilder(borderImage.transform).AnimateSelection();
            
            // Fire hover event to update UI without transitioning
            onElementHovered?.Invoke(elementIndex);
            
            UIAudio.PlayHover();
        }
        
        /// <summary>
        /// Called when element slot is deselected.
        /// </summary>
        public override void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            UpdateVisuals();
            
            animationManager.CreateBuilder(borderImage.transform).AnimateDeselection();
        }
        
        /// <summary>
        /// Called when user presses A button to confirm selection.
        /// </summary>
        public void OnSubmit(BaseEventData eventData)
        {
            animationManager.CreateBuilder(borderImage.transform).AnimatePunch();
            onElementSelected?.Invoke(elementIndex);
            UIAudio.PlaySelect();
        }
        
        private void UpdateVisuals()
        {
            Color targetColor = isSelected ? animationManager.GetSelectedColor() : animationManager.GetNormalColor();
            Color nameColor = isSelected ? animationManager.GetSelectedTextColor() : animationManager.GetNormalTextColor();
            
            if (borderImage != null)
                borderImage.color = targetColor;
            
            if (nameText != null)
                nameText.color = nameColor;
        }
        
        /// <summary>
        /// Gets the element this slot represents.
        /// </summary>
        public ElementEffect GetElement() => element;
    }
}

