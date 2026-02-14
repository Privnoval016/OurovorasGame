using Extensions.UI;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tab 8: Settings menu main controller.
/// Manages three-layer navigation:
/// Layer 1: Main menu (Save, Load, Options, Remap Keys, Return to Title)
/// Layer 2: Options submenu (Audio, Video, Game)
/// Layer 3: Individual setting menus (Audio Settings, Video Settings, Game Settings)
/// </summary>
public class SettingsTab : TabSelection
{
    [Header("Menu Layers")]
    [SerializeField] private MainSettingsMenu mainMenu;
    [SerializeField] private OptionsMenu optionsMenu;
    [SerializeField] private SaveMenu saveMenu;
    [SerializeField] private LoadMenu loadMenu;
    [SerializeField] private AudioSettingsMenu audioMenu;
    [SerializeField] private VideoSettingsMenu videoMenu;
    [SerializeField] private GameSettingsMenu gameMenu;
    [SerializeField] private KeyRemappingMenu keyRemapMenu;
    
    private enum MenuLayer
    {
        Main,           // Layer 1: Main navigation
        Options,        // Layer 2: Options submenu
        SettingsDetail  // Layer 3: Actual settings (Save, Load, Audio, Video, Game, KeyRemap)
    }
    
    private MenuLayer currentLayer = MenuLayer.Main;
    
    #region Initialization
    
    private void Awake()
    {
        // Subscribe to main menu navigation events
        if (mainMenu != null)
        {
            mainMenu.OnSaveMenuRequested += OpenSaveMenu;
            mainMenu.OnLoadMenuRequested += OpenLoadMenu;
            mainMenu.OnOptionsMenuRequested += OpenOptionsMenu;
            mainMenu.OnKeyRemappingRequested += OpenKeyRemapMenu;
            mainMenu.OnReturnToTitleRequested += ReturnToTitle;
        }
        
        // Subscribe to options menu navigation events
        if (optionsMenu != null)
        {
            optionsMenu.OnAudioSettingsRequested += OpenAudioMenu;
            optionsMenu.OnVideoSettingsRequested += OpenVideoMenu;
            optionsMenu.OnGameSettingsRequested += OpenGameMenu;
        }
        
        // Subscribe to back button
        if (InputManager.Instance != null)
        {
            InputManager.Instance.onBack += OnBackInput;
        }
        
        // Initialize all menus as hidden except main
        HideAllMenus();
    }
    
    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.onBack -= OnBackInput;
        }
    }
    
    #endregion
    
    #region TabSelection Overrides
    
    public override void OnTabSelect()
    {
        base.OnTabSelect();
        
        // Show main menu
        ShowMainMenu();
    }
    
    public override void OnTabDeselect()
    {
        base.OnTabDeselect();
        
        // Hide all menus
        HideAllMenus();
        currentLayer = MenuLayer.Main;
    }
    
    #endregion
    
    #region Navigation
    
    private void OnBackInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        // Navigate back based on current layer
        switch (currentLayer)
        {
            case MenuLayer.SettingsDetail:
                // Return to previous layer (either Options or Main)
                ReturnFromSettingsDetail();
                break;
            
            case MenuLayer.Options:
                // Return to main menu
                ReturnToMainMenu();
                break;
            
            case MenuLayer.Main:
                // Already at main menu, do nothing (tab switching handles this)
                break;
        }
    }
    
    private void ShowMainMenu()
    {
        HideAllMenus();
        
        if (mainMenu != null)
        {
            mainMenu.Show();
        }
        
        currentLayer = MenuLayer.Main;
    }
    
    private void ReturnToMainMenu()
    {
        UIAudio.PlayBack();
        
        HideAllMenus();
        
        if (mainMenu != null)
        {
            mainMenu.Show();
        }
        
        currentLayer = MenuLayer.Main;
    }
    
    private void ReturnFromSettingsDetail()
    {
        UIAudio.PlayBack();
        
        // Hide all settings detail menus
        if (saveMenu != null) saveMenu.Hide();
        if (loadMenu != null) loadMenu.Hide();
        if (audioMenu != null) audioMenu.Hide();
        if (videoMenu != null) videoMenu.Hide();
        if (gameMenu != null) gameMenu.Hide();
        if (keyRemapMenu != null) keyRemapMenu.Hide();
        
        // Return to options menu if we came from there, otherwise main menu
        // We can determine this by checking which menu was open
        // For simplicity, check if last opened was audio/video/game (came from Options)
        if (WasOpenedFromOptions())
        {
            if (optionsMenu != null)
            {
                optionsMenu.Show();
            }
            currentLayer = MenuLayer.Options;
        }
        else
        {
            if (mainMenu != null)
            {
                mainMenu.Show();
            }
            currentLayer = MenuLayer.Main;
        }
    }
    
    private void HideAllMenus()
    {
        if (mainMenu != null) mainMenu.Hide();
        if (optionsMenu != null) optionsMenu.Hide();
        if (saveMenu != null) saveMenu.Hide();
        if (loadMenu != null) loadMenu.Hide();
        if (audioMenu != null) audioMenu.Hide();
        if (videoMenu != null) videoMenu.Hide();
        if (gameMenu != null) gameMenu.Hide();
        if (keyRemapMenu != null) keyRemapMenu.Hide();
    }
    
    private bool WasOpenedFromOptions()
    {
        // Check if audio, video, or game menu was active
        // These menus are accessed through Options submenu
        return (audioMenu != null && audioMenu.gameObject.activeSelf) ||
               (videoMenu != null && videoMenu.gameObject.activeSelf) ||
               (gameMenu != null && gameMenu.gameObject.activeSelf);
    }
    
    #endregion
    
    #region Menu Opening - Layer 1 (Main Menu)
    
    private void OpenSaveMenu()
    {
        if (mainMenu != null) mainMenu.Hide();
        if (saveMenu != null) saveMenu.Show();
        currentLayer = MenuLayer.SettingsDetail;
    }
    
    private void OpenLoadMenu()
    {
        if (mainMenu != null) mainMenu.Hide();
        if (loadMenu != null) loadMenu.Show();
        currentLayer = MenuLayer.SettingsDetail;
    }
    
    private void OpenOptionsMenu()
    {
        if (optionsMenu != null) optionsMenu.Show();
        currentLayer = MenuLayer.Options;
    }
    
    private void OpenKeyRemapMenu()
    {
        if (mainMenu != null) mainMenu.Hide();
        if (keyRemapMenu != null) keyRemapMenu.Show();
        currentLayer = MenuLayer.SettingsDetail;
    }
    
    private void ReturnToTitle()
    {
        // TODO: Implement return to title functionality
        Debug.Log("SettingsTab: Return to Title (not implemented)");
        UIAudio.PlaySelect();
    }
    
    #endregion
    
    #region Menu Opening - Layer 2 (Options Menu)
    
    private void OpenAudioMenu()
    {
        if (optionsMenu != null) optionsMenu.Hide();
        if (audioMenu != null) audioMenu.Show();
        currentLayer = MenuLayer.SettingsDetail;
    }
    
    private void OpenVideoMenu()
    {
        if (optionsMenu != null) optionsMenu.Hide();
        if (videoMenu != null) videoMenu.Show();
        currentLayer = MenuLayer.SettingsDetail;
    }
    
    private void OpenGameMenu()
    {
        if (optionsMenu != null) optionsMenu.Hide();
        if (gameMenu != null) gameMenu.Show();
        currentLayer = MenuLayer.SettingsDetail;
    }
    
    #endregion
}

