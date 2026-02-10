using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Makes a GameObject selectable without being a Button/Selectable.
    /// Used for scroll menus to receive input events like Cancel (B button) and Submit (A button).
    /// CRITICAL: Without this, scroll menus can't receive B button to close or A button to select.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SelectableContainer : Selectable, ISubmitHandler, ICancelHandler
    {
        [Header("References")]
        [Tooltip("The ScrollMenu component to notify when item is confirmed")]
        [SerializeField] private ScrollMenu scrollMenu;
        
        [Header("Events")]
        [Tooltip("Called when A button is pressed")]
        public UnityEngine.Events.UnityEvent onSubmit;
        
        [Tooltip("Called when B button is pressed")]
        public UnityEngine.Events.UnityEvent onCancel;
        
        protected override void Awake()
        {
            base.Awake();
            
            if (scrollMenu == null)
                scrollMenu = GetComponentInParent<ScrollMenu>();
                
            // CRITICAL: Ensure this is interactable so it receives input events
            interactable = true;
            
            // Set navigation to explicit to prevent auto-navigation issues
            navigation = new Navigation { mode = Navigation.Mode.None };
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Ensure we stay interactable when enabled
            interactable = true;
            
            Debug.Log($"SelectableContainer.OnEnable - interactable: {interactable}, isActiveAndEnabled: {isActiveAndEnabled}");
        }
        
        private void Update()
        {
            // FALLBACK: Manually check for Submit input since EventSystem routing isn't working
            // This ensures A button works even if EventSystem doesn't route to ISubmitHandler
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                // Check if Submit (A button) was pressed this frame
                if (Input.GetButtonDown("Submit"))
                {
                    Debug.Log("SelectableContainer: Manual Submit detection via Input.GetButtonDown");
                    OnSubmit(null);
                }
            }
        }
        
        /// <summary>
        /// Override to prevent default button behavior.
        /// This component exists only to make the GameObject selectable.
        /// </summary>
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            // Do nothing - we don't want visual transitions
        }
        
        /// <summary>
        /// Sets this container as the EventSystem's selected object.
        /// Call this when activating a scroll menu or other input-receiving UI.
        /// </summary>
        public void SelectThis()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }
        
        /// <summary>
        /// Called when A button is pressed - confirms item selection.
        /// </summary>
        public void OnSubmit(BaseEventData eventData)
        {
            Debug.Log($"SelectableContainer.OnSubmit called! ScrollMenu: {scrollMenu != null}, EventData: {eventData}");
            
            if (scrollMenu != null)
            {
                int selectedIndex = scrollMenu.GetSelectedIndex();
                Debug.Log($"SelectableContainer: Item confirmed at index {selectedIndex}");
                scrollMenu.OnItemConfirmed?.Invoke(selectedIndex);
            }
            else
            {
                Debug.LogWarning("SelectableContainer: ScrollMenu is null!");
            }
            
            onSubmit?.Invoke();
            UIAudio.PlaySelect();
        }
        
        /// <summary>
        /// Called when B button is pressed - used to close the menu.
        /// </summary>
        public void OnCancel(BaseEventData eventData)
        {
            onCancel?.Invoke();
            UIAudio.PlayBack();
        }
    }
}

