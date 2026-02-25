using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Extensions.UI
{
    /// <summary>
    /// Simple visual feedback component for unlock grid button.
    /// Scales and changes color when selected.
    /// Add this to the unlock grid button GameObject.
    /// </summary>
    public class UnlockGridButtonVisual : Selectable, ISubmitHandler
    {
        [Header("Visual Settings")]
        [SerializeField] private Image borderImage;
        [SerializeField] private Image backgroundImage;
        
        private UIAnimationManager animationManager;
        
        [Header("Events")]
        public UnityEvent onClick = new UnityEvent();
        
        private Vector3 originalScale;
        
        protected override void Awake()
        {
            originalScale = transform.localScale;
            animationManager = UIAnimationManager.Instance;
        }
        
        public override void OnSelect(BaseEventData eventData)
        {
            borderImage.color = animationManager.GetSelectedColor();
            
            animationManager.CreateBuilder(borderImage.transform).AnimateSelection();
            
            UIAudio.PlayHover();
        }
        
        public override void OnDeselect(BaseEventData eventData)
        {
            borderImage.color = animationManager.GetNormalColor();
            
            animationManager.CreateBuilder(borderImage.transform).AnimateDeselection();
        }
        
        public void OnSubmit(BaseEventData eventData)
        {
            // Handle A button press - trigger onClick event
            UIAudio.PlaySelect();
            onClick?.Invoke();
        }
    }
}

