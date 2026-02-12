using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Extensions.UI
{
    /// <summary>
    /// Custom button component for action menus (Use/Discard/etc).
    /// Uses UIAnimationManager for consistent visual feedback.
    /// Inherits from Selectable for controller navigation.
    /// </summary>
    public class ActionMenuButton : Selectable, ISubmitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI buttonText;
        
        [Header("Events")]
        [SerializeField] private UnityEvent onButtonPressed;
        
        private UIAnimationManager animationManager;
        
        protected override void Awake()
        {
            base.Awake();
            animationManager = UIAnimationManager.Instance;
        }
        
        /// <summary>
        /// Sets the button text.
        /// </summary>
        public void SetText(string text)
        {
            if (buttonText != null)
                buttonText.text = text;
        }
        
        /// <summary>
        /// Adds a listener to the button pressed event.
        /// </summary>
        public void AddListener(UnityAction action)
        {
            onButtonPressed.AddListener(action);
        }
        
        /// <summary>
        /// Removes all listeners from the button.
        /// </summary>
        public void RemoveAllListeners()
        {
            onButtonPressed.RemoveAllListeners();
        }
        
        #region Selectable Overrides
        
        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            
            if (!interactable) return;
            
            // Animate selection
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform, borderImage, backgroundImage)
                    .AnimateSelection();
            }
            
            UpdateTextColor();
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
            
            UpdateTextColor();
        }
        
        public void OnSubmit(BaseEventData eventData)
        {
            if (!interactable) return;
            
            // Punch animation on submit
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform)
                    .AnimatePunch();
            }
            
            onButtonPressed?.Invoke();
            UIAudio.PlaySelect();
        }
        
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            UpdateTextColor();
        }
        
        #endregion
        
        private void UpdateTextColor()
        {
            if (animationManager == null)
            {
                animationManager = UIAnimationManager.Instance;
                if (animationManager == null) return;
            }
            
            if (buttonText == null) return;
            
            Color textColor;
            
            if (!interactable)
            {
                textColor = animationManager.GetEmptyTextColor();
            }
            else if (currentSelectionState == SelectionState.Selected || 
                     currentSelectionState == SelectionState.Pressed)
            {
                textColor = animationManager.GetSelectedTextColor();
            }
            else
            {
                textColor = animationManager.GetNormalTextColor();
            }
            
            buttonText.color = textColor;
        }
    }
}

