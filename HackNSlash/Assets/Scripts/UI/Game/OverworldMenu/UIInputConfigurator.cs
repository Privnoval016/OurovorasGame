using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Automatically configures the EventSystem's InputSystemUIInputModule
/// to work with the Menu input actions for controller navigation.
/// This bridges the gap between Unity Input System and Unity UI EventSystem.
/// CRITICAL: This is what makes D-Pad/Stick navigation and A/B buttons work!
/// </summary>
[RequireComponent(typeof(EventSystem))]
public class UIInputConfigurator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to InputManager - auto-found if not assigned")]
    [SerializeField] private InputManager inputManager;
    
    [Header("Debug")]
    [Tooltip("Enable to see configuration logs")]
    [SerializeField] private bool debugLogs = true;
    
    private EventSystem eventSystem;
    private InputSystemUIInputModule uiInputModule;
    private PlayerInputActions inputActions;
    
    private void Awake()
    {
        eventSystem = GetComponent<EventSystem>();
        
        if (inputManager == null)
        {
            inputManager = InputManager.Instance;
        }
        
        SetupUIInputModule();
    }
    
    private void OnEnable()
    {
        // Ensure Menu action map is enabled when UI is active
        if (inputActions != null)
        {
            inputActions.Menu.Enable();
            if (debugLogs) Debug.Log("UIInputConfigurator: Enabled Menu action map");
        }
    }
    
    private void OnDisable()
    {
        // Don't disable here - InputManager handles action map switching
    }
    
    /// <summary>
    /// Sets up or configures the InputSystemUIInputModule on the EventSystem.
    /// This is what enables controller/keyboard navigation in Unity UI.
    /// </summary>
    private void SetupUIInputModule()
    {
        // Remove any existing input modules (StandaloneInputModule, etc.)
        var existingModules = GetComponents<BaseInputModule>();
        foreach (var module in existingModules)
        {
            if (module is InputSystemUIInputModule)
            {
                uiInputModule = (InputSystemUIInputModule)module;
            }
            else
            {
                if (debugLogs) Debug.Log($"UIInputConfigurator: Removing old input module: {module.GetType().Name}");
                DestroyImmediate(module);
            }
        }
        
        // Add InputSystemUIInputModule if it doesn't exist
        if (uiInputModule == null)
        {
            uiInputModule = gameObject.AddComponent<InputSystemUIInputModule>();
            if (debugLogs) Debug.Log("UIInputConfigurator: Added InputSystemUIInputModule");
        }
        
        // Create new PlayerInputActions instance
        inputActions = new PlayerInputActions();
        inputActions.Enable();
        
        // Get the Menu action map
        var menuActions = inputActions.Menu;
        
        // CRITICAL: Assign the action references to the UI Input Module
        // These are what connect Input System to EventSystem navigation
        uiInputModule.move = UnityEngine.InputSystem.InputActionReference.Create(menuActions.Navigate);
        uiInputModule.submit = UnityEngine.InputSystem.InputActionReference.Create(menuActions.Select);
        uiInputModule.cancel = UnityEngine.InputSystem.InputActionReference.Create(menuActions.Deselect);
        
        // Mouse/touch actions (we don't have these in Menu action map, so leave null)
        // This is fine - we're controller-only anyway
        uiInputModule.point = null;
        uiInputModule.leftClick = null;
        uiInputModule.middleClick = null;
        uiInputModule.rightClick = null;
        uiInputModule.scrollWheel = null;
        uiInputModule.trackedDevicePosition = null;
        uiInputModule.trackedDeviceOrientation = null;
        
        // Configure input delay and repeat rate for responsive navigation
        uiInputModule.moveRepeatDelay = 0.5f; // Time before repeat starts
        uiInputModule.moveRepeatRate = 0.1f; // Time between repeats
        
        // Enable the Menu action map
        menuActions.Enable();
        
        if (debugLogs)
        {
            Debug.Log("UIInputConfigurator: ✅ Configured InputSystemUIInputModule");
            Debug.Log("  - Move: Menu/Navigate");
            Debug.Log("  - Submit: Menu/Select");  
            Debug.Log("  - Cancel: Menu/Deselect");
            Debug.Log("EventSystem should now respond to controller input!");
        }
    }
    
    /// <summary>
    /// Call this after recompilation or to reset the configuration
    /// </summary>
    [ContextMenu("Reconfigure UI Input")]
    public void ReconfigureUIInput()
    {
        SetupUIInputModule();
    }
}

