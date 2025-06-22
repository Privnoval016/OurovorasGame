using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Items/Consumable")]
public class Consumable : InventoryItem
{
    [Header("Consumable Details")]
    public StatChange[] statChanges; // Array of stat changes this consumable provides
    public UseEffect[] useEffects; // Array of effects this consumable provides buffs in
    public float useDuration = 0f; // Duration for effects that take time to apply
}

public enum UseEffect
{
    None,
    Instant,
    OverTime,
}
