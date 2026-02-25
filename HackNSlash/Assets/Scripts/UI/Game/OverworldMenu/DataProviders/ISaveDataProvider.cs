using System;
using System.Collections.Generic;

/// <summary>
/// Interface for save/load data provider.
/// Separates UI from actual save system implementation.
/// </summary>
public interface ISaveDataProvider
{
    /// <summary>
    /// Gets save data for all slots (autosave + 3 manual saves).
    /// </summary>
    List<SaveSlotData> GetAllSaveSlots();
    
    /// <summary>
    /// Gets save data for a specific slot.
    /// </summary>
    /// <param name="slotIndex">-1 for autosave, 0-2 for manual saves</param>
    SaveSlotData GetSaveSlot(int slotIndex);
    
    /// <summary>
    /// Saves game to the specified slot.
    /// </summary>
    bool SaveToSlot(int slotIndex);
    
    /// <summary>
    /// Loads game from the specified slot.
    /// </summary>
    bool LoadFromSlot(int slotIndex);
    
    /// <summary>
    /// Checks if a slot has save data.
    /// </summary>
    bool IsSlotEmpty(int slotIndex);
}

/// <summary>
/// Data container for save slot display information.
/// </summary>
[Serializable]
public class SaveSlotData
{
    public int slotIndex; // -1 for autosave, 0-2 for manual
    public bool isEmpty;
    public string saveName;
    public string timestamp;
    public int playerLevel;
    public string location;
    public float playtime;
}

