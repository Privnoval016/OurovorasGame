using System;
using UnityEngine;

namespace Extensions.UI
{
    /// <summary>
    /// Common display data extracted from any ItemUIInfo<T>.
    /// Used to pass data to scroll panels without generic type issues.
    /// </summary>
    public struct ItemDisplayData
    {
        public string guid;
        public string itemName;
        public string itemDescription;
        public Sprite icon;
        public Rarity rarity;
        public int amount;
        public bool isStackable;
        public string category;
        
        public ItemDisplayData(string guid, string itemName, string itemDescription, Sprite icon, Rarity rarity, int amount, bool isStackable, string category)
        {
            this.guid = guid;
            this.itemName = itemName;
            this.itemDescription = itemDescription;
            this.icon = icon;
            this.rarity = rarity;
            this.amount = amount;
            this.isStackable = isStackable;
            this.category = category;
        }
    }
    
    public abstract class ScrollUIPanel : MonoBehaviour
    {
        [Header("Inspector References")]
        public RectTransform rectTransform;
        
        [Header("Equipped Indicator (Optional)")]
        [SerializeField] protected UnityEngine.UI.Image equippedIndicator;
        
        /// <summary>
        /// Conversion function to extract display data from any ItemUIInfo<T>.
        /// Set by ScrollMenu when activating.
        /// </summary>
        protected Func<object, ItemDisplayData> extractDisplayData;
        
        private void Awake()
        {
            rectTransform ??= GetComponent<RectTransform>();
            
            // Hide equipped indicator by default
            if (equippedIndicator != null)
                equippedIndicator.gameObject.SetActive(false);
        }

        public abstract void OnSelected(); // called when the panel is hovered over
        
        public abstract void OnDeselected(); // called when the panel is no longer hovered over
        
        public abstract void Refresh(object info); // called to update the panel with new info (ItemUIInfo<T> boxed as object)
        
        /// <summary>
        /// Sets the conversion function for extracting display data from any ItemUIInfo<T>.
        /// </summary>
        public void SetDisplayDataExtractor(Func<object, ItemDisplayData> extractor)
        {
            extractDisplayData = extractor;
        }
        
        /// <summary>
        /// Sets the equipped indicator visibility.
        /// </summary>
        /// <param name="isEquipped">Whether the item is equipped.</param>
        public virtual void SetEquippedIndicator(bool isEquipped)
        {
            if (equippedIndicator != null)
                equippedIndicator.gameObject.SetActive(isEquipped);
        }
    }
}