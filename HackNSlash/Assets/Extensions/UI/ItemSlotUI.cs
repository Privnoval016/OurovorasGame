using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Extensions.UI
{
    /// <summary>
    /// Modular UI component for displaying an equipment/item slot.
    /// Reusable across different menu contexts (equipment, passives, etc.).
    /// Uses UIAnimationManager for consistent animations.
    /// Controller-only navigation - no mouse support.
    /// </summary>
    public class ItemSlotUI : Selectable, ISubmitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private GameObject emptyIndicator;
        
        [Header("Events")]
        [SerializeField] private UnityEvent<int> onSlotSelected;
        [SerializeField] private UnityEvent onSlotDeselected;
        
        private int slotIndex = -1;
        private bool isEmpty = true;
        private bool isSelected = false;
        private EquippedItemDisplayData currentData; // Store current item data
        
        private UIAnimationManager animationManager;
        
        #region Public Methods
        
        /// <summary>
        /// Initializes the slot with an index.
        /// </summary>
        /// <param name="index">The slot index.</param>
        public void Initialize(int index)
        {
            slotIndex = index;
            
            // Get animation manager
            animationManager = UIAnimationManager.Instance;
        }
        
        // ...existing SetItemData, SetEmpty, SetEquippedIndicator, GetSlotIndex, IsEmpty, GetCurrentItemData, ForceDeselect methods...
        
        /// <summary>
        /// Sets the slot to display item data.
        /// </summary>
        /// <param name="data">The item data to display.</param>
        public void SetItemData(EquippedItemDisplayData data)
        {
            currentData = data; // Store for later retrieval
            
            if (data == null || data.isEmpty)
            {
                SetEmpty();
                return;
            }
            
            isEmpty = false;
            
            if (iconImage != null)
            {
                iconImage.sprite = data.icon;
                iconImage.enabled = data.icon != null;
            }
            
            if (nameText != null)
                nameText.text = data.itemName;
            
            if (emptyIndicator != null)
                emptyIndicator.SetActive(false);
            
            UpdateVisuals();
        }
        
        /// <summary>
        /// Sets the slot to empty state.
        /// </summary>
        public void SetEmpty()
        {
            isEmpty = true;
            currentData = null; // Clear stored data
            
            if (iconImage != null)
                iconImage.enabled = false;
            
            if (nameText != null)
                nameText.text = "Empty";
            
            if (emptyIndicator != null)
                emptyIndicator.SetActive(true);
            
            UpdateVisuals();
        }
        
        /// <summary>
        /// Gets the slot index.
        /// </summary>
        public int GetSlotIndex() => slotIndex;
        
        /// <summary>
        /// Gets whether the slot is empty.
        /// </summary>
        public bool IsEmpty() => isEmpty;
        
        /// <summary>
        /// Gets the currently displayed item data.
        /// Returns null if slot is empty.
        /// </summary>
        public EquippedItemDisplayData GetCurrentItemData() => currentData;
        
        /// <summary>
        /// Forces deselection without EventSystem involvement.
        /// Used for cleanup when switching tabs.
        /// </summary>
        public void ForceDeselect()
        {
            if (isSelected)
            {
                Deselect();
            }
        }
        
        #endregion
        
        #region Event System Handlers
        
        public override void OnSelect(BaseEventData eventData)
        {
            Selected();
        }
        
        public override void OnDeselect(BaseEventData eventData)
        {
            Deselect();
        }
        
        public void OnSubmit(BaseEventData eventData)
        {
            // Handle controller A button press - ONLY call callback on Submit, not Select
            UIAudio.PlaySelect();
            onSlotSelected?.Invoke(slotIndex);
        }
        
        #endregion
        
        #region Selection
        
        private void Selected()
        {
            isSelected = true;
            UpdateVisuals();
            
            // Animate selection using animation manager
            if (animationManager != null && borderImage != null)
            {
                animationManager.CreateBuilder(borderImage.transform)
                    .AnimateSelection();
            }
            
            // Play hover sound via EventBus
            UIAudio.PlayHover();
            
            // NOTE: Do NOT invoke onSlotSelected here - only on Submit (A button press)
            // Just selecting/hovering over a slot should not open the scroll menu
        }
        
        private void Deselect()
        {
            isSelected = false;
            UpdateVisuals();
            
            // Animate deselection
            if (animationManager != null && borderImage != null)
            {
                animationManager.CreateBuilder(borderImage.transform)
                    .AnimateDeselection();
            }
            
            // NOTE: Removed onSlotDeselected callback to prevent circular dependency
            // Scroll menu should only close via Cancel (B button), not when slot is deselected
        }
            
        private void UpdateVisuals()
        {
            if (animationManager == null)
            {
                animationManager = UIAnimationManager.Instance;
                if (animationManager == null) return;
            }
            
            Color targetBorderColor;
            Color targetTextColor;
            
            if (isSelected)
            {
                targetBorderColor = animationManager.GetSelectedColor();
                targetTextColor = animationManager.GetSelectedTextColor();
            }
            else if (isEmpty)
            {
                targetBorderColor = animationManager.GetEmptyColor();
                targetTextColor = animationManager.GetEmptyTextColor();
            }
            else
            {
                targetBorderColor = animationManager.GetNormalColor();
                targetTextColor = animationManager.GetNormalTextColor();
            }
            
            // Update border
            if (borderImage != null)
                borderImage.color = targetBorderColor;
            
            // Update background
            if (backgroundImage != null)
            {
                Color bgColor = isEmpty ? animationManager.GetEmptyColor() : animationManager.GetNormalColor();
                bgColor.a = backgroundImage.color.a; // Preserve alpha
                backgroundImage.color = bgColor;
            }
            
            // Update text color
            if (nameText != null)
                nameText.color = targetTextColor;
        }
        
        #endregion
    }
}

