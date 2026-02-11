using TMPro;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Displays item/attack/unlock name and description.
    /// Positioned under video player in Element Progress tab.
    /// Smoothly fades in/out when content changes.
    /// </summary>
    public class DescriptionDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.2f;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;
        
        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Start hidden
            canvasGroup.alpha = 0f;
        }
        
        /// <summary>
        /// Shows attack information.
        /// </summary>
        public void ShowAttack(AttackDisplayData attack)
        {
            if (attack == null || string.IsNullOrEmpty(attack.attackName))
            {
                Hide();
                return;
            }
            
            ShowContent(attack.attackName, attack.description);
        }
        
        /// <summary>
        /// Shows unlock information.
        /// </summary>
        public void ShowUnlock(string unlockName, string description)
        {
            if (string.IsNullOrEmpty(unlockName))
            {
                Hide();
                return;
            }
            
            ShowContent(unlockName, description);
        }
        
        /// <summary>
        /// Shows generic content.
        /// </summary>
        public void ShowContent(string title, string description)
        {
            if (titleText != null)
                titleText.text = title;
            
            if (descriptionText != null)
                descriptionText.text = description ?? "";
            
            // Fade in
            Tween.Alpha(canvasGroup, 1f, fadeDuration, fadeEase, useUnscaledTime: true);
        }
        
        /// <summary>
        /// Hides the description display.
        /// </summary>
        public void Hide()
        {
            Tween.Alpha(canvasGroup, 0f, fadeDuration, fadeEase, useUnscaledTime: true);
        }
    }
}

