using Extensions.UI;
using FMODUnity;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Audio settings menu - controls FMOD bus volumes.
/// Uses AudioSystem for sound playback (never calls FMOD directly for UI sounds).
/// </summary>
public class AudioSettingsMenu : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    
    [Header("Volume Text")]
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    
    [Header("Save/Reset")]
    [SerializeField] private SettingsButton saveButton;
    [SerializeField] private SettingsButton resetButton;
    [SerializeField] private TextMeshProUGUI saveStatusText;
    
    [Header("Settings Keys")]
    [SerializeField] private string masterVolumeKey = "MasterVolume";
    [SerializeField] private string musicVolumeKey = "MusicVolume";
    [SerializeField] private string sfxVolumeKey = "SFXVolume";
    
    [Header("Default Selection")]
    [SerializeField] private Selectable defaultButton;
    
    private EventSystem eventSystem;
    
    private void Awake()
    {
        eventSystem = EventSystem.current;
        
        // Initialize sliders
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        
        // Initialize buttons
        if (saveButton != null)
        {
            saveButton.Initialize("Save");
            saveButton.onPressed.AddListener(SaveSettings);
        }
        
        if (resetButton != null)
        {
            resetButton.Initialize("Reset to Default");
            resetButton.onPressed.AddListener(ResetToDefault);
        }
    }
    
    /// <summary>
    /// Shows the audio settings menu and loads current values.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        
        LoadSettings();
        
        // Select default button
        if (defaultButton != null && eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(defaultButton.gameObject);
        }
    }
    
    /// <summary>
    /// Hides the audio settings menu.
    /// </summary>
    public void Hide()
    {
        SaveSettings(); // Auto-save when leaving
        gameObject.SetActive(false);
    }
    
    #region Volume Control
    
    private void SetMasterVolume(float value)
    {
        // Set FMOD bus volume (Master bus)
        FMOD.Studio.Bus masterBus = RuntimeManager.GetBus("bus:/");
        if (masterBus.isValid())
        {
            masterBus.setVolume(value);
        }
        
        if (masterVolumeText != null)
        {
            int targetPercent = Mathf.RoundToInt(value * 100);
            masterVolumeText.text = $"{targetPercent}%";
        }
        
        // Play feedback sound at new volume
        UIAudio.PlayHover();
    }
    
    private void SetMusicVolume(float value)
    {
        // Set FMOD bus volume (Music bus)
        FMOD.Studio.Bus musicBus = RuntimeManager.GetBus("bus:/Music");
        if (musicBus.isValid())
        {
            musicBus.setVolume(value);
        }
        
        if (musicVolumeText != null)
        {
            int targetPercent = Mathf.RoundToInt(value * 100);
            musicVolumeText.text = $"{targetPercent}%";
        }
    }
    
    private void SetSFXVolume(float value)
    {
        // Set FMOD bus volume (SFX bus)
        FMOD.Studio.Bus sfxBus = RuntimeManager.GetBus("bus:/SFX");
        if (sfxBus.isValid())
        {
            sfxBus.setVolume(value);
        }
        
        if (sfxVolumeText != null)
        {
            int targetPercent = Mathf.RoundToInt(value * 100);
            sfxVolumeText.text = $"{targetPercent}%";
        }
        
        // Play feedback sound at new volume
        UIAudio.PlaySelect();
    }
    
    #endregion
    
    #region Save/Load
    
    private void SaveSettings()
    {
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat(masterVolumeKey, masterVolumeSlider.value);
        
        if (musicVolumeSlider != null)
            PlayerPrefs.SetFloat(musicVolumeKey, musicVolumeSlider.value);
        
        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat(sfxVolumeKey, sfxVolumeSlider.value);
        
        PlayerPrefs.Save();
        
        ShowSaveConfirmation();
    }
    
    private void LoadSettings()
    {
        if (masterVolumeSlider != null)
        {
            float value = PlayerPrefs.GetFloat(masterVolumeKey, 1f);
            masterVolumeSlider.value = value;
        }
        
        if (musicVolumeSlider != null)
        {
            float value = PlayerPrefs.GetFloat(musicVolumeKey, 1f);
            musicVolumeSlider.value = value;
        }
        
        if (sfxVolumeSlider != null)
        {
            float value = PlayerPrefs.GetFloat(sfxVolumeKey, 1f);
            sfxVolumeSlider.value = value;
        }
    }
    
    private void ResetToDefault()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 1f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1f;
        
        SaveSettings();
        UIAudio.PlaySelect();
    }
    
    private void ShowSaveConfirmation()
    {
        if (saveStatusText == null) return;
        
        saveStatusText.text = "Settings Saved!";
        
        // Animate the save status text
        saveStatusText.transform.localScale = Vector3.one * 0.8f;
        saveStatusText.color = new Color(saveStatusText.color.r, saveStatusText.color.g, 
            saveStatusText.color.b, 0f);
        
        Sequence.Create(useUnscaledTime: true)
            .Group(Tween.Scale(saveStatusText.transform, 1f, duration: 0.3f, ease: Ease.OutBack))
            .Group(Tween.Alpha(saveStatusText, 1f, duration: 0.2f, ease: Ease.OutQuad))
            .ChainDelay(1.5f)
            .Chain(Tween.Alpha(saveStatusText, 0f, duration: 0.3f, ease: Ease.InQuad));
        
        UIAudio.PlaySelect();
    }
    
    #endregion
}

