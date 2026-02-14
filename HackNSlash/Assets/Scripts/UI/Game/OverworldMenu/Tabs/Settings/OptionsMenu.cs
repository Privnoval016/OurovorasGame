using Extensions.UI;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Options submenu - second layer navigation for Audio, Video, and Game settings.
/// Acts as an intermediate layer between MainSettingsMenu and individual setting menus.
/// </summary>
public class OptionsMenu : MonoBehaviour
{
    [Header("Navigation Buttons")]
    [SerializeField] private SettingsButton audioButton;
    [SerializeField] private SettingsButton videoButton;
    [SerializeField] private SettingsButton gameButton;
    
    [Header("Default Selection")]
    [SerializeField] private SettingsButton defaultButton;
    
    private EventSystem eventSystem;
    
    /// <summary>
    /// Events for opening individual setting menus.
    /// </summary>
    public System.Action OnAudioSettingsRequested;
    public System.Action OnVideoSettingsRequested;
    public System.Action OnGameSettingsRequested;
    
    private void Awake()
    {
        eventSystem = EventSystem.current;
        
        // Initialize buttons
        if (audioButton != null)
        {
            audioButton.Initialize("Audio Settings");
            audioButton.onPressed.AddListener(() => OnAudioSettingsRequested?.Invoke());
        }
        
        if (videoButton != null)
        {
            videoButton.Initialize("Video Settings");
            videoButton.onPressed.AddListener(() => OnVideoSettingsRequested?.Invoke());
        }
        
        if (gameButton != null)
        {
            gameButton.Initialize("Game Settings");
            gameButton.onPressed.AddListener(() => OnGameSettingsRequested?.Invoke());
        }
    }
    
    /// <summary>
    /// Called when this menu is shown.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        
        // Select default button
        if (defaultButton != null)
        {
            defaultButton.Select();
        }
    }
    
    /// <summary>
    /// Called when this menu is hidden.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}

