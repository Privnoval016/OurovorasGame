using Extensions.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Game settings menu - camera sensitivity, invert Y, and other gameplay settings.
/// Designed to be scrollable for future expansion.
/// </summary>
public class GameSettingsMenu : MonoBehaviour
{
    [Header("Gameplay Settings")]
    [SerializeField] private Slider cameraSensitivitySlider;
    [SerializeField] private TextMeshProUGUI cameraSensitivityText;
    [SerializeField] private Toggle invertYAxisToggle;
    
    [Header("Scroll View (for future expansion)")]
    [SerializeField] private ScrollRect scrollRect;
    
    [Header("Save/Reset")]
    [SerializeField] private SettingsButton saveButton;
    [SerializeField] private SettingsButton resetButton;
    [SerializeField] private TextMeshProUGUI saveStatusText;
    
    [Header("Settings Keys")]
    [SerializeField] private string cameraSensitivityKey = "CameraSensitivity";
    [SerializeField] private string invertYAxisKey = "InvertYAxis";
    
    [Header("Default Selection")]
    [SerializeField] private Selectable defaultButton;
    
    private EventSystem eventSystem;
    
    private void Awake()
    {
        eventSystem = EventSystem.current;
        
        // Initialize controls
        if (cameraSensitivitySlider != null)
            cameraSensitivitySlider.onValueChanged.AddListener(SetCameraSensitivity);
        
        if (invertYAxisToggle != null)
            invertYAxisToggle.onValueChanged.AddListener(SetInvertYAxis);
        
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
    /// Shows the game settings menu and loads current values.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        
        LoadSettings();
        
        // Reset scroll to top
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
        
        // Select default button
        if (defaultButton != null && eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(defaultButton.gameObject);
        }
    }
    
    /// <summary>
    /// Hides the game settings menu.
    /// </summary>
    public void Hide()
    {
        SaveSettings(); // Auto-save when leaving
        gameObject.SetActive(false);
    }
    
    #region Gameplay Settings
    
    private void SetCameraSensitivity(float value)
    {
        if (cameraSensitivityText != null)
            cameraSensitivityText.text = value.ToString("F2");
        
        // TODO: Apply to camera controller when implemented
        // For now, just save the value
        
        UIAudio.PlayHover();
    }
    
    private void SetInvertYAxis(bool inverted)
    {
        // TODO: Apply to camera controller when implemented
        // For now, just save the value
        
        UIAudio.PlaySelect();
    }
    
    #endregion
    
    #region Save/Load
    
    private void SaveSettings()
    {
        if (cameraSensitivitySlider != null)
            PlayerPrefs.SetFloat(cameraSensitivityKey, cameraSensitivitySlider.value);
        
        if (invertYAxisToggle != null)
            PlayerPrefs.SetInt(invertYAxisKey, invertYAxisToggle.isOn ? 1 : 0);
        
        PlayerPrefs.Save();
        
        ShowSaveConfirmation();
    }
    
    private void LoadSettings()
    {
        if (cameraSensitivitySlider != null)
        {
            float value = PlayerPrefs.GetFloat(cameraSensitivityKey, 1f);
            cameraSensitivitySlider.value = value;
        }
        
        if (invertYAxisToggle != null)
        {
            bool inverted = PlayerPrefs.GetInt(invertYAxisKey, 0) == 1;
            invertYAxisToggle.isOn = inverted;
        }
    }
    
    private void ResetToDefault()
    {
        if (cameraSensitivitySlider != null) cameraSensitivitySlider.value = 1f;
        if (invertYAxisToggle != null) invertYAxisToggle.isOn = false;
        
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

