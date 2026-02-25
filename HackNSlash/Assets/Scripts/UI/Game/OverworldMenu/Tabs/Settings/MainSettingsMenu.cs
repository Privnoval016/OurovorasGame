using Extensions.UI;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Main settings menu - first layer with navigation buttons.
/// Routes to Save, Load, Options (submenu), Remap Keys, and Return to Title.
/// </summary>
public class MainSettingsMenu : MonoBehaviour
{
    [Header("Navigation Buttons")]
    [SerializeField] private SettingsButton saveButton;
    [SerializeField] private SettingsButton loadButton;
    [SerializeField] private SettingsButton optionsButton;
    [SerializeField] private SettingsButton keyRemappingButton;
    [SerializeField] private SettingsButton returnToTitleButton;
    
    [Header("Default Selection")]
    [SerializeField] private SettingsButton defaultButton;
    
    private EventSystem eventSystem;
    
    /// <summary>
    /// Events for opening menus.
    /// </summary>
    public System.Action OnSaveMenuRequested;
    public System.Action OnLoadMenuRequested;
    public System.Action OnOptionsMenuRequested;
    public System.Action OnKeyRemappingRequested;
    public System.Action OnReturnToTitleRequested;
    
    private void Awake()
    {
        eventSystem = EventSystem.current;
        
        // Initialize buttons
        if (saveButton != null)
        {
            saveButton.Initialize("Save Game");
            saveButton.onPressed.AddListener(() => OnSaveMenuRequested?.Invoke());
        }
        
        if (loadButton != null)
        {
            loadButton.Initialize("Load Game");
            loadButton.onPressed.AddListener(() => OnLoadMenuRequested?.Invoke());
        }
        
        if (optionsButton != null)
        {
            optionsButton.Initialize("Options");
            optionsButton.onPressed.AddListener(() => OnOptionsMenuRequested?.Invoke());
        }
        
        if (keyRemappingButton != null)
        {
            keyRemappingButton.Initialize("Remap Keybinds");
            keyRemappingButton.onPressed.AddListener(() => OnKeyRemappingRequested?.Invoke());
        }
        
        if (returnToTitleButton != null)
        {
            returnToTitleButton.Initialize("Return to Title");
            returnToTitleButton.onPressed.AddListener(() => OnReturnToTitleRequested?.Invoke());
        }
    }
    
    /// <summary>
    /// Called when this menu is shown.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        
        // Select default button
        if (defaultButton != null && eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(defaultButton.gameObject);
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

