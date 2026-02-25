using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Extensions.UI
{
    /// <summary>
    /// Base class for tab content panels.
    /// Animation is handled by TabGroup for centralized control.
    /// This component only manages first element selection for controller navigation.
    /// </summary>
    public class TabSelection : MonoBehaviour
    {
        [Header("Navigation")]
        [Tooltip("Automatically select first element when tab opens. Should be true for controller support.")]
        [SerializeField] private bool autoSelectFirstElement = true;
        
        [Header("Selection Timing")]
        [Tooltip("Delay before selecting first element to allow animations to start")]
        [SerializeField] private float selectionDelay = 0.15f;
        
        private void Awake()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when the tab is selected.
        /// TabGroup handles all animations - this just manages first element selection.
        /// </summary>
        public virtual void OnTabSelect()
        {
            // CRITICAL: Select first element for controller navigation
            if (autoSelectFirstElement)
            {
                SelectFirstElement();
            }
        }

        /// <summary>
        /// Called when the tab is deselected.
        /// TabGroup handles all animations.
        /// </summary>
        public virtual void OnTabDeselect()
        {
            // Nothing to do - TabGroup handles animation and deactivation
        }
        
        /// <summary>
        /// Selects the first interactable element in this tab's content.
        /// CRITICAL for controller navigation - EventSystem needs a selected GameObject.
        /// </summary>
        protected void SelectFirstElement()
        {
            if (EventSystem.current == null)
            {
                Debug.LogWarning("TabSelection: No EventSystem found! Controller navigation will not work.");
                return;
            }
            
            // Find first interactable Selectable
            Selectable firstSelectable = FindFirstSelectableInChildren();
            
            if (firstSelectable != null)
            {
                // Delay slightly to ensure tab is fully visible
                Tween.Delay(selectionDelay, useUnscaledTime: true)
                    .OnComplete(() =>
                    {
                        if (firstSelectable != null && firstSelectable.gameObject.activeInHierarchy)
                        {
                            EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
                        }
                    });
            }
        }
        
        /// <summary>
        /// Finds the first interactable Selectable in this tab's children.
        /// </summary>
        private Selectable FindFirstSelectableInChildren()
        {
            Selectable[] selectables = GetComponentsInChildren<Selectable>(true);
            foreach (var selectable in selectables)
            {
                if (selectable.IsInteractable())
                    return selectable;
            }
            return null;
        }
    }
}

