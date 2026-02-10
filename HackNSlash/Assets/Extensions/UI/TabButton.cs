using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Individual tab button with smooth PrimeTween animations.
    /// Provides visual feedback when selected/deselected with unscaled time animations.
    /// Automatically sets up Button component and navigation in code.
    /// </summary>
    public class TabButton : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The TabGroup managing all tabs. Auto-found if not assigned.")]
        public TabGroup tabGroup;
        
        [Tooltip("The content panel this tab controls. Must be assigned in Inspector.")]
        public TabSelection contentPanel;
        
        [Header("Navigation (Auto-Setup in Code)")]
        [Tooltip("Previous tab button in the sequence. Used to setup horizontal navigation automatically.")]
        [SerializeField] private TabButton previousTabButton;
        
        [Tooltip("Next tab button in the sequence. Used to setup horizontal navigation automatically.")]
        [SerializeField] private TabButton nextTabButton;
        
        [Tooltip("First selectable element in this tab's content. Auto-selected when tab opens.")]
        [SerializeField] private Selectable firstSelectableInContent;
        
        [Header("Animation Settings")]
        [Tooltip("Duration of the scale-up animation when tab is selected.")]
        [SerializeField] private float selectDuration = 0.2f;
        
        [Tooltip("Duration of the scale-down animation when tab is deselected.")]
        [SerializeField] private float deselectDuration = 0.15f;
        
        [Tooltip("Target scale when tab is selected (1.1 = 10% larger).")]
        [SerializeField] private float selectedScale = 1.1f;
        
        [Tooltip("Easing curve for select animation. OutBack adds overshoot for snappy feel.")]
        [SerializeField] private Ease selectEase = Ease.OutBack;
        
        [Tooltip("Easing curve for deselect animation. OutQuad is smooth and quick.")]
        [SerializeField] private Ease deselectEase = Ease.OutQuad;
        
        [Header("Visual Settings")]
        [Tooltip("Normal color when tab is not selected.")]
        [SerializeField] private Color normalColor = Color.white;
        
        [Tooltip("Color when controller is hovering over tab.")]
        [SerializeField] private Color highlightedColor = new Color(0.78f, 0.78f, 0.78f, 1f); // #C8C8C8
        
        [Tooltip("Color when this tab is the active/selected tab.")]
        [SerializeField] private Color selectedColor = Color.yellow;
        
        [Tooltip("Color when tab is being pressed.")]
        [SerializeField] private Color pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // #808080
        
        [HideInInspector] public bool isSelected;
        
        private Button button;
        private Image buttonImage;
        private RectTransform rectTransform;

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            buttonImage = GetComponent<Image>();
            
            // Get or add Button component (but disable it - tabs use bumpers only)
            button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }
            
            // Disable button interactivity - tabs are controlled via bumpers only
            button.interactable = false;
            button.transition = Selectable.Transition.None;
            
            // Disable navigation on tab buttons
            Navigation nav = new Navigation();
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            
            if (tabGroup == null)
            {
                tabGroup = GetComponentInParent<TabGroup>();
            }

            tabGroup?.Subscribe(this);
            Deselect();
        }

        #endregion

        /// <summary>
        /// Selects this tab with a smooth scale animation and color change.
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
            
            // Change color to selected
            if (buttonImage != null)
            {
                Tween.Color(buttonImage, selectedColor, duration: selectDuration, 
                    ease: Ease.OutQuad, useUnscaledTime: true);
            }
            
            // Play select sound via EventBus
            UIAudio.PlaySelect();
            
            OnTabSelect();
        }

        /// <summary>
        /// Deselects this tab with a smooth scale animation and color change.
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
            
            // Change color back to normal
            if (buttonImage != null)
            {
                Tween.Color(buttonImage, normalColor, duration: deselectDuration, 
                    ease: Ease.OutQuad, useUnscaledTime: true);
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
