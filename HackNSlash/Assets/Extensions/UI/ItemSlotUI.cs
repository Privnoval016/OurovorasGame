using TMPro;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Extensions.UI
{
    /// <summary>
    /// Modular UI component for displaying an equipment/item slot.
    /// Reusable across different menu contexts (equipment, passives, etc.).
    /// Uses PrimeTween for smooth animations with unscaled time.
    /// Controller-only navigation - no mouse support.
    /// </summary>
    public class ItemSlotUI : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private GameObject emptyIndicator;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color emptyColor = Color.gray;
        
        [Header("Text Colors")]
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color selectedTextColor = Color.yellow;
        [SerializeField] private Color emptyTextColor = Color.gray;
        
        [Header("Events")]
        [SerializeField] private UnityEvent<int> onSlotSelected;
        [SerializeField] private UnityEvent onSlotDeselected;
        
        private int slotIndex = -1;
        private bool isEmpty = true;
        private bool isSelected = false;
        private EquippedItemDisplayData currentData; // Store current item data
        
        #region Public Methods
        
        /// <summary>
        /// Initializes the slot with an index.
        /// </summary>
        /// <param name="index">The slot index.</param>
        public void Initialize(int index)
        {
            slotIndex = index;
            SetEmpty();
        }
        
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
        
        public void OnSelect(BaseEventData eventData)
        {
            Select();
        }
        
        public void OnDeselect(BaseEventData eventData)
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
    
    private void Select()
    {
        isSelected = true;
        UpdateVisuals();
        
        // Animate selection with PrimeTween (unscaled time for menu)
        if (borderImage != null)
        {
            Tween.Scale(borderImage.transform, 1.05f, duration: 0.15f, 
                ease: Ease.OutBack, useUnscaledTime: true);
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
        if (borderImage != null)
        {
            Tween.Scale(borderImage.transform, 1f, duration: 0.15f, 
                ease: Ease.OutQuad, useUnscaledTime: true);
        }
        
        // NOTE: Removed onSlotDeselected callback to prevent circular dependency
        // Scroll menu should only close via Cancel (B button), not when slot is deselected
    }
        
    private void UpdateVisuals()
    {
        Color targetBorderColor = normalColor;
        Color targetTextColor = normalTextColor;
        
        if (isSelected)
        {
            targetBorderColor = selectedColor;
            targetTextColor = selectedTextColor;
        }
        else if (isEmpty)
        {
            targetBorderColor = emptyColor;
            targetTextColor = emptyTextColor;
        }
        
        // Update border
        if (borderImage != null)
            borderImage.color = targetBorderColor;
        
        // Update background
        if (backgroundImage != null)
        {
            Color bgColor = isEmpty ? emptyColor : normalColor;
            bgColor.a = backgroundImage.color.a; // Preserve alpha
            backgroundImage.color = bgColor;
        }
        
        // CRITICAL: Update text color for visibility
        if (nameText != null)
            nameText.color = targetTextColor;
    }
        
        #endregion
    }
}

