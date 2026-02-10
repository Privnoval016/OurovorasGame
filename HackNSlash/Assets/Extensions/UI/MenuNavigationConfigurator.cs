using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Extensions.UI
{
    /// <summary>
    /// Automatically configures the entire menu UI system's navigation in code.
    /// Eliminates most manual Inspector setup - only requires assigning tab content panels.
    /// Handles: Button listeners, navigation chains, first selected elements.
    /// </summary>
    [RequireComponent(typeof(TabGroup))]
    public class MenuNavigationConfigurator : MonoBehaviour
    {
        [Header("Auto-Configuration")]
        [Tooltip("Automatically configure all navigation on Start. Recommended to keep true.")]
        [SerializeField] private bool autoConfigureOnStart = true;
        
        [Tooltip("Automatically select the first tab when menu opens.")]
        [SerializeField] private bool autoSelectFirstTab = true;
        
        [Tooltip("Index of the first tab to select (0-based). Usually 0.")]
        [SerializeField] private int initialTabIndex = 0;
        
        private TabGroup tabGroup;
        private List<TabButton> tabButtons = new List<TabButton>();
        
        #region MonoBehaviour Callbacks
        
        private void Awake()
        {
            tabGroup = GetComponent<TabGroup>();
        }
        
        private void Start()
        {
            if (autoConfigureOnStart)
            {
                ConfigureAllNavigation();
            }
            
            if (autoSelectFirstTab && tabButtons.Count > 0)
            {
                // Delay to ensure EventSystem is ready
                Invoke(nameof(SelectInitialTab), 0.1f);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Configures all navigation for the entire menu system.
        /// Call manually if autoConfigureOnStart is false.
        /// </summary>
        public void ConfigureAllNavigation()
        {
            // Find all tab buttons
            tabButtons.Clear();
            tabButtons.AddRange(GetComponentsInChildren<TabButton>(true));
            
            if (tabButtons.Count == 0)
            {
                Debug.LogWarning("MenuNavigationConfigurator: No TabButtons found!");
                return;
            }
            
            Debug.Log($"MenuNavigationConfigurator: Configuring {tabButtons.Count} tabs");
            
            // Configure tab button chain
            ConfigureTabButtonChain();
            
            // Configure each tab's content navigation
            foreach (var tabButton in tabButtons)
            {
                ConfigureTabContent(tabButton);
            }
            
            Debug.Log("MenuNavigationConfigurator: Navigation configuration complete");
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Sets up horizontal navigation chain between tab buttons.
        /// </summary>
        private void ConfigureTabButtonChain()
        {
            for (int i = 0; i < tabButtons.Count; i++)
            {
                Button button = tabButtons[i].GetComponent<Button>();
                if (button == null)
                {
                    Debug.LogWarning($"MenuNavigationConfigurator: Tab button {i} is missing Button component!");
                    continue;
                }
                
                Navigation nav = new Navigation();
                nav.mode = Navigation.Mode.Explicit;
                
                // Left (previous tab, wraps to last)
                int leftIndex = i > 0 ? i - 1 : tabButtons.Count - 1;
                Button leftButton = tabButtons[leftIndex].GetComponent<Button>();
                if (leftButton != null)
                    nav.selectOnLeft = leftButton;
                
                // Right (next tab, wraps to first)
                int rightIndex = i < tabButtons.Count - 1 ? i + 1 : 0;
                Button rightButton = tabButtons[rightIndex].GetComponent<Button>();
                if (rightButton != null)
                    nav.selectOnRight = rightButton;
                
                // Down (first selectable in tab content)
                if (tabButtons[i].contentPanel != null)
                {
                    Selectable firstSelectable = FindFirstSelectable(tabButtons[i].contentPanel.gameObject);
                    if (firstSelectable != null)
                    {
                        nav.selectOnDown = firstSelectable;
                    }
                }
                
                button.navigation = nav;
            }
        }
        
        /// <summary>
        /// Configures navigation within a tab's content.
        /// </summary>
        private void ConfigureTabContent(TabButton tabButton)
        {
            if (tabButton.contentPanel == null) return;
            
            // Find first selectable in content
            Selectable firstSelectable = FindFirstSelectable(tabButton.contentPanel.gameObject);
            if (firstSelectable != null)
            {
                // Setup up navigation from first element back to tab button
                Navigation nav = firstSelectable.navigation;
                Button tabButtonComponent = tabButton.GetComponent<Button>();
                if (tabButtonComponent != null)
                {
                    nav.selectOnUp = tabButtonComponent;
                    firstSelectable.navigation = nav;
                }
            }
            
            // Find and configure SlotGridNavigators
            SlotGridNavigator[] gridNavigators = tabButton.contentPanel.GetComponentsInChildren<SlotGridNavigator>(true);
            foreach (var gridNav in gridNavigators)
            {
                gridNav.ConfigureNavigation();
            }
            
            // Find and configure DrillDownNavigators
            DrillDownNavigator[] drillDownNavigators = tabButton.contentPanel.GetComponentsInChildren<DrillDownNavigator>(true);
            foreach (var drillNav in drillDownNavigators)
            {
                // DrillDownNavigators set up themselves, but we can ensure they're ready
                Debug.Log($"MenuNavigationConfigurator: Found DrillDownNavigator on {drillNav.gameObject.name}");
            }
        }
        
        /// <summary>
        /// Finds the first Selectable component in a GameObject hierarchy.
        /// Used to find the "default" button to select when a tab opens.
        /// </summary>
        private Selectable FindFirstSelectable(GameObject root)
        {
            // Check root first
            Selectable selectable = root.GetComponent<Selectable>();
            if (selectable != null && selectable.IsInteractable())
                return selectable;
            
            // Search children
            Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
            foreach (var s in selectables)
            {
                if (s.IsInteractable())
                    return s;
            }
            
            return null;
        }
        
        /// <summary>
        /// Selects the initial tab and focuses first element IN CONTENT (not tab button).
        /// CRITICAL: Tab buttons are non-interactable, so EventSystem must select content.
        /// </summary>
        private void SelectInitialTab()
        {
            if (tabGroup != null && initialTabIndex >= 0 && initialTabIndex < tabButtons.Count)
            {
                tabGroup.SelectTabByIndex(initialTabIndex);
                
                // CRITICAL: Select first element in tab CONTENT, not tab button
                // Tab buttons are non-interactable (bumper-only), so EventSystem can't select them
                if (EventSystem.current != null && tabButtons[initialTabIndex] != null)
                {
                    TabButton selectedTab = tabButtons[initialTabIndex];
                    if (selectedTab.contentPanel != null)
                    {
                        Selectable firstContent = FindFirstSelectable(selectedTab.contentPanel.gameObject);
                        if (firstContent != null)
                        {
                            EventSystem.current.SetSelectedGameObject(firstContent.gameObject);
                            Debug.Log($"MenuNavigationConfigurator: Selected first content element: {firstContent.gameObject.name}");
                        }
                        else
                        {
                            Debug.LogWarning($"MenuNavigationConfigurator: No selectable content found in tab {initialTabIndex}!");
                        }
                    }
                }
            }
        }
        
        #endregion
    }
}

