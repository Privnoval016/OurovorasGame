using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Extensions.UI
{
    /// <summary>
    /// Reusable action menu for inventory items.
    /// Shows "Use" and "Discard" options when an item is selected.
    /// Completely modular and can be used in any inventory system.
    /// Uses custom ActionMenuButton components for consistent animations.
    /// </summary>
    public class ItemActionMenu : MonoBehaviour
    {
        [Header("Menu Buttons")]
        [SerializeField] private ActionMenuButton useButton;
        [SerializeField] private ActionMenuButton discardButton;
        
        [Header("Settings")]
        [SerializeField] private bool hideOnAction = true;
        
        /// <summary>
        /// Invoked when the Use button is pressed.
        /// </summary>
        public event Action OnUseRequested;
        
        /// <summary>
        /// Invoked when the Discard button is pressed.
        /// </summary>
        public event Action OnDiscardRequested;
        
        /// <summary>
        /// Invoked when the menu is closed/canceled.
        /// </summary>
        public event Action OnMenuClosed;
        
        private EventSystem eventSystem;
        
        private void Awake()
        {
            eventSystem = EventSystem.current;
            
            // Setup button listeners
            if (useButton != null)
            {
                useButton.RemoveAllListeners();
                useButton.AddListener(HandleUsePressed);
            }
            
            if (discardButton != null)
            {
                discardButton.RemoveAllListeners();
                discardButton.AddListener(HandleDiscardPressed);
            }
            
            // Setup navigation
            SetupNavigation();
            
            // Hide by default
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Sets up button navigation for controller support.
        /// </summary>
        private void SetupNavigation()
        {
            if (useButton != null && discardButton != null)
            {
                Navigation useNav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnDown = discardButton,
                    selectOnUp = discardButton
                };
                useButton.navigation = useNav;
                
                Navigation discardNav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = useButton,
                    selectOnDown = useButton
                };
                discardButton.navigation = discardNav;
            }
        }
        
        /// <summary>
        /// Shows the action menu and selects the first button.
        /// </summary>
        /// <param name="canUse">Whether the Use button should be interactable.</param>
        public void Show(bool canUse = true)
        {
            gameObject.SetActive(true);
            
            // Set button interactability
            if (useButton != null)
            {
                useButton.interactable = canUse;
            }
            
            // Select first available button
            if (eventSystem != null)
            {
                if (canUse && useButton != null)
                {
                    eventSystem.SetSelectedGameObject(useButton.gameObject);
                }
                else if (discardButton != null)
                {
                    eventSystem.SetSelectedGameObject(discardButton.gameObject);
                }
            }
        }
        
        /// <summary>
        /// Hides the action menu.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            OnMenuClosed?.Invoke();
        }
        
        private void HandleUsePressed()
        {
            OnUseRequested?.Invoke();
            
            if (hideOnAction)
                Hide();
        }
        
        private void HandleDiscardPressed()
        {
            OnDiscardRequested?.Invoke();
            
            if (hideOnAction)
                Hide();
        }
    }
}

