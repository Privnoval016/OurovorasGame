using System;
using UnityEngine;

namespace Extensions.UI
{
    /// <summary>
    /// Data container for inventory item display.
    /// Includes reference to actual item for proper bijective identification.
    /// </summary>
    public class ItemUIInfo<T>
    {
        /// <summary>
        /// Reference to the actual InventoryItem object (for unique identification).
        /// CRITICAL: Use this instead of itemName for comparisons!
        /// </summary>
        public T itemReference;
        
        public Sprite icon;
        public string itemName;
        public string itemDescription;
        public bool isStackable;
        public int amount;
        public Rarity rarity;
        public string category;
    }
}

