using System.Collections.Generic;
using Extensions.Patterns;
using Extensions.UI;
using UnityEngine;

/// <summary>
/// Implementation of ICompendiumDataProvider that bridges the UI with the compendium system.
/// This is the backend connector for Tab 7 (Compendium).
/// NOTE: Compendium system not yet implemented, this provides placeholder data structure.
/// </summary>
public class CompendiumDataProvider : MonoBehaviour, ICompendiumDataProvider, IService
{
    [Header("Placeholder Compendium Data")]
    [SerializeField] private List<CompendiumEntryData> placeholderEntries = new List<CompendiumEntryData>();
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        Services.Register<CompendiumDataProvider>(this);
        InitializePlaceholderData();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializePlaceholderData()
    {
        // Create placeholder entries if none exist
        if (placeholderEntries.Count == 0)
        {
            // Lore entries
            placeholderEntries.Add(new CompendiumEntryData
            {
                entryName = "The Five Elements",
                description = "Information about the five elemental forces that govern the world.",
                category = "Lore",
                isDiscovered = true
            });
            
            placeholderEntries.Add(new CompendiumEntryData
            {
                entryName = "Ancient Civilization",
                description = "Details about the civilization that existed before the cataclysm.",
                category = "Lore",
                isDiscovered = false
            });
            
            // Enemy entries
            placeholderEntries.Add(new CompendiumEntryData
            {
                entryName = "Shadow Beast",
                description = "A fearsome creature born from corrupted elemental energy.",
                category = "Enemies",
                isDiscovered = true
            });
            
            // Location entries
            placeholderEntries.Add(new CompendiumEntryData
            {
                entryName = "Crystal Caverns",
                description = "A network of caves filled with elemental crystals.",
                category = "Locations",
                isDiscovered = false
            });
        }
    }
    
    #endregion
    
    #region ICompendiumDataProvider Implementation
    
    /// <summary>
    /// Gets all compendium entries.
    /// </summary>
    public List<CompendiumEntryData> GetAllEntries()
    {
        // TODO: Replace with actual compendium system
        return new List<CompendiumEntryData>(placeholderEntries);
    }
    
    /// <summary>
    /// Gets entries filtered by category.
    /// </summary>
    public List<CompendiumEntryData> GetEntriesByCategory(string category)
    {
        var filtered = new List<CompendiumEntryData>();
        
        foreach (var entry in placeholderEntries)
        {
            if (entry.category == category)
                filtered.Add(entry);
        }
        
        return filtered;
    }
    
    /// <summary>
    /// Gets all available compendium categories.
    /// </summary>
    public string[] GetCategories()
    {
        return new string[]
        {
            "Lore",
            "Enemies",
            "Locations",
            "Characters"
        };
    }
    
    #endregion
}

