using UnityEngine;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "AudioReferences/Event")]
public class AudioEvent : ScriptableObject
{
    [Header("Information")]
    public EventReference eventReference;
    
    [TextArea]
    public string description;

    public int poolSize = 10;

    [Header("Randomization Metadata")]
    
    [MinMaxSlider(0f, 2f)]
    [Tooltip("Volume range for randomization.")]
    public Vector2 randomVolumeRange = new Vector2(1f, 1f);
    
    [MinMaxSlider(-3f, 3f)]
    [Tooltip("Pitch range for randomization.")]
    public Vector2 randomPitchRange = new Vector2(1f, 1f);

    [Header("Default Parameters")]
    public DefaultParam[] defaultParameters;

    [System.Serializable]
    public struct DefaultParam
    {
        public AudioParameter parameter;
        public float value;
    }
}