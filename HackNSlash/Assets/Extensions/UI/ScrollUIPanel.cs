using System;
using UnityEngine;

namespace Extensions.UI
{
    public abstract class ScrollUIPanel : MonoBehaviour
    {
        [Header("Inspector References")]
        public RectTransform rectTransform;
        
        [Header("Equipped Indicator (Optional)")]
        [SerializeField] protected UnityEngine.UI.Image equippedIndicator;
        
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