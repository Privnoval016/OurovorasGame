using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Extensions.UI
{
    /// <summary>
    /// Custom button component for category selection in inventory.
    /// Uses UIAnimationManager for consistent visual feedback.
    /// Inherits from Selectable for controller navigation.
    /// </summary>
    public class CategoryButton : Selectable, ISubmitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI categoryText;
        
        [Header("Events")]
        [SerializeField] private UnityEvent<string> onCategorySelected;
        
        private string categoryName;
        private bool isCurrentCategory;
        private UIAnimationManager animationManager;
        
        protected override void Awake()
        {
            base.Awake();
            animationManager = UIAnimationManager.Instance;
        }
        
        /// <summary>
        /// Initializes the button with category name.
        /// </summary>
        public void Initialize(string category)
        {
            categoryName = category;
            
            if (animationManager == null)
                animationManager = UIAnimationManager.Instance;
            
            if (categoryText != null)
                categoryText.text = categoryName;
            
            UpdateVisuals();
        }
        
        /// <summary>
        /// Sets whether this category is currently selected.
        /// </summary>
        public void SetAsCurrentCategory(bool isCurrent)
        {
            isCurrentCategory = isCurrent;
            UpdateVisuals();
        }
        
        /// <summary>
        /// Gets the category name.
        /// </summary>
        public string GetCategoryName() => categoryName;
        
        /// <summary>
        /// Adds a listener to the category selected event.
        /// </summary>
        public void AddListener(UnityEngine.Events.UnityAction action)
        {
            // Wrap the action to pass the category name
            onCategorySelected.AddListener(_ => action?.Invoke());
        }
        
        #region Selectable Overrides
        
        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            
            // Animate selection
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
            
            // Animate deselection
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform, borderImage, backgroundImage)
                    .AnimateDeselection();
            }
            
            UpdateVisuals();
        }
        
        public void OnSubmit(BaseEventData eventData)
        {
            // Punch animation on submit
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform)
                    .AnimatePunch();
            }
            
            onCategorySelected?.Invoke(categoryName);
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
            
            // If this is the current category, use selected color for text
            Color textColor = isCurrentCategory 
                ? animationManager.GetSelectedTextColor() 
                : animationManager.GetNormalTextColor();
            
            if (categoryText != null)
                categoryText.color = textColor;
        }
    }
}

