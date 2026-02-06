using System;
using PrimeTween;
using UnityEngine;

namespace Extensions.UI
{
    /// <summary>
    /// Base class for tab content panels with smooth PrimeTween animations.
    /// All animations use unscaled time since the menu pauses the game (timeScale = 0).
    /// </summary>
    public class TabSelection : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.15f;
        [SerializeField] private Ease fadeInEase = Ease.OutQuad;
        [SerializeField] private Ease fadeOutEase = Ease.InQuad;
        
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        
        private void Awake()
        {
            // Ensure we have a CanvasGroup for fading
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            rectTransform = GetComponent<RectTransform>();
            
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when the tab is selected. Override to add custom behavior.
        /// </summary>
        public virtual void OnTabSelect()
        {
            gameObject.SetActive(true);
            AnimateIn();
        }

        /// <summary>
        /// Called when the tab is deselected. Override to add custom behavior.
        /// </summary>
        public virtual void OnTabDeselect()
        {
            AnimateOut();
        }
        
        /// <summary>
        /// Animates the tab content in with a smooth fade and slight scale.
        /// </summary>
        protected virtual void AnimateIn()
        {
            if (canvasGroup == null) return;
            
            // Start invisible and slightly scaled down
            canvasGroup.alpha = 0f;
            if (rectTransform != null)
                rectTransform.localScale = Vector3.one * 0.95f;
            
            // Fade in
            Tween.Alpha(canvasGroup, 1f, duration: fadeInDuration, 
                ease: fadeInEase, useUnscaledTime: true);
            
            // Scale up to normal with slight overshoot
            if (rectTransform != null)
            {
                Tween.Scale(rectTransform, 1f, duration: fadeInDuration, 
                    ease: Ease.OutBack, useUnscaledTime: true);
            }
        }
        
        /// <summary>
        /// Animates the tab content out with a quick fade.
        /// </summary>
        protected virtual void AnimateOut()
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            // Fade out
            Tween.Alpha(canvasGroup, 0f, duration: fadeOutDuration, 
                ease: fadeOutEase, useUnscaledTime: true)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}

