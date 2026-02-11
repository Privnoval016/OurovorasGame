using System;
using System.Linq;
using UnityEngine;

/**
 * <summary>
 * Class to hold information about unlocks that occur during runtime.
 * This can include things like new abilities, items, or features that become available to the player as they progress through the game.
 * Important that this isnt a scriptable object so we can modify it at runtime without affecting the base data.
 * Lets us easily save and load unlock progress without needing to serialize the entire player status.
 * </summary>
 */
public class RuntimePlayerStatus : MonoBehaviour
{
    [Header("Level")]
    [field: SerializeField] public int Level { get; private set; } = 10;
    
    
    [Header("Element")]
    public ElementLoadout[] elementLoadouts;
    public int currentElementLoadoutIndex = 0;
    public ElementLoadout CurrentElementLoadout => elementLoadouts.Length > 0 ? elementLoadouts[currentElementLoadoutIndex] : null;
    
    public ElementEffect[] elementEffects = Array.Empty<ElementEffect>();
    
    public ElementUnlockRuntimeInfo[] elementUnlocks = Array.Empty<ElementUnlockRuntimeInfo>();
    
    [Header("Inventory")]
    
    public EquipmentLoadout[] loadouts = Array.Empty<EquipmentLoadout>();
    public int currentLoadoutIndex = 0;
    public EquipmentLoadout CurrentLoadout => loadouts.Length > 0 ? loadouts[currentLoadoutIndex] : null;
    
    public InventoryInfo inventoryInfo;
    
    #region Saving and Loading
    
    // TODO: Saving and loading
    
    #endregion
}

[Serializable]
public class ElementUnlockRuntimeInfo
{
    public ElementUnlocks elementUnlock;
    public float unlockLevelProgress;
    public int CurrentLevel => Mathf.FloorToInt(unlockLevelProgress);

    public void Initialize(EvaluatedStats stats)
    {
        elementUnlock.InitializeStatChanges(stats, CurrentLevel);
    }

    public ElementProgressEntry[] GetUnlockedEntries()
    {
        return elementUnlock.progressionEntries
            .Where(entry => CurrentLevel >= entry.levelRequirement)
            .ToArray();
    }
}