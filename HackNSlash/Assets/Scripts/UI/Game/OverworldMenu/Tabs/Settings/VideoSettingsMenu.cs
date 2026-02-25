using Extensions.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Video settings menu - resolution, quality, fullscreen, vsync, brightness.
/// </summary>
public class VideoSettingsMenu : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TextMeshProUGUI brightnessText;
    
    [Header("Save/Reset")]
    [SerializeField] private SettingsButton saveButton;
    [SerializeField] private SettingsButton resetButton;
    [SerializeField] private TextMeshProUGUI saveStatusText;
    
    [Header("Settings Keys")]
    [SerializeField] private string brightnessKey = "Brightness";
    
    [Header("Default Selection")]
    [SerializeField] private Selectable defaultButton;
    
    private Resolution[] availableResolutions;
    private EventSystem eventSystem;
    
    private void Awake()
    {
        eventSystem = EventSystem.current;
        
        // Initialize dropdowns and controls
        InitializeResolutionDropdown();
        InitializeQualityDropdown();
        
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        
        if (vsyncToggle != null)
            vsyncToggle.onValueChanged.AddListener(SetVSync);
        
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        
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
    /// Shows the video settings menu and loads current values.
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
    /// Hides the video settings menu.
    /// </summary>
    public void Hide()
    {
        SaveSettings(); // Auto-save when leaving
        gameObject.SetActive(false);
    }
    
    #region Initialization
    
    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null) return;
        
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
        if (qualityDropdown == null) return;
        
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
        qualityDropdown.onValueChanged.AddListener(SetQuality);
    }
    
    #endregion
    
    #region Video Settings
    
    private void SetResolution(int resolutionIndex)
    {
        if (availableResolutions == null || resolutionIndex < 0 || resolutionIndex >= availableResolutions.Length)
            return;
        
        Resolution resolution = availableResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        
        UIAudio.PlaySelect();
    }
    
    private void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        UIAudio.PlaySelect();
    }
    
    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        UIAudio.PlaySelect();
    }
    
    private void SetVSync(bool enable)
    {
        QualitySettings.vSyncCount = enable ? 1 : 0;
        UIAudio.PlaySelect();
    }
    
    private void SetBrightness(float value)
    {
        // Brightness implementation depends on your rendering setup
        // This is a placeholder that would typically adjust post-processing
        RenderSettings.ambientIntensity = value;
        
        if (brightnessText != null)
        {
            int targetPercent = Mathf.RoundToInt(value * 100);
            brightnessText.text = $"{targetPercent}%";
        }
    }
    
    #endregion
    
    #region Save/Load
    
    private void SaveSettings()
    {
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
        
        PlayerPrefs.Save();
        
        ShowSaveConfirmation();
    }
    
    private void LoadSettings()
    {
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
    }
    
    private void ResetToDefault()
    {
        if (qualityDropdown != null) qualityDropdown.value = QualitySettings.GetQualityLevel();
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (vsyncToggle != null) vsyncToggle.isOn = true;
        if (brightnessSlider != null) brightnessSlider.value = 1f;
        
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

