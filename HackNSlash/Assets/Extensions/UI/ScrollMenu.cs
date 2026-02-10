using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// REWRITTEN for guaranteed functionality with proper input handling.
    /// </summary>
    public class ScrollMenu : MonoBehaviour
    {
        #region Inspector

        [Header("Scroll Settings")]
        [SerializeField] private MovementAxis axis = MovementAxis.Vertical;
        [SerializeField] private CycleMode cycleMode = CycleMode.Stop;

        [Tooltip("Whether to visually focus the center panel.")]
        [SerializeField] private bool focusCenterPanel = true;

        [Tooltip("Number of panels visible at once (e.g., 5 for a 5-item scroll menu).")]
        [SerializeField] private int visiblePanelCount = 5;
        
        [Tooltip("Index of the center/focused panel (usually visiblePanelCount/2).")]
        [SerializeField] private int centerPanelIndex = 2;

        [Header("Timing")]
        [SerializeField] private float scrollCooldown = 0.2f;
        [SerializeField] private float scrollDuration = 0.15f;
        
        [Header("Visuals")]
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private Vector3 focusedScale = Vector3.one * 1.15f;
        [SerializeField] private float scaleDuration = 0.1f;

        [Header("UI")]
        [SerializeField] private Transform scrollItemContainer;

        #endregion

        private List<ScrollUIPanel> panels = new List<ScrollUIPanel>();
        private ScrollUIPanel topBufferPanel;
        private ScrollUIPanel bottomBufferPanel;
        private Vector3 slotDelta;
        
        // Store original positions to restore after animation
        private List<Vector3> originalPanelPositions = new List<Vector3>();
        private Vector3 originalTopBufferPosition;
        private Vector3 originalBottomBufferPosition;

        private IList inventoryItems;
        private Func<object, ItemUIInfo> getItemInfoFunc;

        private IScrollMenuAuthority authority;

        private int selectedIndex;
        private bool isAnimating;
        private float lastScrollTime;
        
        /// <summary>
        /// Event raised when user presses A button to confirm selection.
        /// Passes the selected item's data index.
        /// </summary>
        public System.Action<int> OnItemConfirmed;
        
        /// <summary>
        /// Gets the currently selected item's data.
        /// </summary>
        public ItemUIInfo GetSelectedItem()
        {
            if (inventoryItems == null || selectedIndex < 0 || selectedIndex >= inventoryItems.Count)
                return null;
            
            return getItemInfoFunc(inventoryItems[selectedIndex]);
        }
        
        /// <summary>
        /// Gets the currently selected index.
        /// </summary>
        public int GetSelectedIndex() => selectedIndex;

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            panels = scrollItemContainer.GetComponentsInChildren<ScrollUIPanel>(true).ToList();

            if (panels.Count >= 1)
            {
                // Store original panel positions
                originalPanelPositions.Clear();
                foreach (var panel in panels)
                {
                    originalPanelPositions.Add(panel.rectTransform.localPosition);
                }
                
                RectTransform rt = panels[0].rectTransform;
                // For vertical: panels go TOP to BOTTOM, so next panel is at LOWER Y (negative)
                // For horizontal: panels go LEFT to RIGHT, so next panel is at HIGHER X (positive)
                slotDelta = axis == MovementAxis.Horizontal
                    ? new Vector3(rt.rect.width, 0f, 0f)
                    : new Vector3(0f, -rt.rect.height, 0f);  // NEGATIVE for top-to-bottom layout
                
                // Create buffer panels for smooth scrolling animation
                CreateBufferPanels();
            }

            lastScrollTime = -scrollCooldown;
            Debug.Log($"ScrollMenu: Initialized with {panels.Count} panels, slotDelta={slotDelta}");
        }
        
        /// <summary>
        /// Creates buffer panels at top and bottom for seamless scroll animation.
        /// These panels move off-screen to create the illusion of infinite scroll.
        /// </summary>
        private void CreateBufferPanels()
        {
            if (panels.Count == 0)
            {
                Debug.LogError("ScrollMenu: Cannot create buffer panels - no panels found!");
                return;
            }
            
            // Get the first panel as template
            ScrollUIPanel templatePanel = panels[0];
            Debug.Log($"ScrollMenu: Creating buffer panels using template '{templatePanel.gameObject.name}'");
            
            // Create top buffer panel
            GameObject topBufferObj = GameObject.Instantiate(templatePanel.gameObject, scrollItemContainer);
            topBufferObj.name = "BufferPanel_Top";
            topBufferPanel = topBufferObj.GetComponent<ScrollUIPanel>();
            
            if (topBufferPanel == null)
            {
                Debug.LogError("ScrollMenu: Top buffer GameObject doesn't have ScrollUIPanel component!");
            }
            
            // Position above first panel (for vertical: panel[0] is at top, buffer should be ABOVE it)
            // Since slotDelta is negative for vertical, to go UP we SUBTRACT slotDelta (which adds positive Y)
            Vector3 topPos = panels[0].rectTransform.localPosition - slotDelta;
            topBufferPanel.rectTransform.localPosition = topPos;
            originalTopBufferPosition = topPos;
            Debug.Log($"ScrollMenu: Top buffer created at position {topPos}, panel[0] at {panels[0].rectTransform.localPosition}");
            
            // Create bottom buffer panel
            GameObject bottomBufferObj = GameObject.Instantiate(templatePanel.gameObject, scrollItemContainer);
            bottomBufferObj.name = "BufferPanel_Bottom";
            bottomBufferPanel = bottomBufferObj.GetComponent<ScrollUIPanel>();
            
            if (bottomBufferPanel == null)
            {
                Debug.LogError("ScrollMenu: Bottom buffer GameObject doesn't have ScrollUIPanel component!");
            }
            
            // Position below last panel (for vertical: to go DOWN we ADD slotDelta which is negative Y)
            Vector3 bottomPos = panels[panels.Count - 1].rectTransform.localPosition + slotDelta;
            bottomBufferPanel.rectTransform.localPosition = bottomPos;
            originalBottomBufferPosition = bottomPos;
            Debug.Log($"ScrollMenu: Bottom buffer created at position {bottomPos}, panel[last] at {panels[panels.Count - 1].rectTransform.localPosition}");
            
            Debug.Log($"ScrollMenu: Buffer panels created successfully - Top: {topBufferPanel != null}, Bottom: {bottomBufferPanel != null}");
        }

        #endregion

        #region Activation

        /// <summary>
        /// Activates the scroll menu with the provided items and configuration.
        /// </summary>
        public void Activate<T>(List<T> items, int initialIndex, Func<T, ItemUIInfo> getInfoFunc, IScrollMenuAuthority authority) where T : class
        {
            if (items == null || items.Count == 0)
            {
                Debug.LogWarning("ScrollMenu: Cannot activate with null or empty items list.");
                return;
            }

            inventoryItems = items;
            getItemInfoFunc = o => getInfoFunc((T)o);
            selectedIndex = Mathf.Clamp(initialIndex, 0, items.Count - 1);
            
            this.authority = authority;
            this.authority?.SubscribeToScroll(this);

            isAnimating = false;
            lastScrollTime = -scrollCooldown; // Allow immediate scroll

            RefreshAllPanels();
            UpdateFocus();
            
            // CRITICAL: Initialize buffer panels with correct data for first scroll
            InitializeBufferPanels();
            
            Debug.Log($"ScrollMenu: Activated with {items.Count} items, selectedIndex={selectedIndex}");
        }

        /// <summary>
        /// Deactivates the scroll menu and cleans up subscriptions.
        /// </summary>
        public void Deactivate()
        {
            authority?.UnsubscribeFromScroll(this);
            authority = null;
            
            inventoryItems = null;
            getItemInfoFunc = null;
            isAnimating = false;
            
            Debug.Log("ScrollMenu: Deactivated");
        }

        /// <summary>
        /// Initializes buffer panels with correct data when menu is first activated.
        /// This ensures the first scroll in any direction works perfectly.
        /// </summary>
        private void InitializeBufferPanels()
        {
            if (topBufferPanel == null || bottomBufferPanel == null)
                return;
            
            int actualFocusIndex = GetActualFocusIndex();
            
            // Top buffer shows item that would be at panel[0] if user scrolls down (to next item)
            // That would be: (selectedIndex + 1) - actualFocusIndex
            int topDataIndex = (selectedIndex + 1) - actualFocusIndex;
            RefreshSinglePanel(topBufferPanel, topDataIndex);
            
            // Bottom buffer shows item that would be at panel[last] if user scrolls up (to prev item)
            // That would be: (selectedIndex - 1) + (panels.Count - 1 - actualFocusIndex)
            int bottomDataIndex = (selectedIndex - 1) + (panels.Count - 1 - actualFocusIndex);
            RefreshSinglePanel(bottomBufferPanel, bottomDataIndex);
            
            Debug.Log($"ScrollMenu: Initialized buffers - Top={topDataIndex}, Bottom={bottomDataIndex}");
        }

        #endregion

        #region Input Callbacks

        /// <summary>
        /// Handles scroll input from the input system.
        /// Uses simple time-based cooldown instead of timer system.
        /// </summary>
        public void OnScrollPerformed(Vector2 scrollDelta)
        {
            // Simple cooldown check using Time.unscaledTime
            float timeSinceLastScroll = Time.unscaledTime - lastScrollTime;
            if (timeSinceLastScroll < scrollCooldown)
            {
                return; // Still in cooldown
            }

            if (isAnimating || inventoryItems == null || inventoryItems.Count == 0)
            {
                return;
            }

            // Determine scroll direction based on axis
            // For VERTICAL: Positive Y = UP = previous item (direction -1)
            //               Negative Y = DOWN = next item (direction +1)
            // For HORIZONTAL: Positive X = RIGHT = next item (direction +1)
            //                 Negative X = LEFT = previous item (direction -1)
            float delta = axis == MovementAxis.Horizontal ? scrollDelta.x : scrollDelta.y;

            if (Mathf.Abs(delta) < 0.1f) // Deadzone
            {
                return;
            }

            // Update cooldown timer
            lastScrollTime = Time.unscaledTime;
            
            // Determine direction based on axis and delta
            int direction;
            if (axis == MovementAxis.Vertical)
            {
                // Vertical: positive stick = up = previous item (lower index)
                direction = delta > 0 ? -1 : 1;
            }
            else
            {
                // Horizontal: positive stick = right = next item (higher index)
                direction = delta > 0 ? 1 : -1;
            }
            
            Debug.Log($"ScrollMenu: Scrolling direction={direction}, delta={delta}, axis={axis}");
            
            Scroll(direction);
        }

        #endregion

        #region Scrolling

        /// <summary>
        /// Scrolls the menu in the specified direction.
        /// Uses buffer panels for smooth animation even in Stop mode.
        /// </summary>
        private void Scroll(int direction)
        {
            int oldIndex = selectedIndex;
            int newIndex = GetNextIndex(direction);

            if (newIndex == oldIndex)
            {
                Debug.Log($"ScrollMenu: At boundary, cannot scroll further");
                return; // Can't scroll further
            }

            selectedIndex = newIndex;
            
            Debug.Log($"ScrollMenu: Scrolled from {oldIndex} to {selectedIndex}");

            // Check if we should animate or just update focus
            // In Stop mode, if focus would move to edge, just refresh without animation
            if (cycleMode == CycleMode.Stop)
            {
                int itemCount = inventoryItems.Count;
                int oldFocus = GetActualFocusIndexForIndex(oldIndex);
                int newFocus = GetActualFocusIndex();
                
                // If focus index changed (moved to edge), just refresh without animation
                if (oldFocus != newFocus)
                {
                    Debug.Log($"ScrollMenu: Focus moved from {oldFocus} to {newFocus}, refreshing without animation");
                    RefreshAllPanels();
                    UpdateFocus();
                    return;
                }
            }

            // Use smooth animation with buffer panels
            AnimateScrollWithBuffers(direction);
        }
        
        /// <summary>
        /// Gets what the focus index WOULD BE for a given selectedIndex.
        /// Used to detect if focus changes during scroll.
        /// </summary>
        private int GetActualFocusIndexForIndex(int checkIndex)
        {
            if (cycleMode == CycleMode.Stop)
            {
                int itemCount = inventoryItems.Count;
                
                if (itemCount <= panels.Count)
                {
                    return checkIndex;
                }
                
                if (checkIndex < centerPanelIndex)
                {
                    return checkIndex;
                }
                
                int distanceFromEnd = itemCount - 1 - checkIndex;
                int maxOffset = panels.Count - 1 - centerPanelIndex;
                if (distanceFromEnd < maxOffset)
                {
                    return panels.Count - 1 - distanceFromEnd;
                }
                
                return centerPanelIndex;
            }
            
            return centerPanelIndex;
        }

        private int GetNextIndex(int direction)
        {
            int next = selectedIndex + direction;
            int itemCount = inventoryItems.Count;

            switch (cycleMode)
            {
                case CycleMode.Stop:
                    // Stop at boundaries - can't go past first or last
                    return Mathf.Clamp(next, 0, itemCount - 1);

                case CycleMode.Restart:
                    // Wrap to opposite end when reaching boundary
                    if (next < 0) return itemCount - 1;
                    if (next >= itemCount) return 0;
                    return next;

                case CycleMode.CircularPure:
                    // Always wrap - infinite circular scroll
                    return (next % itemCount + itemCount) % itemCount;

                case CycleMode.CircularStop:
                    // Only wrap if we have more items than panels
                    if (itemCount >= panels.Count)
                        return (next % itemCount + itemCount) % itemCount;
                    else
                        return Mathf.Clamp(next, 0, itemCount - 1);
            }

            return selectedIndex;
        }

        #endregion

        #region Animation

        /// <summary>
        /// Animates scroll with proper buffer recycling.
        /// LOGIC:
        /// - direction > 0: User wants next item. Panels slide UP. Top buffer (above panel[0]) slides into view at panel[0] position.
        /// - direction < 0: User wants prev item. Panels slide DOWN. Bottom buffer (below panel[last]) slides into view at panel[last] position.
        /// </summary>
        private void AnimateScrollWithBuffers(int direction)
        {
            isAnimating = true;

            int actualFocusIndex = GetActualFocusIndex();
            
            // STEP 1: Calculate what data WILL BE shown after scroll completes
            // After scroll, selectedIndex has already changed. We need to show:
            // panel[0] = selectedIndex - actualFocusIndex
            // panel[1] = selectedIndex - actualFocusIndex + 1
            // ...
            // panel[actualFocusIndex] = selectedIndex (focused)
            // ...
            // panel[last] = selectedIndex + (panels.Count - 1 - actualFocusIndex)
            
            // STEP 2: Update the buffer that will slide into view
            if (direction > 0)
            {
                // Scrolling to next item - panels slide UP - top buffer comes into view
                // Top buffer will end up at panel[0]'s position, showing item at (selectedIndex - actualFocusIndex)
                int topDataIndex = selectedIndex - actualFocusIndex;
                RefreshSinglePanel(topBufferPanel, topDataIndex);
            }
            else
            {
                // Scrolling to prev item - panels slide DOWN - bottom buffer comes into view  
                // Bottom buffer will end up at panel[last]'s position, showing item at (selectedIndex + (panels.Count - 1 - actualFocusIndex))
                int bottomDataIndex = selectedIndex + (panels.Count - 1 - actualFocusIndex);
                RefreshSinglePanel(bottomBufferPanel, bottomDataIndex);
            }

            // STEP 3: Animate ALL panels (including buffers) by slideOffset
            // For vertical top-to-bottom layout with slotDelta = -height:
            //   direction=1 (next): want panels to slide UP (+Y), so offset = -slotDelta * direction = +height
            //   direction=-1 (prev): want panels to slide DOWN (-Y), so offset = -slotDelta * direction = -height
            Vector3 slideOffset = -slotDelta * direction;
            
            int tweenCount = 0;
            
            // Animate main panels
            foreach (var panel in panels)
            {
                if (panel == null || panel.rectTransform == null) continue;
                Vector3 endPos = panel.rectTransform.localPosition + slideOffset;
                Tween.LocalPosition(panel.rectTransform, endPos, scrollDuration, Ease.OutQuad, useUnscaledTime: true);
                tweenCount++;
            }
            
            // Animate buffers
            if (topBufferPanel != null && topBufferPanel.rectTransform != null)
            {
                Vector3 endPos = topBufferPanel.rectTransform.localPosition + slideOffset;
                Tween.LocalPosition(topBufferPanel.rectTransform, endPos, scrollDuration, Ease.OutQuad, useUnscaledTime: true);
                tweenCount++;
            }
            if (bottomBufferPanel != null && bottomBufferPanel.rectTransform != null)
            {
                Vector3 endPos = bottomBufferPanel.rectTransform.localPosition + slideOffset;
                Tween.LocalPosition(bottomBufferPanel.rectTransform, endPos, scrollDuration, Ease.OutQuad, useUnscaledTime: true);
                tweenCount++;
            }

            // STEP 4: On complete - snap back positions and refresh all data
            if (tweenCount > 0)
            {
                // Use delay to wait for animation to complete
                Tween.Delay(scrollDuration, useUnscaledTime: true).OnComplete(() =>
                {
                    // Reset all positions to their ORIGINAL positions
                    for (int i = 0; i < panels.Count; i++)
                    {
                        if (panels[i] != null && panels[i].rectTransform != null && i < originalPanelPositions.Count)
                            panels[i].rectTransform.localPosition = originalPanelPositions[i];
                    }
                    if (topBufferPanel != null && topBufferPanel.rectTransform != null)
                        topBufferPanel.rectTransform.localPosition = originalTopBufferPosition;
                    if (bottomBufferPanel != null && bottomBufferPanel.rectTransform != null)
                        bottomBufferPanel.rectTransform.localPosition = originalBottomBufferPosition;
                    
                    // Refresh ALL panel data for new selectedIndex
                    RefreshAllPanels();
                    UpdateFocus();
                    
                    isAnimating = false;
                });
            }
            else
            {
                isAnimating = false;
            }
        }

        /// <summary>
        /// Resets all panel positions back to their original starting positions.
        /// </summary>
        private void ResetPanelPositions()
        {
            // Reset main panels to their original positions
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] != null && panels[i].rectTransform != null && i < originalPanelPositions.Count)
                    panels[i].rectTransform.localPosition = originalPanelPositions[i];
            }

            // Reset buffer positions
            if (topBufferPanel != null && topBufferPanel.rectTransform != null)
                topBufferPanel.rectTransform.localPosition = originalTopBufferPosition;
            
            if (bottomBufferPanel != null && bottomBufferPanel.rectTransform != null)
                bottomBufferPanel.rectTransform.localPosition = originalBottomBufferPosition;
        }
        
        /// <summary>
        /// Updates buffer panels with correct data for animation.
        /// When direction=1 (next): panels slide UP, so TOP buffer becomes visible → show new top item
        /// When direction=-1 (prev): panels slide DOWN, so BOTTOM buffer becomes visible → show new bottom item
        /// </summary>
        private void UpdateBufferPanels(int direction)
        {
            if (topBufferPanel == null || bottomBufferPanel == null)
                return;
            
            int actualFocusIndex = GetActualFocusIndex();
            
            if (direction > 0)
            {
                // Scrolling to NEXT (panels slide UP)
                // Top buffer will be visible showing the new item at panel[0]
                int topDataIndex = selectedIndex - actualFocusIndex;
                RefreshSinglePanel(topBufferPanel, topDataIndex);
            }
            else
            {
                // Scrolling to PREV (panels slide DOWN)
                // Bottom buffer will be visible showing the new item at panel[last]
                int bottomDataIndex = selectedIndex + (panels.Count - 1 - actualFocusIndex);
                RefreshSinglePanel(bottomBufferPanel, bottomDataIndex);
            }
        }
        
        /// <summary>
        /// Refreshes a single panel with data at the given index.
        /// </summary>
        private void RefreshSinglePanel(ScrollUIPanel panel, int dataIndex)
        {
            if (panel == null) return;
            
            // Handle wrapping for circular modes
            if (cycleMode == CycleMode.CircularPure || 
                (cycleMode == CycleMode.CircularStop && inventoryItems.Count >= panels.Count))
            {
                dataIndex = (dataIndex % inventoryItems.Count + inventoryItems.Count) % inventoryItems.Count;
            }

            if (dataIndex >= 0 && dataIndex < inventoryItems.Count)
            {
                ItemUIInfo info = getItemInfoFunc(inventoryItems[dataIndex]);
                panel.Refresh(info);
            }
            else
            {
                panel.Refresh(null);
            }
        }


        #endregion

        #region Panel Management

        private void RefreshAllPanels()
        {
            Debug.Log($"ScrollMenu: RefreshAllPanels - selectedIndex={selectedIndex}, centerPanel={centerPanelIndex}, cycleMode={cycleMode}");
            
            // Calculate the actual focus position based on mode and boundaries
            int actualFocusIndex = GetActualFocusIndex();
            
            for (int i = 0; i < panels.Count; i++)
            {
                // Calculate data index relative to the actual focus position
                int offset = i - actualFocusIndex;
                int dataIndex = selectedIndex + offset;
                
                Debug.Log($"  Panel {i}: offset={offset}, dataIndex={dataIndex}, actualFocus={actualFocusIndex}");
                RefreshPanel(panels[i], dataIndex);
            }
        }
        
        /// <summary>
        /// Gets the actual focus index based on cycle mode and boundaries.
        /// For Stop mode: Focus moves to edges when near start/end of list.
        /// For other modes: Focus stays at centerPanelIndex.
        /// </summary>
        private int GetActualFocusIndex()
        {
            if (cycleMode == CycleMode.Stop)
            {
                int itemCount = inventoryItems.Count;
                
                // If we have fewer items than panels, focus the actual item position
                if (itemCount <= panels.Count)
                {
                    return selectedIndex;
                }
                
                // Near the start: focus moves down from center
                if (selectedIndex < centerPanelIndex)
                {
                    return selectedIndex;
                }
                
                // Near the end: focus moves up from center
                int distanceFromEnd = itemCount - 1 - selectedIndex;
                int maxOffset = panels.Count - 1 - centerPanelIndex;
                if (distanceFromEnd < maxOffset)
                {
                    return panels.Count - 1 - distanceFromEnd;
                }
                
                // In the middle: focus stays at center
                return centerPanelIndex;
            }
            
            // For all other modes, focus always stays at center
            return centerPanelIndex;
        }

        private void RefreshPanel(ScrollUIPanel panel, int dataIndex)
        {
            int originalIndex = dataIndex;
            
            // Handle wrapping for circular modes
            if (cycleMode == CycleMode.CircularPure || 
                (cycleMode == CycleMode.CircularStop && inventoryItems.Count >= panels.Count))
            {
                dataIndex = (dataIndex % inventoryItems.Count + inventoryItems.Count) % inventoryItems.Count;
            }

            if (dataIndex >= 0 && dataIndex < inventoryItems.Count)
            {
                ItemUIInfo info = getItemInfoFunc(inventoryItems[dataIndex]);
                panel.Refresh(info);
                string panelName = panel.gameObject.name;
                string itemName = info?.itemName ?? "NULL_INFO";
                Debug.Log($"    Panel '{panelName}' → dataIndex={dataIndex}, item='{itemName}'");
            }
            else
            {
                // Only show as null/empty if we're in Stop mode and out of bounds
                panel.Refresh(null);
                Debug.Log($"    Panel '{panel.gameObject.name}' → NULL (originalIndex={originalIndex}, adjusted={dataIndex} OOB)");
            }
        }

        private void UpdateFocus()
        {
            if (!focusCenterPanel)
            {
                Debug.Log("ScrollMenu: Focus disabled");
                return;
            }

            int actualFocusIndex = GetActualFocusIndex();
            Debug.Log($"ScrollMenu: UpdateFocus - actualFocusIndex={actualFocusIndex}");
            
            for (int i = 0; i < panels.Count; i++)
            {
                ScrollUIPanel panel = panels[i];
                
                if (panel == null || panel.rectTransform == null)
                {
                    Debug.LogWarning($"ScrollMenu: Panel {i} or its rectTransform is null!");
                    continue;
                }
                
                RectTransform rt = panel.rectTransform;

                if (i == actualFocusIndex)
                {
                    panel.OnSelected();
                    // Use proper PrimeTween API with explicit parameters
                    Tween.Scale(
                        target: rt,
                        endValue: focusedScale,
                        duration: scaleDuration,
                        ease: Ease.OutQuad,
                        cycles: 1,
                        cycleMode: PrimeTween.CycleMode.Restart,
                        startDelay: 0f,
                        endDelay: 0f,
                        useUnscaledTime: true
                    );
                    Debug.Log($"  Panel {i}: SELECTED and scaled to {focusedScale}");
                }
                else
                {
                    panel.OnDeselected();
                    // Use proper PrimeTween API with explicit parameters
                    Tween.Scale(
                        target: rt,
                        endValue: normalScale,
                        duration: scaleDuration,
                        ease: Ease.OutQuad,
                        cycles: 1,
                        cycleMode: PrimeTween.CycleMode.Restart,
                        startDelay: 0f,
                        endDelay: 0f,
                        useUnscaledTime: true
                    );
                    Debug.Log($"  Panel {i}: deselected and scaled to {normalScale}");
                }
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