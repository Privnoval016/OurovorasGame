using FMOD.Studio;
using UnityEngine;

[CreateAssetMenu(menuName = "AudioReferences/Snapshot")]
public class AudioSnapshot : ScriptableObject
{
    [Tooltip("The FMOD snapshot event to transition to.")]
    public EventDescription snapshotDesc;
    public float fadeTime = 0.5f;
}