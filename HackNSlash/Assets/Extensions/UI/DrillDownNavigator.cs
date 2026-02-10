using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Extensions.UI
{
    /// <summary>
    /// Enables "drill-down" navigation: when a container is selected, the user can press a button
    /// to navigate into its children, then press back to return to parent navigation.
    /// Perfect for equipment unlock grids where you want to hover on a container, then drill in
    /// to see individual unlock slots.
    /// </summary>
    public class DrillDownNavigator : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler, ICancelHandler
    {
        [Header("References")]
        [Tooltip("The container that can be drilled into. When null, uses this GameObject.")]
        [SerializeField] private GameObject drillDownContainer;
        
        [Tooltip("The first selectable when drilling down into children.")]
        [SerializeField] private Selectable firstChildSelectable;
        
        [Tooltip("Visual indicator shown when this container is selected.")]
        [SerializeField] private GameObject selectionIndicator;
        
        [Header("Settings")]
        [Tooltip("If true, automatically finds first Selectable in children.")]
        [SerializeField] private bool autoFindFirstChild = true;
        
        [Tooltip("If true, plays audio feedback on drill down/up.")]
        [SerializeField] private bool playAudio = true;
        
        private bool isDrilledDown = false;
        private bool isSelected = false;
        private Button button;
        private Selectable returnToSelectable; // What to select when backing out
        
        #region MonoBehaviour Callbacks
        
        private void Awake()
        {
            if (drillDownContainer == null)
                drillDownContainer = gameObject;
            
            // Setup button for navigation
            button = GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();
            
            if (autoFindFirstChild && firstChildSelectable == null)
            {
                firstChildSelectable = FindFirstChildSelectable();
            }
            
            if (selectionIndicator != null)
                selectionIndicator.SetActive(false);
        }
        
        #endregion
        
        #region Event System Handlers
        
        public void OnSelect(BaseEventData eventData)
        {
            if (isDrilledDown) return;
            
            isSelected = true;
            
            if (selectionIndicator != null)
                selectionIndicator.SetActive(true);
            
            if (playAudio)
                UIAudio.PlayHover();
        }
        
        public void OnDeselect(BaseEventData eventData)
        {
            if (isDrilledDown) return;
            
            isSelected = false;
            
            if (selectionIndicator != null)
                selectionIndicator.SetActive(false);
        }
        
        public void OnSubmit(BaseEventData eventData)
        {
            if (isDrilledDown) return;
            
            DrillDown();
        }
        
        public void OnCancel(BaseEventData eventData)
        {
            if (!isDrilledDown) return;
            
            DrillUp();
        }
        
        #endregion
        
        #region Drill Navigation
        
        /// <summary>
        /// Drills down into child elements, enabling navigation within them.
        /// </summary>
        public void DrillDown()
        {
            if (isDrilledDown) return;
            if (firstChildSelectable == null)
            {
                Debug.LogWarning("DrillDownNavigator: No firstChildSelectable assigned!");
                return;
            }
            
            isDrilledDown = true;
            returnToSelectable = button;
            
            // Disable parent button navigation
            if (button != null)
            {
                Navigation nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                button.interactable = false;
            }
            
            // Select first child
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstChildSelectable.gameObject);
            }
            
            if (playAudio)
                UIAudio.PlaySelect();
        }
        
        /// <summary>
        /// Drills up from child elements back to parent container navigation.
        /// </summary>
        public void DrillUp()
        {
            if (!isDrilledDown) return;
            
            isDrilledDown = false;
            
            // Re-enable parent button navigation
            if (button != null)
            {
                button.interactable = true;
                // Navigation will be reconfigured by MenuNavigationConfigurator
            }
            
            // Select this container again
            if (EventSystem.current != null && returnToSelectable != null)
            {
                EventSystem.current.SetSelectedGameObject(returnToSelectable.gameObject);
            }
            
            if (playAudio)
                UIAudio.PlayBack();
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Finds the first interactable Selectable in child hierarchy.
        /// </summary>
        private Selectable FindFirstChildSelectable()
        {
            if (drillDownContainer == null) return null;
            
            Selectable[] selectables = drillDownContainer.GetComponentsInChildren<Selectable>(true);
            foreach (var selectable in selectables)
            {
                // Skip self
                if (selectable.gameObject == gameObject) continue;
                
                if (selectable.IsInteractable())
                    return selectable;
            }
            
            return null;
        }
        
        /// <summary>
        /// Checks if this navigator is currently drilled down.
        /// </summary>
        public bool IsDrilledDown() => isDrilledDown;
        
        #endregion
    }
}

