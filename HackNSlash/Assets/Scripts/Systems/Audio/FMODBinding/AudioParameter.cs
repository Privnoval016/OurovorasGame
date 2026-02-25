using UnityEngine;

[CreateAssetMenu(menuName = "AudioReferences/Parameter")]
public class AudioParameter : ScriptableObject
{
    [Tooltip("Matches the parameter name in FMOD.")]
    public string parameterName;
}