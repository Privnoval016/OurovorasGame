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
    /// </summary>
    public class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
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
        
        [Header("Events")]
        [SerializeField] private UnityEvent<int> onSlotSelected;
        [SerializeField] private UnityEvent onSlotDeselected;
        
        private int slotIndex = -1;
        private bool isEmpty = true;
        private bool isSelected = false;
        
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
        
        #endregion
        
        #region Event System Handlers
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            Select();
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isSelected)
                Deselect();
        }
        
        public void OnSelect(BaseEventData eventData)
        {
            Select();
        }
        
        public void OnDeselect(BaseEventData eventData)
        {
            Deselect();
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
        
        onSlotSelected?.Invoke(slotIndex);
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
        
        onSlotDeselected?.Invoke();
    }
        
        private void UpdateVisuals()
        {
            Color targetColor = normalColor;
            
            if (isSelected)
                targetColor = selectedColor;
            else if (isEmpty)
                targetColor = emptyColor;
            
            if (borderImage != null)
                borderImage.color = targetColor;
            
            if (backgroundImage != null)
            {
                Color bgColor = isEmpty ? emptyColor : normalColor;
                bgColor.a = backgroundImage.color.a; // Preserve alpha
                backgroundImage.color = bgColor;
            }
        }
        
        #endregion
    }
}

