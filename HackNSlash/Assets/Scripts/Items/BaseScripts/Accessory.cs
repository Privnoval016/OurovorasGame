using UnityEngine;

[CreateAssetMenu(fileName = "New Accessory", menuName = "Inventory/Items/Accessory")]
public class Accessory : InventoryItem
{
    [Header("Accessory Details")]
    
    public InnateStatChange[] statChanges; // Array of stat changes this accessory provides
    
    public EquipmentEffect[] equipmentEffects; // Array of effects this accessory provides
}

public enum EquipmentEffect
{
    None
}
