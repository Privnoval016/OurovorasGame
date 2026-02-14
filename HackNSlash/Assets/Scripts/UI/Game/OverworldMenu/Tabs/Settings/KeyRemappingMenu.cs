using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Key remapping menu - allows remapping combat input actions.
/// Only remaps combat actions, not menu navigation.
/// TODO: Implement full InputSystem rebinding UI.
/// </summary>
public class KeyRemappingMenu : MonoBehaviour
{
    [Header("Remapping UI")]
    [SerializeField] private GameObject remapButtonPrefab; // Prefab for individual remap buttons
    [SerializeField] private Transform remapButtonContainer; // Parent for remap buttons
    [SerializeField] private TextMeshProUGUI instructionText;
    
    [Header("Save/Reset")]
    [SerializeField] private SettingsButton saveButton;
    [SerializeField] private SettingsButton resetButton;
    [SerializeField] private TextMeshProUGUI saveStatusText;
    
    [Header("Default Selection")]
    [SerializeField] private Selectable defaultButton;
    
    private EventSystem eventSystem;
    private bool isWaitingForInput = false;
    
    private void Awake()
    {
        eventSystem = EventSystem.current;
        
        // Initialize buttons
        if (saveButton != null)
        {
            saveButton.Initialize("Save");
            saveButton.onPressed.AddListener(SaveBindings);
        }
        
        if (resetButton != null)
        {
            resetButton.Initialize("Reset to Default");
            resetButton.onPressed.AddListener(ResetBindings);
        }
        
        // TODO: Populate remap buttons for each combat action
        // This requires integration with InputManager and InputActions
        Debug.Log("KeyRemappingMenu: Full rebinding implementation pending");
    }
    
    /// <summary>
    /// Shows the key remapping menu.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        
        if (instructionText != null)
            instructionText.text = "Select an action to remap, then press any button to assign.";
        
        // Select default button
        if (defaultButton != null && eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(defaultButton.gameObject);
        }
    }
    
    /// <summary>
    /// Hides the key remapping menu.
    /// </summary>
    public void Hide()
    {
        isWaitingForInput = false;
        gameObject.SetActive(false);
    }
    
    #region Remapping (Placeholder)
    
    /// <summary>
    /// Starts listening for input to remap an action.
    /// </summary>
    /// <param name="actionName">The name of the action to remap</param>
    public void StartRemap(string actionName)
    {
        if (isWaitingForInput) return;
        
        isWaitingForInput = true;
        
        if (instructionText != null)
            instructionText.text = $"Press any button to bind to {actionName}...";
        
        // TODO: Implement InputSystem rebinding
        // Use InputActionRebindingExtensions.PerformInteractiveRebinding()
        // Example:
        // var rebindOperation = action.PerformInteractiveRebinding()
        //     .OnComplete(operation => CompleteRemap(operation))
        //     .Start();
        
        Debug.Log($"KeyRemappingMenu: Rebinding {actionName} (not implemented)");
        
        // Placeholder: End remap after a moment
        Invoke(nameof(CancelRemap), 2f);
    }
    
    private void CompleteRemap(InputActionRebindingExtensions.RebindingOperation operation)
    {
        isWaitingForInput = false;
        
        if (instructionText != null)
            instructionText.text = "Binding updated!";
        
        operation.Dispose();
        UIAudio.PlaySelect();
        
        // Reset instruction text after a delay
        Invoke(nameof(ResetInstructionText), 1.5f);
    }
    
    private void CancelRemap()
    {
        isWaitingForInput = false;
        ResetInstructionText();
    }
    
    private void ResetInstructionText()
    {
        if (instructionText != null)
            instructionText.text = "Select an action to remap, then press any button to assign.";
    }
    
    #endregion
    
    #region Save/Reset
    
    private void SaveBindings()
    {
        // TODO: Save binding overrides to PlayerPrefs or JSON
        // InputSystem stores overrides in action maps
        
        Debug.Log("KeyRemappingMenu: Saving bindings (not implemented)");
        
        if (saveStatusText != null)
        {
            saveStatusText.text = "Bindings Saved!";
            // Animate similar to other menus
        }
        
        UIAudio.PlaySelect();
    }
    
    private void ResetBindings()
    {
        // TODO: Reset all bindings to default
        // Use inputAction.RemoveAllBindingOverrides()
        
        Debug.Log("KeyRemappingMenu: Resetting bindings (not implemented)");
        UIAudio.PlaySelect();
    }
    
    #endregion
}

