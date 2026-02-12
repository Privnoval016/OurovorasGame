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
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private float selectedScale = 1.1f;
        [SerializeField] private float animationDuration = 0.2f;
        
        [Header("Events")]
        public UnityEvent onClick = new UnityEvent();
        
        private Vector3 originalScale;
        
        protected override void Awake()
        {
            originalScale = transform.localScale;
        }
        
        public override void OnSelect(BaseEventData eventData)
        {
            // Scale up
            Tween.Scale(transform, originalScale * selectedScale, animationDuration, Ease.OutBack, useUnscaledTime: true);
            
            // Change color
            if (borderImage != null)
                Tween.Color(borderImage, selectedColor, animationDuration, useUnscaledTime: true);
            if (backgroundImage != null)
                Tween.Color(backgroundImage, selectedColor * 0.3f, animationDuration, useUnscaledTime: true);
            
            UIAudio.PlayHover();
        }
        
        public override void OnDeselect(BaseEventData eventData)
        {
            // Scale down
            Tween.Scale(transform, originalScale, animationDuration, Ease.OutQuad, useUnscaledTime: true);
            
            // Reset color
            if (borderImage != null)
                Tween.Color(borderImage, normalColor, animationDuration, useUnscaledTime: true);
            if (backgroundImage != null)
                Tween.Color(backgroundImage, normalColor * 0.1f, animationDuration, useUnscaledTime: true);
        }
        
        public void OnSubmit(BaseEventData eventData)
        {
            // Handle A button press - trigger onClick event
            UIAudio.PlaySelect();
            onClick?.Invoke();
        }
    }
}

