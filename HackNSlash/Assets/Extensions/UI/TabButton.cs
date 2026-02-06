using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Individual tab button with smooth PrimeTween animations.
    /// Provides visual feedback when selected/deselected with unscaled time animations.
    /// </summary>
    public class TabButton : MonoBehaviour
    {
        public TabGroup tabGroup;
        public bool isSelected;
        public TabSelection contentPanel;
        
        [Header("Animation Settings")]
        [SerializeField] private float selectDuration = 0.2f;
        [SerializeField] private float deselectDuration = 0.15f;
        [SerializeField] private float selectedScale = 1.1f;
        [SerializeField] private Ease selectEase = Ease.OutBack;
        [SerializeField] private Ease deselectEase = Ease.OutQuad;
        
        private Image buttonImage;
        private RectTransform rectTransform;

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            buttonImage = GetComponent<Image>();
            
            if (tabGroup == null)
            {
                tabGroup = GetComponentInParent<TabGroup>();
            }

            tabGroup?.Subscribe(this);
            Deselect();
        }

        #endregion

        /// <summary>
        /// Selects this tab with a smooth scale animation.
        /// </summary>
        public void Select()
        {
            isSelected = true;
            
            // Animate scale up
            if (rectTransform != null)
            {
                Tween.Scale(rectTransform, selectedScale, duration: selectDuration, 
                    ease: selectEase, useUnscaledTime: true);
            }
            
            // Play select sound via EventBus
            UIAudio.PlaySelect();
            
            OnTabSelect();
        }

        /// <summary>
        /// Deselects this tab with a smooth scale animation.
        /// </summary>
        public void Deselect()
        {
            isSelected = false;
            
            // Animate scale down
            if (rectTransform != null)
            {
                Tween.Scale(rectTransform, 1f, duration: deselectDuration, 
                    ease: deselectEase, useUnscaledTime: true);
            }
            
            OnTabDeselect();
        }
        
        /// <summary>
        /// Called when this tab is selected.
        /// </summary>
        protected virtual void OnTabSelect()
        {
            contentPanel?.OnTabSelect();
        }
        
        /// <summary>
        /// Called when this tab is deselected.
        /// </summary>
        protected virtual void OnTabDeselect()
        {
            contentPanel?.OnTabDeselect();
        }
    }
}
