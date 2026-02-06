using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions.Timers;
using UnityEngine;
using UnityEngine.InputSystem;
using PrimeTween;

namespace Extensions.UI
{
    /// <summary>
    /// Defines the axis of movement for the scroll menu.
    /// </summary>
    public enum MovementAxis { Horizontal, Vertical }

    /// <summary>
    /// Defines how the menu behaves at boundaries.
    /// </summary>
    public enum CycleMode
    {
        CircularStop,   // Circular wrapping only when items >= panels, otherwise stop at edges
        CircularPure,   // True circular carousel - items wrap visually in a continuous loop
        Restart,        // Stop at edges, but jump back to start when going past the end
        Stop           // Stop at first/last item, no wrapping or jumping
    }

    /// <summary>
    /// A modular, reusable scrolling menu component for displaying and navigating through items.
    /// Completely decoupled from game-specific logic and usable across different projects.
    /// </summary>
    public class ScrollMenu : MonoBehaviour
    {
        #region Inspector

        [Header("Scroll Settings")]
        [SerializeField] private MovementAxis axis;
        [SerializeField] private CycleMode cycleMode;

        [Tooltip("Whether to visually focus the center panel.")]
        [SerializeField] private bool focusCenterPanel = true;

        [SerializeField] private int centerPanelIndex = 2;

        [Header("Timing")]
        [SerializeField] private float scrollCooldown = 0.11f;
        [SerializeField] private float scrollDuration = 0.1f;
        
        [Header("Visuals")]
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private Vector3 focusedScale = Vector3.one * 1.15f;
        [SerializeField] private float scaleDuration = 0.08f;

        [Header("UI")]
        [SerializeField] private Transform scrollItemContainer;

        #endregion

        private LinkedList<ScrollUIPanel> panelQueue;
        private Vector3 slotDelta;

        private IList inventoryItems;
        private Func<object, ItemUIInfo> getItemInfoFunc;

        private CountdownTimer scrollCooldownTimer;
        private IScrollMenuAuthority authority;

        private int selectedIndex;
        private bool isAnimating;

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            var panels = scrollItemContainer
                .GetComponentsInChildren<ScrollUIPanel>(true).ToList();

            panelQueue = new LinkedList<ScrollUIPanel>(panels);

            if (panels.Count >= 1)
            {
                RectTransform rt = panels[0].rectTransform;

                slotDelta = axis == MovementAxis.Horizontal
                    ? new Vector3(rt.rect.width, 0f, 0f)
                    : new Vector3(0f, rt.rect.height, 0f);
            }

            scrollCooldownTimer = new CountdownTimer(scrollCooldown, true);
            scrollCooldownTimer.Stop();
        }

        #endregion

        #region Activation

        /// <summary>
        /// Activates the scroll menu with the provided items and configuration.
        /// </summary>
        /// <typeparam name="T">The type of items to display.</typeparam>
        /// <param name="items">The list of items to display.</param>
        /// <param name="initialIndex">The index of the initially selected item.</param>
        /// <param name="getInfoFunc">Function to convert items to UI display info.</param>
        /// <param name="authority">The authority that handles input subscription.</param>
        public void Activate<T>(List<T> items, int initialIndex, Func<T, 
            ItemUIInfo> getInfoFunc, IScrollMenuAuthority authority) where T : class
        {
            scrollCooldownTimer = new CountdownTimer(scrollCooldown, true);
            scrollCooldownTimer.Stop();
            
            if (items == null || items.Count == 0)
            {
                Debug.LogWarning("ScrollMenu: Cannot activate with null or empty items list.");
                return;
            }

            inventoryItems = items;
            getItemInfoFunc = o => getInfoFunc((T)o);

            selectedIndex = Mathf.Clamp(initialIndex, 0, items.Count - 1);

            this.authority = authority;
            this.authority.SubscribeToScroll(this);

            InitializePanels();
        }

        /// <summary>
        /// Deactivates the scroll menu and cleans up subscriptions.
        /// </summary>
        public void Deactivate()
        {
            if (authority != null)
            {
                authority.UnsubscribeFromScroll(this);
                authority = null;
            }

            inventoryItems = null;
            getItemInfoFunc = null;
            isAnimating = false;
            scrollCooldownTimer?.Stop();
        }

        #endregion

        #region Input Callbacks

        /// <summary>
        /// Handles scroll input from the input system.
        /// </summary>
        /// <param name="scrollDelta">The scroll direction and magnitude.</param>
        public void OnScrollPerformed(Vector2 scrollDelta)
        {
            if (isAnimating || scrollCooldownTimer.IsRunning || inventoryItems == null)
                return;

            Vector2 v = scrollDelta;
            float delta = axis == MovementAxis.Horizontal ? v.x : -v.y;

            if (Mathf.Approximately(delta, 0f))
                return;

            scrollCooldownTimer.Restart();
            scrollCooldownTimer.Start();

            if (delta > 0f)
                Scroll(+1);
            else
                Scroll(-1);
        }

        #endregion

        #region Scrolling

        /// <summary>
        /// Scrolls the menu in the specified direction.
        /// </summary>
        /// <param name="direction">The direction to scroll (1 for forward, -1 for backward).</param>
        private void Scroll(int direction)
        {
            int next = NextIndex(direction);
    
            // For Restart mode, if we're at the boundary and trying to go further, jump to opposite end
            if (cycleMode == CycleMode.Restart)
            {
                bool atEnd = (direction > 0 && selectedIndex == inventoryItems.Count - 1);
                bool atStart = (direction < 0 && selectedIndex == 0);
        
                if (atEnd || atStart)
                {
                    selectedIndex = atEnd ? 0 : inventoryItems.Count - 1;
                    InitializePanels();
                    return;
                }
            }
    
            if (next == selectedIndex)
            {
                return;
            }

            selectedIndex = next;

            // When items <= panels, don't scroll the container, just update focus
            if (inventoryItems.Count <= panelQueue.Count)
            {
                InitializePanels();
                return;
            }

            if (direction > 0)
                ScrollForward();
            else
                ScrollBackward();
        }

        private int NextIndex(int delta)
        {
            int next = selectedIndex + delta;
            int itemCount = inventoryItems.Count;

            switch (cycleMode)
            {
                case CycleMode.Stop:
                    return Mathf.Clamp(next, 0, itemCount - 1);

                case CycleMode.Restart:
                    // Clamp at boundaries - the jump is handled in Scroll()
                    return Mathf.Clamp(next, 0, itemCount - 1);

                case CycleMode.CircularPure:
                    // Always wrap around
                    return (next % itemCount + itemCount) % itemCount;

                case CycleMode.CircularStop:
                    // Only wrap if we have enough items to fill all panels
                    if (itemCount >= panelQueue.Count)
                        return (next % itemCount + itemCount) % itemCount;
                    else
                        return Mathf.Clamp(next, 0, itemCount - 1);
            }

            return selectedIndex;
        }

        #endregion

        #region Queue Motion

        private void ScrollForward()
        {
            isAnimating = true;

            ScrollUIPanel panel = panelQueue.First.Value;
            panelQueue.RemoveFirst();
            panelQueue.AddLast(panel);

            var last = panelQueue.Last.Previous.Value.rectTransform;
            panel.rectTransform.localPosition = last.localPosition + slotDelta;

            RefreshPanel(panel, selectedIndex + centerPanelIndex);

            AnimateContainer(-slotDelta);
        }

        private void ScrollBackward()
        {
            isAnimating = true;

            ScrollUIPanel panel = panelQueue.Last.Value;
            panelQueue.RemoveLast();
            panelQueue.AddFirst(panel);

            var first = panelQueue.First.Next.Value.rectTransform;
            panel.rectTransform.localPosition = first.localPosition - slotDelta;

            RefreshPanel(panel, selectedIndex - centerPanelIndex);

            AnimateContainer(slotDelta);
        }

        private void AnimateContainer(Vector3 offset)
        {
            scrollItemContainer.localPosition += offset;

            Tween.LocalPosition(scrollItemContainer, Vector3.zero, scrollDuration, Ease.Linear, 
                    1, PrimeTween.CycleMode.Restart, 0F, 0F, true)
                .OnComplete(() =>
                {
                    UpdateFocus();
                    isAnimating = false;
                });
        }

        #endregion

        #region Panel Data

        private void InitializePanels()
        {
            int i = 0;
            foreach (var panel in panelQueue)
            {
                int dataIndex = selectedIndex - centerPanelIndex + i;
                RefreshPanel(panel, dataIndex);
                i++;
            }

            UpdateFocus();
        }

        private void RefreshPanel(ScrollUIPanel panel, int dataIndex)
        {
            // Use circular indexing for CircularPure and CircularStop modes
            if (cycleMode == CycleMode.CircularPure || cycleMode == CycleMode.CircularStop)
                dataIndex = (dataIndex % inventoryItems.Count + inventoryItems.Count) % inventoryItems.Count;

            if (dataIndex >= 0 && dataIndex < inventoryItems.Count)
                panel.Refresh(getItemInfoFunc(inventoryItems[dataIndex]));
            else
                panel.Refresh(null);
        }

        #endregion

        #region Focus

        private void UpdateFocus()
        {
            if (!focusCenterPanel)
                return;

            int i = 0;
            foreach (var panel in panelQueue)
            {
                RectTransform rt = panel.rectTransform;

                if (i == centerPanelIndex)
                {
                    panel.OnSelected();
                    Tween.Scale(rt, focusedScale, scaleDuration, Ease.OutQuad, 1, 
                        PrimeTween.CycleMode.Restart, 0F, 0F, true);
                }
                else
                {
                    panel.OnDeselected();
                    Tween.Scale(rt, normalScale, scaleDuration, Ease.OutQuad, 1, 
                        PrimeTween.CycleMode.Restart, 0F, 0F, true);
                }

                i++;
            }
        }

        #endregion
    }
    
    public interface IScrollMenuAuthority
    {

        public void SubscribeToScroll(ScrollMenu menu);
        public void UnsubscribeFromScroll(ScrollMenu menu);
    }
}