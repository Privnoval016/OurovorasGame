using UnityEngine;

/// <summary>
/// Placeholder data class for compendium entries (lore, enemies, items, etc.).
/// This is a temporary implementation until the full compendium system is designed.
/// </summary>
[CreateAssetMenu(fileName = "NewCompendiumEntry", menuName = "Compendium/Entry")]
public class CompendiumEntry : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique identifier for this entry")]
    public string entryId;
    
    [Tooltip("Display name of this entry")]
    public string entryName;
    
    [Tooltip("Category (e.g., Lore, Enemies, Items, Locations)")]
    public string category;
    
    [Tooltip("Detailed description")]
    [TextArea(3, 10)]
    public string description;
    
    [Tooltip("Icon for this entry")]
    public Sprite icon;
    
    [Header("Discovery")]
    [Tooltip("Whether this entry has been discovered by the player")]
    public bool isDiscovered;
}

