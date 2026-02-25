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

    [Header("Default Parameters")]
    public DefaultParam[] defaultParameters;

    [System.Serializable]
    public struct DefaultParam
    {
        public AudioParameter parameter;
        public float value;
    }
}