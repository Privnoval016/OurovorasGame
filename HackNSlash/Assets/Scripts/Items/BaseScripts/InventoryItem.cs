using Extensions.UI;
using UnityEngine;

public abstract class InventoryItem : ScriptableObject
{
    [Header("Item Details")]
    public string itemName;
    public string itemDescription;
    public Rarity itemRarity;
    public Sprite itemIcon;
    
    [Header("Item Properties")]
    public int purchasePrice; // Price to purchase the item
    public int SellPrice => Mathf.RoundToInt(purchasePrice * 0.5f); // Price to sell the item, typically half of purchase price

}

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}
