using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Items/Consumable")]
public class Consumable : InventoryItem
{
    [Header("Consumable Details")]
    public InnateStatChange[] statChanges; // Array of stat changes this consumable provides

    public float useDuration = 0f; // Duration for effects that take time to apply
}