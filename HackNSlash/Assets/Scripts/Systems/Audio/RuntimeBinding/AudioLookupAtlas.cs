using UnityEngine;

/**
 * <summary>
 * Manages audio lookup references for various game systems.
 * </summary>
 */
[CreateAssetMenu(menuName = "AudioReferences/Audio Lookup Atlas")]
public class AudioLookupAtlas : ScriptableObject
{
    #region Static Lookup
    
    public static AudioLookupAtlas Instance;
    
    public static void Initialize(AudioLookupAtlas atlas)
    {
        Instance = atlas;
    }
    
    #endregion
    
    [Header("Music Tracks")]
    [Tooltip("Audio event for overworld music.")]
    public AudioEvent explorationMusicEvent;
    
    [Header("Common Music Parameters")]
    [Tooltip("Parameter controlling the intensity of the music.")]
    public AudioParameter musicIntensityParam;
    [Tooltip("Parameter controlling the state of the music (e.g., non-combat, regular encounter, boss encounter).")]
    public AudioParameter musicStateParam;
}