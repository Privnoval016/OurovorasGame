using Extensions.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

/// <summary>
/// Tab 8: Settings menu for audio, video, gameplay, and key remapping.
/// Provides standard game settings configuration with smooth PrimeTween animations.
/// Audio uses FMOD through AudioSystem (no Unity AudioMixer).
/// </summary>
public class SettingsTab : TabSelection
{
    [Header("Audio Settings - FMOD")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    
    [Header("Video Settings")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private Slider brightnessSlider;
    
    [Header("Gameplay Settings")]
    [SerializeField] private Slider cameraSensitivitySlider;
    [SerializeField] private Toggle invertYAxisToggle;
    [SerializeField] private TextMeshProUGUI cameraSensitivityText;
    
    [Header("Save/Load")]
    [SerializeField] private Button saveSettingsButton;
    [SerializeField] private Button loadSettingsButton;
    [SerializeField] private Button resetToDefaultButton;
    [SerializeField] private TextMeshProUGUI saveStatusText;
    
    [Header("Key Remapping")]
    [SerializeField] private GameObject keyRemappingPanel;
    [SerializeField] private Button openKeyRemappingButton;
    
    [Header("Settings Keys")]
    [SerializeField] private string masterVolumeKey = "MasterVolume";
    [SerializeField] private string musicVolumeKey = "MusicVolume";
    [SerializeField] private string sfxVolumeKey = "SFXVolume";
    [SerializeField] private string brightnessKey = "Brightness";
    [SerializeField] private string cameraSensitivityKey = "CameraSensitivity";
    
    private Resolution[] availableResolutions;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        InitializeSettings();
    }
    
    #endregion
    
    #region TabSelection Overrides
    
    /// <summary>
    /// Called when this tab is selected.
    /// </summary>
    public override void OnTabSelect()
    {
        base.OnTabSelect();
        LoadSettings();
    }
    
    /// <summary>
    /// Called when this tab is deselected.
    /// </summary>
    public override void OnTabDeselect()
    {
        base.OnTabDeselect();
        SaveSettings();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeSettings()
    {
        // Initialize audio sliders
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        
        // Initialize video settings
        InitializeResolutionDropdown();
        InitializeQualityDropdown();
        
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        
        if (vsyncToggle != null)
            vsyncToggle.onValueChanged.AddListener(SetVSync);
        
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        
        // Initialize gameplay settings
        if (cameraSensitivitySlider != null)
            cameraSensitivitySlider.onValueChanged.AddListener(SetCameraSensitivity);
        
        if (invertYAxisToggle != null)
            invertYAxisToggle.onValueChanged.AddListener(SetInvertYAxis);
        
        // Initialize buttons
        if (saveSettingsButton != null)
            saveSettingsButton.onClick.AddListener(SaveSettings);
        
        if (loadSettingsButton != null)
            loadSettingsButton.onClick.AddListener(LoadSettings);
        
        if (resetToDefaultButton != null)
            resetToDefaultButton.onClick.AddListener(ResetToDefault);
        
        if (openKeyRemappingButton != null)
            openKeyRemappingButton.onClick.AddListener(OpenKeyRemapping);
    }
    
    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null)
            return;
        
        availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        
        var options = new System.Collections.Generic.List<string>();
        int currentResolutionIndex = 0;
        
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            string option = $"{availableResolutions[i].width} x {availableResolutions[i].height} @ {availableResolutions[i].refreshRateRatio}Hz";
            options.Add(option);
            
            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }
    
    private void InitializeQualityDropdown()
    {
        if (qualityDropdown == null)
            return;
        
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
        qualityDropdown.onValueChanged.AddListener(SetQuality);
    }
    
    #endregion
    
    #region Audio Settings
    
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
            // Animate the text number change for smooth feel
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
    
    #region Video Settings
    
    private void SetResolution(int resolutionIndex)
    {
        if (availableResolutions == null || resolutionIndex < 0 || resolutionIndex >= availableResolutions.Length)
            return;
        
        Resolution resolution = availableResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
    
    private void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }
    
    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    
    private void SetVSync(bool enable)
    {
        QualitySettings.vSyncCount = enable ? 1 : 0;
    }
    
    private void SetBrightness(float value)
    {
        // Brightness implementation depends on your rendering setup
        // This is a placeholder that would typically adjust post-processing
        RenderSettings.ambientIntensity = value;
    }
    
    #endregion
    
    #region Gameplay Settings
    
    private void SetCameraSensitivity(float value)
    {
        if (cameraSensitivityText != null)
            cameraSensitivityText.text = value.ToString("F2");
        
        // The actual camera sensitivity would be applied through a camera controller
        // This just saves the value
    }
    
    private void SetInvertYAxis(bool inverted)
    {
        // The actual inversion would be applied through a camera controller
        // This just saves the value
    }
    
    #endregion
    
    #region Save/Load Settings
    
    /// <summary>
    /// Saves all current settings to PlayerPrefs.
    /// </summary>
    public void SaveSettings()
    {
        // Audio
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat(masterVolumeKey, masterVolumeSlider.value);
        
        if (musicVolumeSlider != null)
            PlayerPrefs.SetFloat(musicVolumeKey, musicVolumeSlider.value);
        
        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat(sfxVolumeKey, sfxVolumeSlider.value);
        
        // Video
        if (resolutionDropdown != null)
            PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
        
        if (qualityDropdown != null)
            PlayerPrefs.SetInt("QualityIndex", qualityDropdown.value);
        
        if (fullscreenToggle != null)
            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        
        if (vsyncToggle != null)
            PlayerPrefs.SetInt("VSync", vsyncToggle.isOn ? 1 : 0);
        
        if (brightnessSlider != null)
            PlayerPrefs.SetFloat(brightnessKey, brightnessSlider.value);
        
        // Gameplay
        if (cameraSensitivitySlider != null)
            PlayerPrefs.SetFloat(cameraSensitivityKey, cameraSensitivitySlider.value);
        
        if (invertYAxisToggle != null)
            PlayerPrefs.SetInt("InvertYAxis", invertYAxisToggle.isOn ? 1 : 0);
        
        PlayerPrefs.Save();
        
        if (saveStatusText != null)
        {
            saveStatusText.text = "Settings Saved!";
            
            // Animate the save status text with a fade and scale
            saveStatusText.transform.localScale = Vector3.one * 0.8f;
            saveStatusText.color = new Color(saveStatusText.color.r, saveStatusText.color.g, 
                saveStatusText.color.b, 0f);
            
            Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Scale(saveStatusText.transform, 1f, duration: 0.3f, 
                    ease: Ease.OutBack))
                .Group(Tween.Alpha(saveStatusText, 1f, duration: 0.2f, ease: Ease.OutQuad))
                .ChainDelay(1.5f)
                .Chain(Tween.Alpha(saveStatusText, 0f, duration: 0.3f, ease: Ease.InQuad))
                .OnComplete(ClearSaveStatus);
            
            // Play confirmation sound
            UIAudio.PlaySelect();
        }
    }
    
    /// <summary>
    /// Loads saved settings from PlayerPrefs.
    /// </summary>
    public void LoadSettings()
    {
        // Audio
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
        
        // Video
        if (resolutionDropdown != null)
        {
            int index = PlayerPrefs.GetInt("ResolutionIndex", resolutionDropdown.value);
            resolutionDropdown.value = index;
        }
        
        if (qualityDropdown != null)
        {
            int index = PlayerPrefs.GetInt("QualityIndex", QualitySettings.GetQualityLevel());
            qualityDropdown.value = index;
        }
        
        if (fullscreenToggle != null)
        {
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.isOn = isFullscreen;
        }
        
        if (vsyncToggle != null)
        {
            bool vsync = PlayerPrefs.GetInt("VSync", QualitySettings.vSyncCount) == 1;
            vsyncToggle.isOn = vsync;
        }
        
        if (brightnessSlider != null)
        {
            float value = PlayerPrefs.GetFloat(brightnessKey, 1f);
            brightnessSlider.value = value;
        }
        
        // Gameplay
        if (cameraSensitivitySlider != null)
        {
            float value = PlayerPrefs.GetFloat(cameraSensitivityKey, 1f);
            cameraSensitivitySlider.value = value;
        }
        
        if (invertYAxisToggle != null)
        {
            bool inverted = PlayerPrefs.GetInt("InvertYAxis", 0) == 1;
            invertYAxisToggle.isOn = inverted;
        }
    }
    
    /// <summary>
    /// Resets all settings to default values.
    /// </summary>
    public void ResetToDefault()
    {
        // Reset audio
        if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 1f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1f;
        
        // Reset video
        if (qualityDropdown != null) qualityDropdown.value = QualitySettings.GetQualityLevel();
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (vsyncToggle != null) vsyncToggle.isOn = true;
        if (brightnessSlider != null) brightnessSlider.value = 1f;
        
        // Reset gameplay
        if (cameraSensitivitySlider != null) cameraSensitivitySlider.value = 1f;
        if (invertYAxisToggle != null) invertYAxisToggle.isOn = false;
        
        SaveSettings();
    }
    
    private void ClearSaveStatus()
    {
        if (saveStatusText != null)
            saveStatusText.text = "";
    }
    
    #endregion
    
    #region Key Remapping
    
    private void OpenKeyRemapping()
    {
        if (keyRemappingPanel != null)
            keyRemappingPanel.SetActive(true);
        
        // Key remapping implementation would go here
        // This is a complex feature that would require a separate component
        Debug.Log("Key remapping panel opened (implementation pending)");
    }
    
    #endregion
}

