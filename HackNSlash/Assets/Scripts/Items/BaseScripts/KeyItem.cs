using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Key Item", menuName = "Inventory/Items/Key Item")]
public class KeyItem : InventoryItem
{
    private void OnValidate()
    {
        purchasePrice = -1; // Key items typically cannot be purchased
    }
}
