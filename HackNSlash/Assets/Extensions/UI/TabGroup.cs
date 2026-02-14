using System;
using System.Collections.Generic;
using PrimeTween;  // Used for tab swipe animations
using UnityEngine;
using UnityEngine.InputSystem;

namespace Extensions.UI
{
    /// <summary>
    /// Manages tab navigation and switching with smooth PrimeTween animations.
    /// Uses EventBus for audio to avoid singleton dependencies.
    /// </summary>
    public class TabGroup : MonoBehaviour
    {
    [Header("Tabs")] public List<TabButton> tabButtons;
    public TabButton selectedTab;
    private int SelectedTabIndex => tabButtons.IndexOf(selectedTab);

    [Header("Input Actions")] 
    public bool tabActive = true;
    
    [Header("Tab Animation")]
    [SerializeField] private bool enableTabSwipe = true;
    [SerializeField] private float swipeDistance = 1920f;
    [SerializeField] private float swipeDuration = 0.3f;
    [SerializeField] private float fadeDuration = 0.25f;
    
    public Action<int, int> OnTabSwitched = delegate { }; // (oldIndex, newIndex)

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            InputManager.Instance.onTabLeft += OnTabLeft;
            InputManager.Instance.onTabRight += OnTabRight;
        }

        private void Start()
        {
            if (tabButtons == null || tabButtons.Count == 0) return;

            // Select the first tab by default
            
            selectedTab = tabButtons[0];
            selectedTab.Select();
        }

        #endregion
        
        #region Tab Management
        
        /// <summary>
        /// Programmatically selects a specific tab.
        /// Called by TabButton when clicked or by external code.
        /// </summary>
        /// <param name="tab">The tab button to select.</param>
        /// <param name="force">If true, forces the selection even if already on that tab.</param>
        public void SelectTab(TabButton tab, bool force = false)
        {
            if (tab == null || !tabButtons.Contains(tab)) return;
            
            int oldIndex = selectedTab != null ? tabButtons.IndexOf(selectedTab) : 0;
            int newIndex = tabButtons.IndexOf(tab);

            SwitchTabs(oldIndex, newIndex, force);
        }
        
        /// <summary>
        /// Programmatically selects a tab by index.
        /// </summary>
        /// <param name="index">The tab index (0-based).</param>
        /// <param name="force">If true, forces the selection even if already on that tab.</param>
        public void SelectTabByIndex(int index, bool force = false)
        {
            if (index < 0 || index >= tabButtons.Count) return;
            SelectTab(tabButtons[index], force);
        }
        
        #endregion

        #region Input Callbacks

    private void OnTabLeft(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!tabActive) return;
        if (tabButtons.Count == 0) return;

        int oldIndex = SelectedTabIndex;
        int newIndex = oldIndex <= 0 ? tabButtons.Count - 1 : oldIndex - 1;
        
        SwitchTabs(oldIndex, newIndex);
    }

    private void OnTabRight(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!tabActive) return;
        if (tabButtons.Count == 0) return;

        int oldIndex = SelectedTabIndex;
        int newIndex = oldIndex >= tabButtons.Count - 1 ? 0 : oldIndex + 1;
        
        SwitchTabs(oldIndex, newIndex);
    }
    
    private void SwitchTabs(int oldIndex, int newIndex, bool force = false)
    {
        if (oldIndex == newIndex && !force) return; // No change
        
        OnTabSwitched(oldIndex, newIndex);
        
        bool swipingRight = newIndex > oldIndex;
        TabButton oldTab = tabButtons[oldIndex];
        TabButton newTab = tabButtons[newIndex];
        
        Debug.Log($"TabGroup: Switching from tab {oldIndex} to {newIndex}, swipingRight={swipingRight}");
        
        // Get TabSelection components
        TabSelection oldContent = oldTab.contentPanel;
        TabSelection newContent = newTab.contentPanel;
        
        if (oldContent == null || newContent == null)
        {
            Debug.LogError("TabGroup: Tab content panels don't have TabSelection component!");
            return;
        }
        
        // Activate new tab immediately (but invisible)
        newContent.gameObject.SetActive(true);
        
        // Get components for animation
        var oldCanvasGroup = oldContent.GetComponent<CanvasGroup>();
        var newCanvasGroup = newContent.GetComponent<CanvasGroup>();
        var oldRect = oldContent.GetComponent<RectTransform>();
        var newRect = newContent.GetComponent<RectTransform>();
        
        if (enableTabSwipe && oldRect != null && newRect != null)
        {
            // Setup positions - use anchoredPosition for UI RectTransforms
            float oldEndX = swipingRight ? -swipeDistance : swipeDistance;
            float newStartX = swipingRight ? swipeDistance : -swipeDistance;
            
            // Ensure new tab starts at correct position
            newRect.anchoredPosition = new Vector2(newStartX, 0f);
            if (newCanvasGroup != null) newCanvasGroup.alpha = 0f;
            
            Debug.Log($"  Old tab sliding to X={oldEndX}, New tab sliding from X={newStartX} to 0");
            
            // Animate OLD tab out
            if (oldCanvasGroup != null)
            {
                Tween.Alpha(oldCanvasGroup, 0f, fadeDuration * 0.7f, Ease.InQuad, useUnscaledTime: true);
            }
            Tween.UIAnchoredPosition(oldRect, new Vector2(oldEndX, 0f), swipeDuration * 0.7f, Ease.InCubic, useUnscaledTime: true)
                .OnComplete(() => 
                {
                    oldContent.gameObject.SetActive(false);
                    oldRect.anchoredPosition = Vector2.zero; // Reset position
                });
            
            // Animate NEW tab in
            if (newCanvasGroup != null)
            {
                newCanvasGroup.alpha = 0f;
                Tween.Alpha(newCanvasGroup, 1f, fadeDuration, Ease.OutQuad, useUnscaledTime: true);
            }
            Tween.UIAnchoredPosition(newRect, Vector2.zero, swipeDuration, Ease.OutCubic, useUnscaledTime: true);
        }
        else
        {
            // No animation - just switch
            oldContent.gameObject.SetActive(false);
            newContent.gameObject.SetActive(true);
        }
        
        // Update selection
        selectedTab?.Deselect();
        selectedTab = newTab;
        selectedTab?.Select();
        
        UIAudio.PlayTabSwitch();
    }

        #endregion

        #region Tab Button Methods


        public void Subscribe(TabButton button)
        {
            if (tabButtons == null)
                tabButtons = new List<TabButton>();

            if (!tabButtons.Contains(button))
                tabButtons.Add(button);
        }

        #endregion
    }
}
