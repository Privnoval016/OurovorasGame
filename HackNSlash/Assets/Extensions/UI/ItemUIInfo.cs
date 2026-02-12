using System;
using UnityEngine;

namespace Extensions.UI
{
    /// <summary>
    /// Data container for inventory item display.
    /// Uses GUID for unique identification to avoid reference equality issues.
    /// </summary>
    public class ItemUIInfo<T>
    {
        /// <summary>
        /// Unique identifier for this item instance.
        /// CRITICAL: Use this for comparisons instead of reference equality!
        /// </summary>
        public string guid;
        
        /// <summary>
        /// Reference to the actual object (for data access).
        /// DO NOT use for equality comparisons - use guid instead!
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

