using UnityEngine;

[CreateAssetMenu(fileName = "MissionEntryData", menuName = "ScriptableObjects/MissionEntryData", order = 1)]
public class MissionEntryData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique identifier for this mission")]
    public string missionId;

    [Tooltip("Display name of this mission")]
    public string missionName;

    [Tooltip("Detailed description of the mission")]
    [TextArea(3, 10)]
    public string description;

    [Tooltip("Icon representing the mission")]
    public Sprite icon;

    [Header("Mission Details")]
    [Tooltip("Objectives or tasks required to complete the mission")]
    public string[] objectives;

    [Tooltip("Rewards for completing the mission (e.g., items, experience)")]
    public string[] rewards;

    [Header("Discovery")]
    [Tooltip("Whether this mission has been discovered by the player")]
    public bool isDiscovered;
}
