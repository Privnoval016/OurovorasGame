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
    
    [Header("Player SFX Events")]
    [Tooltip("Audio event for player footstep sound effect.")]
    public AudioEvent playerFootstepSound;
    [Tooltip("Audio event for player jump sound effect.")]
    public AudioEvent playerJumpSound;
    [Tooltip("Audio event for player landing sound effect.")]
    public AudioEvent playerLandSound;
    
    [Header("Standard Enemy SFX Events")]
    [Tooltip("Audio event for enemy death sound effect.")]
    public AudioEvent enemyDeathSound;
    [Tooltip("Audio event for enemy hit sound effect.")]
    public AudioEvent enemyHitSound;
    [Tooltip("Audio event for enemy alert sound effect.")]
    public AudioEvent enemyAlertSound;
}