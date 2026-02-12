namespace Extensions.UI
{
    /// <summary>
    /// Defines how items should be sorted in inventory displays.
    /// </summary>
    public enum InventorySortMethod
    {
        /// <summary>
        /// Sort alphabetically by item name (A-Z).
        /// </summary>
        NameAscending,
        
        /// <summary>
        /// Sort alphabetically by item name (Z-A).
        /// </summary>
        NameDescending,
        
        /// <summary>
        /// Sort by rarity (Legendary → Common).
        /// </summary>
        RarityDescending,
        
        /// <summary>
        /// Sort by rarity (Common → Legendary).
        /// </summary>
        RarityAscending,
        
        /// <summary>
        /// Sort by quantity (Most → Least).
        /// </summary>
        QuantityDescending,
        
        /// <summary>
        /// Sort by quantity (Least → Most).
        /// </summary>
        QuantityAscending,
        
        /// <summary>
        /// Sort by date obtained (Newest first).
        /// Placeholder for future implementation.
        /// </summary>
        DateObtainedNewest,
        
        /// <summary>
        /// Sort by date obtained (Oldest first).
        /// Placeholder for future implementation.
        /// </summary>
        DateObtainedOldest,
        
        /// <summary>
        /// Sort by item type/category.
        /// </summary>
        Category
    }
}

